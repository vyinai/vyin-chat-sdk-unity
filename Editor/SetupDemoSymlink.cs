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

            bool success = CreateSymlinkInternal(fullSymlinkPath, fullTargetPath);

            if (success)
            {
                AssetDatabase.Refresh();
                Debug.Log($"Created symlink: {SymlinkPath} -> {TargetPath}");
            }
        }

        private static bool CreateSymlinkInternal(string symlinkPath, string targetPath)
        {
#if UNITY_EDITOR_WIN
            return CreateSymlinkWindows(symlinkPath, targetPath);
#else
            return CreateSymlinkUnix(symlinkPath, targetPath);
#endif
        }

#if UNITY_EDITOR_WIN
        private static bool CreateSymlinkWindows(string symlinkPath, string targetPath)
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c mklink /D \"{symlinkPath}\" \"{targetPath}\"";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas"; // Request Administrator privileges
                process.StartInfo.CreateNoWindow = true;

                try
                {
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.LogError($"Failed to create symlink. Exit code: {process.ExitCode}. Make sure you have Administrator privileges.");
                        return false;
                    }

                    return true;
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    if (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
                    {
                        Debug.LogWarning("Symlink creation cancelled. Administrator privileges are required.");
                    }
                    else
                    {
                        Debug.LogError($"Failed to create symlink: {ex.Message}");
                    }
                    return false;
                }
            }
        }
#else
        private static bool CreateSymlinkUnix(string symlinkPath, string targetPath)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/bin/ln";
            process.StartInfo.Arguments = $"-s \"{targetPath}\" \"{symlinkPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;

            try
            {
                process.Start();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"Failed to create symlink: {error}");
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create symlink: {ex.Message}");
                return false;
            }
        }
#endif
    }
}
