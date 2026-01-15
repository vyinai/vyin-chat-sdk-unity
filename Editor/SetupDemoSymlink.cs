using UnityEditor;
using UnityEngine;
using System.IO;

namespace VyinChatSdk.Editor
{
    public static class SetupDemoSymlink
    {
        private const string SymlinkPath = "Assets/VyinChatSDKDemo";
        private const string TargetPath = "Assets/VyinChatSDK/Samples~/Demo";

        [MenuItem("VyinChat/Setup Demo Symlink")]
        public static void CreateSymlink()
        {
            if (Directory.Exists(SymlinkPath) || File.Exists(SymlinkPath))
            {
                Debug.Log("VyinChatSDKDemo already exists.");
                return;
            }

            var fullSymlinkPath = Path.GetFullPath(SymlinkPath);
            var fullTargetPath = Path.GetFullPath(TargetPath);

            if (!Directory.Exists(fullTargetPath))
            {
                Debug.LogError($"Target path does not exist: {fullTargetPath}");
                return;
            }

#if UNITY_EDITOR_WIN
            // Windows: use mklink /D
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c mklink /D \"{fullSymlinkPath}\" \"{fullTargetPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
#else
            // macOS/Linux: use ln -s
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/bin/ln";
            process.StartInfo.Arguments = $"-s \"{fullTargetPath}\" \"{fullSymlinkPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
#endif

            AssetDatabase.Refresh();
            Debug.Log($"Created symlink: {SymlinkPath} -> {TargetPath}");
        }
    }
}
