import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import * as z from "zod";

/**
 * Registers the gameobject handling prompt with the MCP server.
 * This prompt defines the proper workflow for handling GameObjects within the Unity Editor.
 * 
 * @param server The McpServer instance to register the prompt with.
 */
export function registerGameObjectHandlingPrompt(server: McpServer) {
  server.prompt(
    'gameobject_handling_strategy',
    'Defines the proper workflow for handling gameobjects in Unity',
    {
      gameObjectIdOrName: z.string().describe("The resource to identify the GameObject intended to handle. It can be the **instance ID**, the **name** or the **path** to the GameObject."),
    },
    async ({ gameObjectIdOrName }) => ({
      messages: [
        {
          role: 'user', 
          content: {
            type: 'text',
            text: `You are an expert AI assistant integrated with Unity via a MCP server.

When working directly with GameObjects or any of their components in Unity scenes, you have access to the following resources and tools:
- Resource "get_scenes_hierarchy" (unity://scenes_hierarchy) to list all GameObjects.
- Resource "get_gameobject" (unity://gameobject/{idOrName}) to fetch detailed GameObject info, with the *idOrName* being either the **instance ID**, the **name** or the **path** to the GameObject.
- Tool "select_gameobject" to select a GameObject by **instance ID**, the **name** or the **path** of the GameObject.
- Tool "update_gameobject" to update a GameObject's core properties (name, tag, layer, active state, static state), or create the GameObject if it does not exist.
- Tool "update_component" to update or add a component on a GameObject, including common frequently used components (e.g. Transform, RectTransform, BoxCollider, Rigidbody, etc).
- Tool "create_prefab" to create a prefab from a GameObject in the scene with optional MonoBehaviour script and serialized field values.

Workflow:
1. Use "get_scenes_hierarchy" to confirm the GameObject ID, name or path for "${gameObjectIdOrName}".
2. If you need to update the GameObject's core properties (name, tag, layer, active state, static state), or create the GameObject if it does not exist, use "update_gameobject".
3. To focus the Unity Editor on the target GameObject, invoke "select_gameobject".
4. Optionally, use "unity://gameobject/${gameObjectIdOrName}" to retrieve detailed properties.
5. To update or add a component on the GameObject, use "update_component".
6. Confirm success and report any errors.

Guidance:
- Use "update_gameobject" for creating GameObjects in the scene or to change a GameObject's core properties.
- Use "update_component" for adding or modifying components on an existing GameObject in the scene.
- Use "create_prefab" for creating prefabs from GameObjects in the scene.
- Component Scripts must be compiled in the Unity project before using "update_component" or "create_prefab".
- Always validate inputs and request clarification if the identifier is ambiguous.`
          }
        },
        {
          role: 'user',
          content: {
            type: 'text',
            text: `Handle GameObject "${gameObjectIdOrName}" through the above workflow.`
          }
        }
      ]
    })
  );
}
