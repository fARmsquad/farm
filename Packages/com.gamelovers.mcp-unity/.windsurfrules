## 1. Project Structure

- The project consists of two main parts:
  - **`Editor/` (Unity C#/Editor Package)**: A C# Unity Editor package that exposes Unity Editor functionality via a WebSocket bridge.
  - **`Server/` (Node.js/TypeScript)**: A Node.js server implementing the Model Context Protocol (MCP), using the official TypeScript SDK.

## 2. Communication & Protocol

*   **Primary Protocol**: Model Context Protocol (MCP).
    *   The Unity side uses the `websocket-sharp` library: https://github.com/sta/websocket-sharp/tree/master/websocket-sharp/Server
    *   The Node.js server uses the `@modelcontextprotocol/sdk` for protocol implementation and exposes tools/resources/prompts to LLMs and AI clients.
*   **Transport Layer**: WebSockets.
    *   Node.js server (`Server/`) acts as a WebSocket *client* to the WebSocket *server* running within the Unity Editor (`Editor/`).
*   **Message Format**: JSON. MCP defines the structure for tool/resource calls.
    *   Node.js tools typically send a request like: `{ method: "csharp_tool_name", params: { ... } }` to Unity via `mcpUnity.sendRequest()`.
    *   Unity C# tools receive this, execute, and return a `JObject` response.
*   **Response Format**: JSON. MCP defines the structure for tool/resource responses.
    *   Unity C# tools return a `JObject` response.
    *   Node.js tools receive this and return a `JObject` response.
*   **Error Handling**: MCP defines a standard error format.
    *   Unity C# tools return an error object if an exception occurs.
    *   Node.js tools receive this and return an error object.

**Communication Flow**:
- Request: AI LLM Assistant -> Node.js MCP Server -> WebSocket Bridge -> Unity Editor Package -> Unity Editor API
- Response: Unity Editor Package -> WebSocket Bridge -> Node.js MCP Server -> AI LLM Assistant

## 3. Minimum Supported Versions

- Unity: 2022.3 or newer
- Node.js: 18.0.0 or newer

## 4. Key Components

### Unity Editor Package (Editor/)

*   **`UnityBridge/`**:
    *   `McpUnityServer.cs`: **Singleton**. The central nervous system on the Unity side. Initializes WebSocket server, registers tools/resources, and dispatches incoming requests to appropriate handlers.
        *   *LLM Assistant Note*: When adding new tools/resources, they must be registered here in `RegisterTools()` or `RegisterResources()`. Pay attention to how existing tools are instantiated (currently direct `new Tool()`).
    *   `McpUnitySocketHandler.cs`: Handles individual WebSocket client connections, message parsing, and routing requests to `McpUnityServer` for tool/resource execution.
    *   `McpUnityEditorWindow.cs`: // TODO: Complete
*   **`Tools/`**: Contains C# classes inheriting from `McpToolBase`. Each class implements a specific action that can be triggered in Unity.
    *   *LLM Assistant Note*: New tools should follow the `McpToolBase` pattern: define `Name` (matching Node.js tool name), `Description`, `IsAsync`, and override `Execute()` or `ExecuteAsync()`. Use `Undo.RecordObject` for actions that modify the scene or assets.
*   **`Resources/`**: Contains C# classes inheriting from `McpResourceBase` (assumption, or similar base). These provide read-only access to Unity Editor state.
    *   *LLM Assistant Note*: New resources should follow the `McpResourceBase` pattern: define `Name` (matching Node.js resource name), `Description`, and override `Fetch()` or `FetchAsync()`
*   **`Services/`**: Contains classes providing specific functionalities, often designed for dependency injection.
    *   Example: `TestRunnerService.cs`, `ConsoleLogsService.cs`.
    *   *LLM Assistant Note*: Prefer dependency injection for new services. If a tool needs a complex piece of logic, consider abstracting it into a service.
*   **`Utils/`**: Utility classes for common tasks like logging (`McpLogger.cs`), settings management (`McpUnitySettings.cs`), GameObject creation (`GameObjectHierarchyCreator.cs`)

### Node.js Server (Server/)

*   **`src/`**:
    *   `index.ts`: **Entry point**. Initializes the MCP server, registers all MCP tools and resources, and starts the `McpUnity` WebSocket bridge.
        *   *LLM Assistant Note*: New tools/resources implemented in TypeScript must be registered here using `server.tool(...)` or `server.resource(...)`.
    *   `unity/mcpUnity.ts` (or [.js](cci:7://file:///c:/Users/migas/Desktop/mcp-unity/Server~/build/prompts/gameobjectHandlingPrompt.js:0:0-0:0) if compiled): Manages the WebSocket client connection *to* the Unity Editor. Handles sending requests and receiving responses.
    *   **`tools/`**: TypeScript modules defining MCP tools. Each tool module typically:
        *   Defines input/output schemas using `zod`.
        *   Provides a registration function (e.g., `registerMyTool(server, mcpUnity, logger)`).
        *   Implements a `toolHandler` function that interacts with `mcpUnity.sendRequest()` to call the corresponding C# tool in Unity.
        *   *LLM Assistant Note*: Follow the existing pattern for new tools: define clear Zod schemas, structure the request for `mcpUnity.sendRequest` correctly (matching the C# tool's `Name` and expected parameters).
    *   **`resources/`**: TypeScript modules defining MCP resources. Similar structure to tools but for read-only data.
    *   **`prompts/`**: Contains predefined MCP prompts that guide LLMs in using the available tools and resources for specific workflows.
        *   *LLM Assistant Note*: If a new complex workflow emerges, consider adding a prompt here. Prompts should clearly list relevant tools/resources and outline step-by-step procedures.
    *   **`utils/`**: Helper utilities, e.g., `logger.ts`, `errors.ts`.
*   **`package.json`**: Manages Node.js dependencies, scripts for building (`tsc`), running, and debugging, including `@modelcontextprotocol/sdk`, `ws`, `express`, etc.
*   **`tsconfig.json`**: TypeScript compiler configuration.
*   **`build/`**: Output directory for compiled JavaScript files from `src/`.

## 5. Integration & Usage

- The Unity Editor package is designed to be used as a package (via UPM or direct import).
- The Node.js server can be started independently and connects to Unity via WebSocket (port configurable, default 8090).
- The system is designed for use with LLM-based IDEs (e.g., Windsurf, Cursor, Claude Desktop) to enable AI-powered Unity Editor automation and queries.

## 6. Configuration

- Configuration utilities are provided for generating and injecting MCP config into various IDEs (Cursor, Claude Desktop, Windsurf).
- Unity-side settings are persisted in `ProjectSettings/McpUnitySettings.json`.

## 7. Design Patterns & Best Practices

### 7.1. Node.js (Server/ - TypeScript)

*   **Modularity**: Keep tools, resources, and utilities in separate files/modules.
*   **Schema Validation**: Use `zod` extensively for defining and validating the `inputSchema` for all tools and resources. This catches errors early.
*   **Async/Await**: Use `async/await` for all I/O operations, especially calls to Unity.
*   **Error Handling**: Implement robust error handling in tool handlers. Use the `McpUnityError` class for custom errors. Return meaningful error messages to the MCP client.
*   **Logging**: Use the provided `Logger` for comprehensive logging. Log entry/exit points of handlers, parameters, and significant events.
*   **Configuration**: Externalize configuration (e.g., WebSocket ports, though Unity side is primary for port).
*   **MCP SDK Adherence**: Follow best practices for the `@modelcontextprotocol/sdk` when registering tools and resources.

### 7.2. Unity C# (Editor/)

*   **`McpToolBase` / `McpResourceBase`**: Adhere to the established base class patterns for tools and resources.
    *   Ensure `Name` property in C# tools matches the `method` string sent from Node.js.
*   **Single Responsibility Principle (SRP)**: Tools should be focused on a single task. Complex logic can be delegated to services.
*   **Unity API Usage**:
    *   Use `EditorUtility` for tasks like marking objects dirty (`EditorUtility.SetDirty()`), displaying progress bars, etc.
    *   Use `Undo.RecordObject()` before modifying any `UnityEngine.Object` to support undo functionality in the editor. Use `Undo.RegisterCreatedObjectUndo()` for newly created objects.
    *   Be mindful of operations that must run on Unity's main thread. If a tool is `IsAsync = true`, its `ExecuteAsync` method will be marshaled to the main thread.
*   **Immutability**: Prefer immutable data structures where possible, though Unity's API often requires direct object manipulation.
*   **Error Handling**: Return `JObject` responses indicating success or failure, with clear messages. Use `McpUnitySocketHandler.CreateErrorResponse()` or similar utility if available.
*   **Logging**: Use `McpLogger` for consistent logging within Unity.
*   **No Blocking Operations in WebSocket Handlers**: For long-running tasks, tools should be marked `IsAsync = true` and use `ExecuteAsync` to avoid blocking the WebSocket communication thread.
*   **Dependency Injection**: The project shows a trend towards DI (e.g., `TestRunnerService`). Prefer injecting dependencies into services and tools where practical, rather than relying on singletons or static access, to improve testability and maintainability.
    *   If `McpUnityServer` needs to provide these, they should be initialized in its constructor or an `InitializeServices` method and passed down.

## 8. References

- MCP Protocol: https://modelcontextprotocol.io
- TypeScript SDK: https://github.com/modelcontextprotocol/typescript-sdk
- Inspector: https://github.com/modelcontextprotocol/inspector

## 9. Guidelines for LLM assistant Contributions and Conventions

*   **Understand the Flow**: Before adding/modifying, trace the call flow from the Node.js tool/resource definition to the corresponding C# handler.
*   **Consistency**:
    *   **Naming**: Follow existing naming conventions for tools, methods, and parameters (e.g., `camelCase` for JSON/JS/TS, `PascalCase` for C#). Tool names (string identifiers) should be consistent across Node.js and C#.
    *   **Parameter Passing**: If a Node.js tool sends `params.someData`, the C# tool should expect `parameters["someData"]`. For complex objects (like `gameObjectData` or `componentData`), keep them as nested JSON objects.
    *   **Response Structure**: C# tools should return `JObject`s that the Node.js tool handler can easily process and convert into an MCP `CallToolResult`.
*   **Schema First (Node.js)**: When creating a new tool/resource on the Node.js side, define its `zod` schema for parameters first.
*   **C# Implementation Second**: Implement the corresponding C# `McpToolBase` (or resource equivalent). Ensure its `Name` matches what the Node.js tool will send.
*   **Registration**:
    *   Register the Node.js tool/resource/prompt in `Server/src/index.ts`.
    *   Register the C# tool/resource in `Editor/UnityBridge/McpUnityServer.cs`.
*   **Prompts**: If adding a significant new capability or workflow, update or add an MCP prompt in `Server/src/prompts/` to guide users/LLMs.
*   **Error Handling is Key**: Ensure errors are caught and propagated correctly with informative messages at both Node.js and C# levels.
*   **Idempotency**: Where possible, design tools to be idempotent (applying them multiple times with the same input yields the same result). This is not always feasible but is a good goal.
*   **Security/Safety**: Be cautious with tools that modify files or execute arbitrary code. Currently, the scope is within Unity, but general caution is advised.
*   **Testability**: Write code that is testable. DI helps significantly on the C# side.
*   **Conventional Commits**: Follow Conventional Commits for all commit messages. Example: `feat(unity): add new_tool_name for X functionality`.

## 10. Debugging with MCP Inspector

To debug the MCP Node.js server using the Model Context Protocol Inspector, run the following command from the project root:

```shell
npx @modelcontextprotocol/inspector node Server/build/index.js
```

This will launch the MCP Inspector, allowing you to inspect and debug live MCP traffic between the Node.js server and connected clients (such as Unity or LLM AI Assistant IDEs).