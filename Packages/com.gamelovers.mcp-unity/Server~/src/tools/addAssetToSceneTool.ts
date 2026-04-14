import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import * as z from 'zod';
import { Logger } from '../utils/logger.js';

// Constants for the tool
const toolName = 'add_asset_to_scene';
const toolDescription = 'Adds an asset from the AssetDatabase to the Unity scene';

// Parameter schema for the tool
const paramsSchema = z.object({
  assetPath: z.string().optional().describe('The path of the asset in the AssetDatabase'),
  guid: z.string().optional().describe('The GUID of the asset'),
  position: z.object({
    x: z.number().default(0).describe('X position in the scene'),
    y: z.number().default(0).describe('Y position in the scene'),
    z: z.number().default(0).describe('Z position in the scene')
  }).optional().describe('Position in the scene (defaults to Vector3.zero)'),
  parentPath: z.string().optional().describe('The path of the parent GameObject in the hierarchy'),
  parentId: z.number().optional().describe('The instance ID of the parent GameObject')
});

/**
 * Creates and registers the AddAssetToScene tool with the MCP server
 * 
 * @param server The MCP server to register the tool with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerAddAssetToSceneTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
  
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
 * Handler function for the AddAssetToScene tool
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The validated parameters for the tool
 * @param logger The logger instance for diagnostic information
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if validation fails or the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any) {  
  if (!params.assetPath && !params.guid) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Either 'assetPath' or 'guid' must be provided"
    );
  }
  
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to add asset to scene`
    );
  }
  
  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully added asset to scene`
    }]
  };
}
