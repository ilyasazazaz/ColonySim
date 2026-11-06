using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SimpleColony;
public static class ColonyBuild
{
    [MenuItem("Simple Colony/Validate simulation")]
    public static void Validate()
    {
        SimulationValidation.Run();
        Debug.Log("COLONY VALIDATION PASSED");
    }
    [MenuItem("Simple Colony/Build Android APK")]
    public static void Android() { PlayerSettings.companyName = "ColonySim"; PlayerSettings.productName = "Simple Colony"; PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.colonysim.simplecolony"); PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft; EditorUserBuildSettings.buildAppBundle = false; System.IO.Directory.CreateDirectory("Builds"); var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = "Builds/SimpleColony.apk", target = BuildTarget.Android, options = BuildOptions.Development }); if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("APK build failed: " + report.summary.result); }
}
