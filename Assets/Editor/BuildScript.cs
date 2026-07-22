using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Headless Android build entry point, invoked from the command line via
//   Unity -batchmode -quit -executeMethod BuildScript.BuildAndroid
// (see Tools/build-android.sh). Produces a signed .aab ready for Google Play.
//
// Keystore passwords are read from the environment so no secret is ever stored
// in the project:
//   CM_KEYSTORE_PASS  - password for the keystore
//   CM_KEYALIAS_PASS  - password for the key alias
// The keystore path and alias themselves already live in Player Settings.
//
// Output path comes from CM_BUILD_OUTPUT (absolute path to the .aab to write).
public static class BuildScript {
  public static void BuildAndroid() {
    var keystorePass = Environment.GetEnvironmentVariable("CM_KEYSTORE_PASS");
    var aliasPass = Environment.GetEnvironmentVariable("CM_KEYALIAS_PASS");
    var output = Environment.GetEnvironmentVariable("CM_BUILD_OUTPUT");

    if (string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(aliasPass)) {
      Fail("CM_KEYSTORE_PASS and CM_KEYALIAS_PASS must both be set in the environment.");
    }

    if (string.IsNullOrEmpty(output)) {
      Fail("CM_BUILD_OUTPUT (absolute path to the .aab to write) must be set.");
    }

    // Sign with the existing upload keystore configured in Player Settings.
    PlayerSettings.Android.useCustomKeystore = true;
    PlayerSettings.Android.keystorePass = keystorePass;
    PlayerSettings.Android.keyaliasPass = aliasPass;

    // Build an App Bundle (.aab), not an APK — Play needs the bundle.
    EditorUserBuildSettings.buildAppBundle = true;

    var scenes = EditorBuildSettings.scenes
      .Where(s => s.enabled)
      .Select(s => s.path)
      .ToArray();

    if (scenes.Length == 0) {
      Fail("No enabled scenes in Build Settings — nothing to build.");
    }

    var options = new BuildPlayerOptions {
      scenes = scenes,
      locationPathName = output,
      target = BuildTarget.Android,
      targetGroup = BuildTargetGroup.Android,
      options = BuildOptions.None
    };

    Debug.Log($"[BuildScript] Building AAB -> {output} " +
              $"(v{PlayerSettings.bundleVersion} code {PlayerSettings.Android.bundleVersionCode}, {scenes.Length} scene(s))");

    var report = BuildPipeline.BuildPlayer(options);
    var summary = report.summary;

    if (summary.result == BuildResult.Succeeded) {
      Debug.Log($"[BuildScript] SUCCESS: {summary.totalSize / (1024 * 1024)} MB in {summary.totalTime.TotalSeconds:F0}s -> {output}");
      EditorApplication.Exit(0);
    }
    else {
      Fail($"Build {summary.result} with {summary.totalErrors} error(s).");
    }
  }

  private static void Fail(string message) {
    Debug.LogError("[BuildScript] " + message);
    EditorApplication.Exit(1);
  }
}
