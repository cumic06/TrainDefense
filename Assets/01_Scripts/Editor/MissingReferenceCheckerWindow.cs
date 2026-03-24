using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TrainDefense.Editor
{
    public class MissingReferenceCheckerWindow : EditorWindow
    {
        private List<MissingReferenceScanner.ScanResult> _results = new List<MissingReferenceScanner.ScanResult>();
        private Vector2 _scrollPos;
        public const string PREFS_AUTO_BUILD_CHECK = "MissingReferenceChecker_AutoBuildCheck";
        public const string PREFS_INCLUDE_NULL = "MissingReferenceChecker_IncludeNull";

        [MenuItem("Tools/Missing Reference Checker")]

        public static void ShowWindow()
        {
            GetWindow<MissingReferenceCheckerWindow>("Missing Reference Checker");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            bool autoBuildCheck = EditorPrefs.GetBool(PREFS_AUTO_BUILD_CHECK, true);
            bool newAutoBuildCheck = EditorGUILayout.Toggle("Scan Automatically Before Build", autoBuildCheck);
            if (newAutoBuildCheck != autoBuildCheck)
            {
                EditorPrefs.SetBool(PREFS_AUTO_BUILD_CHECK, newAutoBuildCheck);
            }

            bool includeNull = EditorPrefs.GetBool(PREFS_INCLUDE_NULL, false);
            bool newIncludeNull = EditorGUILayout.Toggle("Include 'None' (Null) References", includeNull);
            if (newIncludeNull != includeNull)
            {
                EditorPrefs.SetBool(PREFS_INCLUDE_NULL, newIncludeNull);
            }

            if (GUILayout.Button("Manual Scan (All Prefabs and Scenes)", GUILayout.Height(40)))
            {
                _results = MissingReferenceScanner.ScanAll(newIncludeNull);
            }



            if (_results.Count > 0)
            {
                EditorGUILayout.HelpBox($"Found {_results.Count} missing references!", MessageType.Warning);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (var result in _results)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        EditorGUIUtility.PingObject(result.TargetObject);
                        Selection.activeObject = result.TargetObject;
                    }
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"Path: {result.AssetPath}");
                    EditorGUILayout.LabelField($"Object: {result.ObjectName}");
                    EditorGUILayout.LabelField($"Component: {result.ComponentName}");
                    EditorGUILayout.LabelField($"Property: {result.PropertyName}");
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No missing references found.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }
    }
}
