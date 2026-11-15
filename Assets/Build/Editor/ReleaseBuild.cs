using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SunSwitch.Build
{
    /// <summary>
    /// Sun Switch > Build Game
    ///
    /// Release builds of the game: the scenes in Build Settings (Main first, then Section1-4, as
    /// GameScenesBuilder sets them) into Builds/&lt;version&gt;/&lt;platform&gt;/, with the release
    /// player settings applied first (name, company, bundle id, icon, window, splash). That
    /// folder holds only the game, ready to zip; Burst's debug symbols go next to it.
    ///
    ///   Apply Release Settings   only the player settings and the app icon
    ///   macOS / Windows / Both   settings + a non-development build per platform
    ///
    /// Command line (CI or a terminal):
    ///   Unity -batchmode -quit -projectPath . -executeMethod SunSwitch.Build.ReleaseBuild.BuildFromCommandLine
    ///         [-platforms mac,win] [-buildRoot path]
    /// exits with code 1 if any build fails. Windows needs the "Windows Build Support (Mono)" module
    /// from Unity Hub; without it the Windows build is skipped with a message saying so.
    /// </summary>
    public static class ReleaseBuild
    {
        public const string ProductName = "Sun Switch";
        public const string CompanyName = "Sun Switch Team";
        public const string BundleId = "com.sunswitchteam.sunswitch";
        public const string IconPath = "Assets/Art/UI/AppIcon.png";
        const string DefaultRoot = "Builds";

        [MenuItem("Sun Switch/Build Game/Apply Release Settings", priority = 0)]
        public static void ApplySettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion)) PlayerSettings.bundleVersion = "0.1.0";

            // Opens borderless full screen at the desktop resolution; Alt+Enter / the window
            // controls still work, and a windowed start is 1920 x 1080 and resizable.
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.forceSingleInstance = true;

            // Straight into the game's own Start screen.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.157f, 0.137f, 0.129f);

            var icon = AppIconBaker.Bake(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Sun Switch] Release settings applied: {ProductName} {PlayerSettings.bundleVersion} ({BundleId}).");
        }

        [MenuItem("Sun Switch/Build Game/macOS", priority = 20)]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, DefaultRoot);

        [MenuItem("Sun Switch/Build Game/Windows", priority = 21)]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, DefaultRoot);

        [MenuItem("Sun Switch/Build Game/macOS + Windows", priority = 22)]
        public static void BuildBoth()
        {
            Build(BuildTarget.StandaloneOSX, DefaultRoot);
            Build(BuildTarget.StandaloneWindows64, DefaultRoot);
        }

        /// <summary>Entry point for -executeMethod. See the class summary for the arguments.</summary>
        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string name, string fallback)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
            }
            var platforms = Arg("-platforms", "mac,win").Split(',').Select(p => p.Trim().ToLowerInvariant());
            string root = Arg("-buildRoot", DefaultRoot);
            bool ok = true;
            foreach (var p in platforms)
            {
                if (p == "mac") ok &= Build(BuildTarget.StandaloneOSX, root);
                else if (p == "win") ok &= Build(BuildTarget.StandaloneWindows64, root, skipIfMissing: true);
                else { Debug.LogError($"[Sun Switch] Unknown platform '{p}' (use mac, win)."); ok = false; }
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Builds one platform. Returns true on success (or a skipped missing module when allowed).</summary>
        public static bool Build(BuildTarget target, string root, bool skipIfMissing = false)
        {
            string platform = target == BuildTarget.StandaloneOSX ? "macOS" : "Windows";
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                string message = $"[Sun Switch] {platform} build skipped: install the \"{platform} Build Support (Mono)\" module for this Unity version in Unity Hub.";
                if (skipIfMissing) { Debug.LogWarning(message); return true; }
                Debug.LogError(message);
                return false;
            }

            ApplySettings();
            var scenes = CheckScenes();
            if (scenes == null) return false;

            string version = PlayerSettings.bundleVersion;
            string folder = Path.Combine(root, version, platform);
            string location = Path.Combine(folder, target == BuildTarget.StandaloneOSX ? ProductName + ".app" : ProductName + ".exe");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;
            string line = $"[Sun Switch] {platform} build {summary.result}: {location} ({summary.totalSize / (1024f * 1024f):F0} MB, {summary.totalTime.TotalSeconds:F0} s, {summary.totalErrors} errors)";
            if (ok) Debug.Log(line); else Debug.LogError(line);

            // Burst leaves debug symbols next to the game ("..._DoNotShip"); keep them, but outside the
            // platform folder, so that folder can be zipped and shared as it is.
            foreach (var debug in Directory.GetDirectories(folder, "*_DoNotShip"))
            {
                string symbols = Path.Combine(root, version, platform + " debug symbols");
                if (Directory.Exists(symbols)) Directory.Delete(symbols, true);
                Directory.Move(debug, symbols);
            }
            return ok;
        }

        /// <summary>The enabled Build Settings scenes, checked: Main first, all four sections, no sandboxes.</summary>
        static string[] CheckScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string Problem()
            {
                if (scenes.Length == 0 || Path.GetFileNameWithoutExtension(scenes[0]) != "Main") return "Main.unity must be the first enabled scene";
                for (int i = 1; i <= 4; i++)
                    if (!scenes.Any(s => Path.GetFileNameWithoutExtension(s) == "Section" + i)) return $"Section{i}.unity is not in Build Settings";
                var sandbox = scenes.FirstOrDefault(s => s.Contains("/Sandbox/"));
                if (sandbox != null) return $"{sandbox} is a test scene and shouldn't ship";
                var missing = scenes.FirstOrDefault(s => !File.Exists(s));
                return missing != null ? $"{missing} doesn't exist" : null;
            }
            var problem = Problem();
            if (problem == null) return scenes;
            Debug.LogError($"[Sun Switch] Build stopped: {problem}. Run Sun Switch > Build Game Scenes to set Build Settings.");
            return null;
        }
    }
}
