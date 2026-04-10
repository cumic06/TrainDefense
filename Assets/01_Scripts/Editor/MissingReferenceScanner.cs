using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainDefense.Editor
{
    public static class MissingReferenceScanner
    {
        public struct ScanResult
        {
            public string AssetPath;
            public string ObjectName;
            public string ComponentName;
            public string PropertyName;
            public Object TargetObject;
        }

        public static List<ScanResult> ScanAll(bool includeNull = false)
        {
            var results = new List<ScanResult>();
            
            // Scan Prefabs
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue; // Skip Packages

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                results.AddRange(ScanGameObjectForMissingReferences(prefab, path, includeNull));
            }

            // Scan Scenes
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var originalActiveScenePath = SceneManager.GetActiveScene().path;
            
            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(scenePath) || !scenePath.StartsWith("Assets/")) continue; // Skip Packages


                // Open the scene to scan
                // Use Single to avoid side effects from multiple loaded scenes (e.g., URP 2D Global Light warnings).
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    results.AddRange(ScanGameObjectForMissingReferences(root, scenePath, includeNull));
                }
            }

            // Restore original scene setup if possible (best effort).
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
            else if (!string.IsNullOrEmpty(originalActiveScenePath))
            {
                EditorSceneManager.OpenScene(originalActiveScenePath, OpenSceneMode.Single);
            }

            return results;
        }

        public static List<ScanResult> ScanGameObjectForMissingReferences(GameObject go, string assetPath, bool includeNull = false)
        {
            var results = new List<ScanResult>();
            var components = go.GetComponentsInChildren<Component>(true);

            foreach (var component in components)
            {
                // Missing Script
                if (component == null)
                {
                    results.Add(new ScanResult
                    {
                        AssetPath = assetPath,
                        ObjectName = "Unknown (Missing Script)",
                        ComponentName = "Missing Script",
                        PropertyName = "Script",
                        TargetObject = go
                    });
                    continue;
                }

                // Check if this is a custom script for 'None' (Null) detection
                bool isCustomScript = false;
                if (includeNull && component is MonoBehaviour mb)
                {
                    MonoScript script = MonoScript.FromMonoBehaviour(mb);
                    if (script != null)
                    {
                        string scriptPath = AssetDatabase.GetAssetPath(script);
                        // Only target scripts under Assets/01_Scripts/ (Custom scripts)
                        if (scriptPath.StartsWith("Assets/01_Scripts/"))
                        {
                            isCustomScript = true;
                        }
                    }
                }

                var serializedObject = new SerializedObject(component);
                var it = serializedObject.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // Check if it's missing (InstanceID != 0 but objectValue == null)
                        bool isMissing = it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0;
                        
                        // Only check isNull if it's a custom script and includeNull is on
                        bool isNull = includeNull && isCustomScript && it.objectReferenceValue == null && it.objectReferenceInstanceIDValue == 0;

                        if (isNull)
                        {
                            // Check if it's marked with ReadOnly attribute (like Odin's [ReadOnly])
                            var field = component.GetType().GetField(it.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                // Check attributes by name to be flexible with custom or plugin ReadOnly attributes
                                var attributes = field.GetCustomAttributes(true);
                                foreach (var attr in attributes)
                                {
                                    if (attr.GetType().Name.Contains("ReadOnly"))
                                    {
                                        isNull = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (isMissing || isNull)
                        {
                            results.Add(new ScanResult
                            {
                                AssetPath = assetPath,
                                ObjectName = component.gameObject.name,
                                ComponentName = component.GetType().Name,
                                PropertyName = it.displayName,
                                TargetObject = component
                            });
                        }

                    }
                }
            }


            return results;
        }

    }
}
