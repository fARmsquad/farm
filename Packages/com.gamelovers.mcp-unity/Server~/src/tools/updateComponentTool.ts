import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'update_component';
const toolDescription = 'Updates component fields on a GameObject or adds it to the GameObject if it does not contain the component';
const paramsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject to update'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy to update (alternative to instanceId)'),
  componentName: z.string().describe('The name of the component to update or add'),
  componentData: z.record(z.any()).optional().describe('An object containing the fields to update on the component (optional)')
});

/**
 * Creates and registers the Update Component tool with the MCP server
 * This tool allows updating or adding components to GameObjects in the Unity Editor
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerUpdateComponentTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
      
  // Register this tool with the MCP server
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

/**
 * Handles updating or adding a component to a GameObject in Unity
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  // Validate parameters - require either instanceId or objectPath
  if ((params.instanceId === undefined || params.instanceId === null) && 
      (!params.objectPath || params.objectPath.trim() === '')) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Either 'instanceId' or 'objectPath' must be provided"
    );
  }
  
  if (!params.componentName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'componentName' must be provided"
    );
  }
  
  // Send request to Unity
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath,
      componentName: params.componentName,
      componentData: params.componentData
    }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to update component on GameObject`
    );
  }
  
  // Create a description of which GameObject was targeted
  const targetDescription = params.objectPath 
    ? `path '${params.objectPath}'` 
    : `ID ${params.instanceId}`;
  
  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully updated component on GameObject with ${targetDescription}`
    }]
  };
}
