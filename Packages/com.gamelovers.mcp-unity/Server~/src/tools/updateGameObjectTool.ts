import * as z from 'zod';
import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'update_gameobject';
const toolDescription = 'Updates properties of a GameObject in the Unity scene by its instance ID or path. If the GameObject does not exist at the specified path, it will be created.';
const paramsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject to update'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy to update (alternative to instanceId)'),
  gameObjectData: z.object({
    name: z.string().optional().describe('New name for the GameObject'),
    tag: z.string().optional().describe('New tag for the GameObject'),
    layer: z.number().int().optional().describe('New layer for the GameObject'),
    activeSelf: z.boolean().optional().describe('Set the active state of the GameObject (GameObject.SetActive(value))'),
    isStatic: z.boolean().optional().describe('Set the static state of the GameObject (GameObject.isStatic = value)'),
  }).describe('An object containing the fields to update on the GameObject. If the GameObject does not exist at objectPath, it will be created.')
    .refine(data => Object.keys(data).length > 0, { message: 'gameObjectData must contain at least one property to update.' }),
});

/**
 * Creates and registers the Update GameObject tool with the MCP server
 * This tool allows updating or creating GameObjects into the Unity Editor active scene
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerUpdateGameObjectTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handles updating or creating GameObjects into the Unity Editor active scene
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

    const response = await mcpUnity.sendRequest({
        method: 'update_gameobject',
        params: {
          instanceId: params.instanceId,
          objectPath: params.objectPath,
          gameObjectData: params.gameObjectData,
        }
      });
  
      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message || `Failed to update the GameObject`
        );
      }
  
      // Create a description of which GameObject was targeted
      const targetDescription = params.objectPath 
        ? `path '${params.objectPath}'` 
        : `ID ${params.instanceId}`;
      
      return {
        content: [{
          type: response.type,
          text: response.message || `Successfully updated the GameObject with ${targetDescription}`
        }]
      };
}
