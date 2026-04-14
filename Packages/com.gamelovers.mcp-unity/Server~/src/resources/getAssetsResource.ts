import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the resource
const resourceName = 'get_assets';
const resourceUri = 'unity://assets';
const resourceMimeType = 'application/json';

/**
 * Creates and registers the Assets resource with the MCP server
 * This resource provides access to assets in the Unity project
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetAssetsResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);
      
  // Register this resource with the MCP server
  server.resource(
    resourceName,
    resourceUri,
    {
      description: 'Retrieve assets from the Unity Asset Database',
      mimeType: resourceMimeType
    },
    async () => {
      try {
        return await resourceHandler(mcpUnity);
      } catch (error) {
        logger.error(`Error handling resource ${resourceName}: ${error}`);
        throw error;
      }
    }
  );
}

/**
 * Handles requests for asset information from Unity's Asset Database
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @returns A promise that resolves to the assets data
 * @throws McpUnityError if the request to Unity fails
 */
async function resourceHandler(mcpUnity: McpUnity): Promise<ReadResourceResult> {
      // Since we're using a non-templated ResourceDefinition, we need to handle all assets without parameters
      const response = await mcpUnity.sendRequest({
        method: resourceName,
        params: {}
      });
      
      if (!response.success) {
        throw new McpUnityError(
          ErrorType.RESOURCE_FETCH,
          response.message || 'Failed to fetch assets from Unity Asset Database'
        );
      }
      
      // Transform the data into a structured format
      const assets = response.assets || [];
      
      const assetsData = {
        assets: assets.map((asset: any) => ({
          name: asset.name,
          filename: asset.filename,
          path: asset.path,
          type: asset.type,
          extension: asset.extension,
          guid: asset.guid,
          size: asset.size
        }))
      };
      
      return {
        contents: [
          {
            uri: resourceUri,
            mimeType: resourceMimeType,
            text: JSON.stringify(assetsData, null, 2)
          }
        ]
      };
}
