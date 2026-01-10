/*
* MIT License
* Copyright (c) 2025-2026 Coded Immersions
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Mirror.VR.Editor
{
    public class MirrorVREditorHelper
    {
        [InitializeOnLoadMethod]
        [UnityEditor.Callbacks.DidReloadScripts]
        public static void OnLoad()
        {
            #region Scripting Define Symbols

            AddSymbolsForTarget(BuildTargetGroup.Standalone);
            AddSymbolsForTarget(BuildTargetGroup.Android);

            #endregion

            #region Logging
            
            if (!SessionState.GetBool("MirrorVR Startup Log", false))
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "<color=grey>[MirrorVR]</color> Thanks for using MirrorVR! | Discord: <u>https://discord.gg/6KCH9xvGUE</u> | GitHub: <u>https://github.com/CodedImmersions/MirrorVR</u>\nPlease consider donating to support the developers and development of MirrorVR. <u>https://buymeacoffee.com/codedimmersions</u>");
                SessionState.SetBool("MirrorVR Startup Log", true);
            }

            #endregion

            #region Edit NetworkManagerEditor

            string[] paths = Directory.GetFiles(Path.Combine(Application.dataPath, "Mirror", "Editor"), "NetworkManagerEditor.cs", SearchOption.AllDirectories);
            if (paths.Length > 0)
            {
                string nme = paths[0];
                string nmecontents = File.ReadAllText(nme);

                if (!nmecontents.Contains("protected void ScanForNetworkIdentities()"))
                {
                    nmecontents = nmecontents.Replace("void ScanForNetworkIdentities()", "protected void ScanForNetworkIdentities()");
                    File.WriteAllText(nme, nmecontents);
                }

                if (!nmecontents.Contains("protected ReorderableList spawnList"))
                {
                    nmecontents = nmecontents.Replace("ReorderableList spawnList", "protected ReorderableList spawnList");
                    File.WriteAllText(nme, nmecontents);
                }
            }
            else
            {
                throw new FileNotFoundException("MirrorVR cannot make the necessary changes to NetworkManagerEditor.cs because it cannot be found. Please make sure it is located at Assets/Mirror/Editor/NetworkManagerEditor.cs.");
            }

            #endregion
        }

        #region Helper Methods
        private static void CopyDir(string srcdir, string targetdir)
        {
            foreach (string file in Directory.GetFiles(srcdir))
            {
                if (file.EndsWith(".meta")) continue; //fix to remove GUID conflicts

                string targetfile = Path.Combine(targetdir, Path.GetFileName(file));
                if (File.Exists(targetfile)) { if (SameFile(file, targetfile)) continue; }

                File.Copy(file, targetfile, true);
            }
        }

        private static bool SameFile(string file1, string file2)
        {
            if (!File.Exists(file1) || !File.Exists(file2)) return false;

            using (var sha = SHA256.Create())
            {
                byte[] hash1 = sha.ComputeHash(File.ReadAllBytes(file1));
                byte[] hash2 = sha.ComputeHash(File.ReadAllBytes(file2));

                for (int i = 0; i < hash1.Length; i++) { if (hash1[i] != hash2[i]) return false; }

                return true;
            }
        }
        #endregion

        private static void AddSymbolsForTarget(BuildTargetGroup target)
        {
            var defines = new HashSet<string>(GetCurrentDefines(target).Split(';'))
            {
                "MIRROR_VR",
                "MIRROR_VR_EA", //remove 2026-03-15
                "MIRROR_VR_EA_OR_LATER", //remove 2026-03-15
                "MIRROR_VR_1", //remove 2027-01-10
                "MIRROR_VR_1_OR_LATER" //remove 2027-01-10
            };

            if (Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache"), "Oculus.Platform.asmdef", SearchOption.AllDirectories).Length > 0) defines.Add("META_XR_SDK");
            if (Directory.GetFiles(Application.dataPath, "PlayFab.asmdef", SearchOption.AllDirectories).Length > 0) defines.Add("PLAYFAB");

            if (!BuildPipeline.isBuildingPlayer && target != BuildTargetGroup.Android)
            {
                if (Directory.GetFiles(Directory.GetCurrentDirectory(), "com.rlabrecque.steamworks.net.asmdef", SearchOption.AllDirectories).Length > 0) defines.Add("STEAMWORKS_NET");
            }

            SetDefines(target, defines);
        }

        private static string GetCurrentDefines(BuildTargetGroup target)
        {
#if UNITY_2021_2_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target));
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
        }

        private static void SetDefines(BuildTargetGroup target, HashSet<string> defines)
        {
            string newdefines = string.Join(";", defines);
            string currentDefines = GetCurrentDefines(target);

            if (newdefines != currentDefines)
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target), newdefines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
#endif
            }
        }
    }
}
