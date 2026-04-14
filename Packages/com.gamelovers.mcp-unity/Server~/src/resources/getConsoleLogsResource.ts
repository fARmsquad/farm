import { Logger } from '../utils/logger.js';
import { ResourceTemplate, McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { Variables } from '@modelcontextprotocol/sdk/shared/uriTemplate.js';

// Constants for the resource
const resourceName = 'get_console_logs';
const resourceMimeType = 'application/json';
const resourceUri = 'unity://logs/{logType}?offset={offset}&limit={limit}&includeStackTrace={includeStackTrace}';
const resourceTemplate = new ResourceTemplate(resourceUri, {
  list: () => listLogTypes(resourceMimeType)
});

function listLogTypes(resourceMimeType: string) {
  return {
    resources: [
      {
        uri: `unity://logs/?offset=0&limit=50&includeStackTrace=true`,
        name: "All logs",
        description: "All Unity console logs (newest first). ⚠️ Set includeStackTrace=false to save 80-90% tokens. Use limit=50 to avoid token limits.",
        mimeType: resourceMimeType
      },
      {
        uri: `unity://logs/error?offset=0&limit=20&includeStackTrace=true`,
        name: "Error logs",
        description: "Error logs only. ⚠️ Start with includeStackTrace=false for quick overview, then true only if debugging specific errors.",
        mimeType: resourceMimeType
      },
      {
        uri: `unity://logs/warning?offset=0&limit=30&includeStackTrace=true`,
        name: "Warning logs", 
        description: "Warning logs only. ⚠️ Use includeStackTrace=false by default to save tokens.",
        mimeType: resourceMimeType
      },
      {
        uri: `unity://logs/info?offset=0&limit=25&includeStackTrace=false`,
        name: "Info logs",
        description: "Info logs only. Stack traces excluded by default to minimize tokens.",
        mimeType: resourceMimeType
      }
    ]
  };
}

/**
 * Registers the get_console_logs resource with the MCP server
 */
export function registerGetConsoleLogsResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);

  server.resource(
    resourceName,
    resourceTemplate,
    {
      description: 'Retrieve Unity console logs by type with pagination support. See individual log type descriptions for optimal settings.',
      mimeType: resourceMimeType
    },
    async (uri, variables) => {
      try {
        return await resourceHandler(mcpUnity, uri, variables, logger);
      } catch (error) {
        logger.error(`Error handling resource ${resourceName}: ${error}`);
        throw error;
      }
    }
  );
}

/**
 * Handles requests for Unity console logs by log type
 */
async function resourceHandler(mcpUnity: McpUnity, uri: URL, variables: Variables, logger: Logger): Promise<ReadResourceResult> {
  // Extract and convert the parameter from the template variables
  let logType = variables["logType"] ? decodeURIComponent(variables["logType"] as string) : undefined;
  if (logType === '') logType = undefined;
  
  // Extract pagination parameters with validation
  const offset = variables["offset"] ? parseInt(variables["offset"] as string, 10) : 0;
  const limit = variables["limit"] ? parseInt(variables["limit"] as string, 10) : 100;
  
  // Extract includeStackTrace parameter
  let includeStackTrace = true; // Default to true for backward compatibility
  if (variables["includeStackTrace"] !== undefined) {
    const value = variables["includeStackTrace"] as string;
    includeStackTrace = value === 'true' || value === '1' || value === 'yes';
  }
  
  // Validate pagination parameters
  if (isNaN(offset) || offset < 0) {
    throw new McpUnityError(ErrorType.VALIDATION, 'Invalid offset parameter: must be a non-negative integer');
  }
  if (isNaN(limit) || limit <= 0) {
    throw new McpUnityError(ErrorType.VALIDATION, 'Invalid limit parameter: must be a positive integer');
  }

  // Send request to Unity
  const response = await mcpUnity.sendRequest({
    method: resourceName,
    params: {
      logType: logType,
      offset: offset,
      limit: limit,
      includeStackTrace: includeStackTrace
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.RESOURCE_FETCH,
      response.message || 'Failed to fetch logs from Unity'
    );
  }

  return {
    contents: [{
      uri: `unity://logs/${logType ?? ''}?offset=${offset}&limit=${limit}&includeStackTrace=${includeStackTrace}`,
      mimeType: resourceMimeType,
      text: JSON.stringify(response, null, 2)
    }]
  };
}
