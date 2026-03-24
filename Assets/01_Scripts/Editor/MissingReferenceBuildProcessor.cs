using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TrainDefense.Editor
{
    public class MissingReferenceBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!EditorPrefs.GetBool(MissingReferenceCheckerWindow.PREFS_AUTO_BUILD_CHECK, true))
            {
                Debug.Log("[MissingReferenceBuildProcessor] Automatic check is disabled in Tools/Missing Reference Checker.");
                return;
            }

            Debug.Log("[MissingReferenceBuildProcessor] Scanning for missing references before build...");

            bool includeNull = EditorPrefs.GetBool(MissingReferenceCheckerWindow.PREFS_INCLUDE_NULL, false);
            var results = MissingReferenceScanner.ScanAll(includeNull);

            
            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    Debug.LogError($"[MissingReference] Broken reference in {result.AssetPath}: Object '{result.ObjectName}', Component '{result.ComponentName}', Property '{result.PropertyName}'");
                }
                
                // Usually we want to stop the build if there are critical errors.
                // In Unity, throwing an exception in OnPreprocessBuild will stop the build process.
                // However, some projects might prefer a warning. 
                // Given the user's request for '자동 검사', I'll halt the build with an error.
                
                if (EditorUtility.DisplayDialog("Missing References Found!", 
                    $"Found {results.Count} missing references. Build will be canceled. Check console for details.", 
                    "OK"))
                {
                    // Throwing exception to halt build
                    throw new BuildPlayerWindow.BuildMethodException($"Build halted: Found {results.Count} missing references.");
                }
            }
            else
            {
                Debug.Log("[MissingReferenceBuildProcessor] No missing references found. Proceeding with build.");
            }
        }
    }
}
