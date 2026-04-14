import { Logger } from '../utils/logger.js';
import { ResourceTemplate, McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { Variables } from '@modelcontextprotocol/sdk/shared/uriTemplate.js';

// Constants for the resource
const resourceName = 'get_tests';
const resourceMimeType = 'application/json';
const resourceUri = 'unity://tests/{testMode}';
const resourceTemplate = new ResourceTemplate(resourceUri, { 
  list: () => listTestModes(resourceMimeType)
});

export interface TestItem {
  name: string;
  fullName: string;
  path: string;
  testMode: string;
  runState: string;
}

/**
 * Get a list of all test modes (EditMode and PlayMode)
 * @param resourceMimeType The MIME type for the resource
 * @returns A list of resources for each test mode
 */
function listTestModes(resourceMimeType: string) {
  return {
    resources: [
      { 
        uri: `unity://tests/EditMode`, 
        name: "List only 'EditMode' tests",
        description: "List only 'EditMode' tests from Unity's test runner",
        mimeType: resourceMimeType
      },
      { 
        uri: `unity://tests/PlayMode`, 
        name: "List only 'PlayMode' tests",
        description: "List only 'PlayMode' tests from Unity's test runner",
        mimeType: resourceMimeType
      },
      { 
        uri: `unity://tests/`, 
        name: "List all tests",
        description: "List of all tests in Unity's test runner, this includes PlayMode and EditMode tests",
        mimeType: resourceMimeType
      }
    ]
  };
}

/**
 * Creates and registers the Tests resource with the MCP server
 * This resource provides access to Unity's Test Runner tests
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetTestsResource(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering resource: ${resourceName}`);
      
  // Register this resource with the MCP server
  server.resource(
    resourceName,
    resourceTemplate,
    {
      description: 'Retrieve tests from Unity\'s Test Runner',
      mimeType: resourceMimeType
    },
    async (uri, variables) => {
      try {
        return await resourceHandler(mcpUnity, uri, variables);
      } catch (error) {
        logger.error(`Error handling resource ${resourceName}: ${error}`);
        throw error;
      }
    }
  );
}

/**
 * Handles requests for test information from Unity's Test Runner
 * Retrieves tests filtered by test mode (EditMode or PlayMode)
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param uri The requested resource URI
 * @param variables Variables extracted from the URI template
 * @returns A promise that resolves to the test results
 * @throws McpUnityError if the request to Unity fails
 */
async function resourceHandler(mcpUnity: McpUnity, uri: URL, variables: Variables): Promise<ReadResourceResult> {
  // Convert the new handler signature to work with our existing code
  const testMode = variables["testMode"];
  
  const response = await mcpUnity.sendRequest({
    method: resourceName,
    params: {
      testMode
    }
  });
        
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.RESOURCE_FETCH,
      response.message || `Failed to fetch the ${testMode} tests from Unity`
    );
  }
  
  return {
    contents: [{
      uri: `unity://tests/${testMode}`,
      mimeType: resourceMimeType,
      text: JSON.stringify(response, null, 2)
    }]
  };
}