// 이 파일을 Unity 프로젝트의 Assets/Editor/BuildScript.cs 에 복사하세요.
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildScript
{
    static string[] GetScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);
        return scenes.ToArray();
    }

    static string OutputPath(string fallback)
    {
        return Environment.GetEnvironmentVariable("BUILD_OUTPUT_PATH") ?? fallback;
    }

    public static void BuildAndroid()
    {
        PlayerSettings.Android.useCustomKeystore = false;

        var opts = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = OutputPath("Builds/Android/game.apk"),
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Android 빌드 실패: {report.summary.result}");
    }

    public static void BuildWindows()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = OutputPath("Builds/Windows/game.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Windows 빌드 실패: {report.summary.result}");
    }
}
