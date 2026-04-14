import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import * as z from 'zod';
import { Logger } from '../utils/logger.js';

// Constants for the tool
const toolName = 'create_prefab';
const toolDescription = 'Creates a prefab with optional MonoBehaviour script and serialized field values';

// Parameter schema for the tool
const paramsSchema = z.object({
  componentName: z.string().optional().describe('The name of the MonoBehaviour Component to add to the prefab (optional)'),
  prefabName: z.string().describe('The name of the prefab to create'),
  fieldValues: z.record(z.any()).optional().describe('Optional JSON object of serialized field values to apply to the prefab')
});

/**
 * Creates and registers the CreatePrefab tool with the MCP server
 * 
 * @param server The MCP server to register the tool with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerCreatePrefabTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
 * Handler function for the CreatePrefab tool
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The validated parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if validation fails or the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any) {
  if (!params.prefabName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "'prefabName' must be provided"
    );
  }
  
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to create prefab`
    );
  }
  
  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully created prefab`
    }],
    // Include the prefab path in the result for programmatic access
    data: {
      prefabPath: response.prefabPath
    }
  };
}
