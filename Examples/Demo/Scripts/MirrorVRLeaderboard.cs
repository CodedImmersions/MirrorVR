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

using EpicTransport.Attributes;
using System.Text;
using TMPro;
using UnityEngine;

namespace Mirror.VR.Demo
{
    public class MirrorVRLeaderboard : NetworkBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SyncVar, DoNotBackup] private string lbrawtext;

        public override void OnStartServer() => InvokeRepeating(nameof(SetLB), 0, 0.5f);

        private void SetLB()
        {
            if (MirrorVRManager.PlayerList == null) return;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < MirrorVRManager.PlayerList.Count; i++)
            {
                MirrorVRPlayer player = MirrorVRManager.PlayerList[i];
                if (player.connectionId == 0)
                    sb.AppendLine($"{i + 1}. {player.PlayerName} (Host)");
                else
                    sb.AppendLine($"{i + 1}. {player.PlayerName}");
            }

            lbrawtext = sb.ToString();
        }

        public override void OnStartClient()
        {
            InvokeRepeating(nameof(SyncText), 0, 0.5f);
        }

        private void SyncText() => text.text = lbrawtext;
    }
}
