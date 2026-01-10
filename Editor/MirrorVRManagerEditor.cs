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

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace Mirror.VR.Editor
{
    [CustomEditor(typeof(MirrorVRManager))]
    public class MirrorVRManagerEditor : NetworkManagerEditor
    {
        private Texture2D _longerlogo;
        private string version = string.Empty;

        private void OnEnable()
        {
            string path = Path.Combine(Application.dataPath, "MirrorVR", "package.json");
            if (!File.Exists(path))
            {
                Debug.LogError("[MirrorVR] package.json cannot be found. Please make sure you haven't moved the MirrorVR directory, and that it is located at Assets/MirrorVR.");
                return;
            }

            PackageJson pj = JsonConvert.DeserializeObject<PackageJson>(File.ReadAllText(path));
            version = $"{pj.displayName} v{pj.version}";
        }

        public override void OnInspectorGUI()
        {
            MirrorVRManager manager = (MirrorVRManager)target;
            GUIStyle labelstyle = new GUIStyle(GUI.skin.label);

            if (_longerlogo == null)
                _longerlogo = Resources.Load<Texture2D>("MirrorVRLogoLong");

            #region Logo
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(_longerlogo);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            #endregion

            GUILayout.Space(10);

            #region Version
            if (!string.IsNullOrEmpty(version))
            {
                labelstyle.alignment = TextAnchor.MiddleCenter;
                labelstyle.fontStyle = FontStyle.Italic;
                labelstyle.fontSize = 10;
                GUILayout.Label(version, labelstyle);

                GUILayout.Space(5);
            }
            #endregion

            #region Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.color = new Color(100f / 255f, 113f / 255f, 245f / 255f);
            if (GUILayout.Button("Discord", GUILayout.Width(75))) UnityEngine.Application.OpenURL("https://discord.gg/6KCH9xvGUE");

            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            if (GUILayout.Button("GitHub", GUILayout.Width(75))) UnityEngine.Application.OpenURL("https://github.com/CodedImmersions/MirrorVR");

            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("Docs", GUILayout.Width(75))) UnityEngine.Application.OpenURL("https://codedimmersions.gitbook.io/mirrorvr");

            GUI.color = new Color(0f, 0.8f, 0f);
            if (GUILayout.Button("Donate", GUILayout.Width(75))) UnityEngine.Application.OpenURL("https://buymeacoffee.com/codedimmersions");

            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            #endregion

            GUILayout.Space(15);

            labelstyle.alignment = TextAnchor.MiddleCenter;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 18;
            GUILayout.Label("Mirror VR Manager", labelstyle);

            serializedObject.Update();


            string[] excluded = typeof(NetworkManager)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .Select(f => f.Name)
                .Concat(new[] { "m_Script" })
                .ToArray();

            DrawPropertiesExcluding(serializedObject, excluded);
            GUILayout.Space(8);
            DrawUILine(Color.gray);

            EditorGUILayout.Space(10);

            GUILayout.Label("Network Manager", labelstyle);

            foreach (var name in excluded)
            {
                if (name == "m_Script" || name == "spawnPrefabs") continue;

                var property = serializedObject.FindProperty(name);
                if (property != null) EditorGUILayout.PropertyField(property, true);
            }

            #region Network Manager Inspector
#if MIRROR_VR //fixes a bug that wouldn't let this compile because of the changes we made
            base.Init();
            base.spawnList.DoLayoutList();
            if (GUILayout.Button("Populate Spawnable Prefabs")) base.ScanForNetworkIdentities();
#endif
            #endregion

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }

    [Serializable]
    internal class PackageJson
    {
        public string version;
        public string displayName;
    }
}
