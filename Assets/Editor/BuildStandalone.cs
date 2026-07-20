using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildStandalone
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/TitleScene.unity",
        "Assets/Scenes/GameScene.unity"
    };

    [MenuItem("Build/Build Standalone Linux")]
    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64, "Builds/FireworksSimulator/FireworksSimulator.x86_64");
    }

    [MenuItem("Build/Build Standalone Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/FireworksSimulatorWindows/FireworksSimulator.exe");
    }

    [MenuItem("Build/Build Standalone macOS")]
    public static void BuildMacOS()
    {
        Build(BuildTarget.StandaloneOSX, "Builds/FireworksSimulatorMac/FireworksSimulator.app");
    }

    public static void BuildLinuxBatch()
    {
        BuildLinux();
    }

    private static void Build(BuildTarget target, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Standalone build failed: " + report.summary.result);
        }
    }
}
