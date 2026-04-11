using System;
using UnityEngine;
using UnityEditor; // Required for Undo operations

namespace McpUnity.Utils
{
    public static class GameObjectHierarchyCreator
    {
        public static GameObject FindOrCreateHierarchicalGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("GameObject path cannot be null or empty.", nameof(path));
            }

            path = path.Trim('/');
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("GameObject path cannot consist only of slashes.", nameof(path));
            }

            string[] parts = path.Split('/');
            GameObject currentParent = null;
            GameObject foundOrCreatedObject = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string name = parts[i];
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"Invalid path: empty segment at part {i + 1} in path '{path}'. Ensure segments are not empty.");
                }

                Transform childTransform;
                if (currentParent == null)
                {
                    GameObject rootObj = GameObject.Find(name);
                    childTransform = rootObj?.transform;
                }
                else
                {
                    childTransform = currentParent.transform.Find(name);
                }

                if (childTransform == null)
                {
                    GameObject newObj = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(newObj, $"Create {name}");
                    if (currentParent != null)
                    {
                        newObj.transform.SetParent(currentParent.transform, false);
                    }
                    foundOrCreatedObject = newObj;
                    currentParent = newObj;
                }
                else
                {
                    foundOrCreatedObject = childTransform.gameObject;
                    currentParent = foundOrCreatedObject;
                }
            }

            if (foundOrCreatedObject == null)
            {
                throw new InvalidOperationException($"Failed to find or create GameObject for path '{path}'. This indicates an unexpected state.");
            }

            return foundOrCreatedObject;
        }
    }
}
