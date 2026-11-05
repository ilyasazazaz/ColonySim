using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SimpleColony;
public static class ColonyBuild
{
    [MenuItem("Simple Colony/Validate simulation")]
    public static void Validate() { for (int seed = 0; seed < 30; seed++) { var w = new World(seed); if (w.Edges() != 37) throw new System.Exception("Edges"); foreach (var n in w.Nodes) if (n.Links.Count > 4) throw new System.Exception("Degree"); for (int i = 0; i < 30; i++) w.Nodes[i].Known = true; for (int i = 0; i < 30; i++) if (w.Path(14, i, true) < 0) throw new System.Exception("Connectivity"); for (int t = 0; t < 12000; t++) w.Step(); foreach (var n in w.Nodes) if (float.IsNaN(n.Stock) || n.Stock < 0) throw new System.Exception("Invalid resource"); } Debug.Log("COLONY VALIDATION PASSED"); }
    [MenuItem("Simple Colony/Build Android APK")]
    public static void Android() { PlayerSettings.companyName = "ColonySim"; PlayerSettings.productName = "Simple Colony"; PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.colonysim.simplecolony"); PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft; EditorUserBuildSettings.buildAppBundle = false; System.IO.Directory.CreateDirectory("Builds"); var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = "Builds/SimpleColony.apk", target = BuildTarget.Android, options = BuildOptions.Development }); if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("APK build failed: " + report.summary.result); }
}
