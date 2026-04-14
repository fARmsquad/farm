import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the resource
const resourceName = 'get_packages';
const resourceUri = 'unity://packages';
const resourceMimeType = 'application/json';

/**
 * Creates and registers the Packages resource with the MCP server
 * This resource provides access to the Unity Package Manager packages
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetPackagesResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);
      
  // Register this resource with the MCP server
  server.resource(
    resourceName,
    resourceUri,
    {
      description: 'Retrieve all packages from the Unity Package Manager',
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
 * Handles requests for package information from Unity Package Manager
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @returns A promise that resolves to the packages data
 * @throws McpUnityError if the request to Unity fails
 */
async function resourceHandler(mcpUnity: McpUnity): Promise<ReadResourceResult> {
      const response = await mcpUnity.sendRequest({
        method: resourceName,
        params: {}
      });
      
      if (!response.success) {
        throw new McpUnityError(
          ErrorType.RESOURCE_FETCH,
          response.message || 'Failed to fetch packages from Unity Package Manager'
        );
      }
      
      // Transform the data into a structured format
      const projectPackages = response.projectPackages || [];
      const registryPackages = response.registryPackages || [];
      
      const packagesData = {
        projectPackages: projectPackages.map((pkg: any) => ({
          name: pkg.name,
          displayName: pkg.displayName,
          version: pkg.version,
          description: pkg.description,
          category: pkg.category,
          source: pkg.source,
          state: pkg.state,
          author: pkg.author
        })),
        registryPackages: registryPackages.map((pkg: any) => ({
          name: pkg.name,
          displayName: pkg.displayName,
          version: pkg.version,
          description: pkg.description,
          category: pkg.category,
          source: pkg.source,
          state: pkg.state,
          author: pkg.author
        }))
      };
      
      return {
        contents: [
          {
            uri: resourceUri,
            text: JSON.stringify(packagesData, null, 2),
            mimeType: resourceMimeType
          }
        ]
      };
}
