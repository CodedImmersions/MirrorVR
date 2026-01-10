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

using EpicTransport;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.VR.Demo
{
    public class MirrorVRDemoUIManager : MonoBehaviour
    {
        [Header("Name")]
        public TMP_InputField nameInput;

        [Header("Lobbies")]
        public Button joinCreateButton;
        public Button joinRandomButton;
        public Button disconnectButton;

        [Space]

        public TMP_InputField lobbyNameInput;

        [Header("Colors")]
        public Slider rSlider;
        public Slider gSlider;
        public Slider bSlider;


        private void Start()
        {
            EOSTransport.OnJoinedLobby += OnJoin;
            EOSTransport.OnLeftLobby += OnLeave;
        }

        private void OnDestroy()
        {
            EOSTransport.OnJoinedLobby -= OnJoin;
            EOSTransport.OnLeftLobby -= OnLeave;
        }

        #region Name
        public void SetName() => MirrorVRManager.SetUsername(nameInput.text ?? "MirrorVR Demo Player");
        #endregion


        #region Lobbies
        public void JoinOrCreate() => MirrorVRManager.JoinOrCreateLobby(lobbyNameInput.text ?? "MirrorVR-Demo");

        public void JoinRandom() => MirrorVRManager.JoinRandomLobby();

        public void Disconnect() => MirrorVRManager.Disconnect();
        #endregion


        #region Color
        public void SetColor() => MirrorVRManager.SetColor(new Color(rSlider.value, gSlider.value, bSlider.value));
        #endregion

        private void OnJoin(string lobbyId)
        {
            joinCreateButton.interactable = false;
            joinRandomButton.interactable = false;
            disconnectButton.interactable = true;
        }

        private void OnLeave()
        {
            joinCreateButton.interactable = true;
            joinRandomButton.interactable = true;
            disconnectButton.interactable = false;
        }
    }
}
