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
using UnityEngine;

using TMPro;

using EpicTransport;
using EpicTransport.Attributes;
using Epic.OnlineServices;
using Epic.OnlineServices.Reports;
using System;
using System.Linq;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mirror.VR
{
    [RequireComponent(typeof(HMSkip))]
    [HelpURL("https://codedimmersions.gitbook.io/mirrorvr/manual/components/mirror-vr-player")]
    public class MirrorVRPlayer : NetworkBehaviour
    {
        public static MirrorVRPlayer Player { get; private set; }

        [Header("Object References")]
        [SerializeField] private Transform headRef;
        [SerializeField] private Transform bodyRef;
        [SerializeField] private Transform leftRef;
        [SerializeField] private Transform rightRef;
        [Space]
        public TMP_Text nameText;


        [Header("Player Info"), Space(15)]
        [SyncVar(hook = nameof(OnPlayerNameChanged)), DoNotBackup] internal string playerName = string.Empty;

        [Header("Cosmetics")]
        [SerializeField] private List<CosmeticSlot> cosmeticSlots = new List<CosmeticSlot>();
        [Space(15)]

        private readonly SyncList<Cosmetic> cosmeticsData = new SyncList<Cosmetic>();


        [Header("Color")]
        [SerializeField] private List<Renderer> TargetMesh = new List<Renderer>();
        [SyncVar, DoNotBackup] private Color playerColor;


        [SyncVar, DoNotBackup] private double clientrtt;

        public string PlayerName => playerName;

        public Color PlayerColor => playerColor;

        /// <summary>
        /// The ProductUserId of this client
        /// </summary>
        public string ProductUserID => puid;

        /// <summary>
        /// The identifier of this client in the current lobby
        /// </summary>
        [HideInInspector] public int PlayerLobbyId;

        /// <returns>The current player's ping in milliseconds</returns>
        public double RTT { get { return Math.Round(clientrtt * 1000); } }

        [SyncVar, HideInInspector] public int connectionId;
        [SyncVar] internal string puid;

        private readonly SyncDictionary<string, string> customProperties = new SyncDictionary<string, string>();
#if UNITY_EDITOR
        private List<string> customPropertiesDisplay = new List<string>();
#endif

        private MirrorVRManager manager;
        private Transform root;

        public override void OnStartLocalPlayer()
        {
            MirrorVRManager.LocalPlayer = this;
            manager = MirrorVRManager.instance;
            Player = this;

            string savedcolor = PlayerPrefs.GetString("PlayerColor");

            if (string.IsNullOrEmpty(savedcolor)) playerColor = manager.defaultColor;
            else playerColor = JsonUtility.FromJson<Color>(savedcolor);

            MirrorVRManager.GetEquippedCosmetics(cosmetics => { for (int i = 0; i < cosmetics.Count; i++) { ToggleCosmetic(cosmetics[i].slot, cosmetics[i].name, true); } });

            string pf = Application.platform switch
            {
                RuntimePlatform.Android => "Meta Quest",

                RuntimePlatform.WindowsPlayer => "SteamVR",
                RuntimePlatform.LinuxPlayer => "SteamVR",

                RuntimePlatform.WindowsEditor => "Unity Editor",
                RuntimePlatform.OSXEditor => "Unity Editor",
                RuntimePlatform.LinuxEditor => "Unity Editor",

                _ => Application.platform.ToString()
            };

            EOSTransport.UpdateMemberAttribute(new Epic.OnlineServices.Lobby.AttributeData() { Key = MirrorVRManager.PlatformPropertyKey, Value = pf });
            SetCustomProperty(MirrorVRManager.PlatformPropertyKey, pf);

            CmdUpdateValues(EOSManager.DisplayName, playerColor);
        }

        public override void OnStopLocalPlayer()
        {
            MirrorVRManager.LocalPlayer = null;
            Player = null;
        }

        public override void OnStartClient()
        {
            InvokeRepeating(nameof(Tick), 0, 0.2f);
            cosmeticsData.OnChange += OnCosmeticChanged;
            DontDestroyOnLoad(this);

            root = headRef.parent;
        }

        public override void OnStartServer()
        {
            puid = EOSManager.LocalUserProductIDString;
            connectionId = connectionToClient.connectionId;
        }

        private void OnDestroy() => cosmeticsData.OnChange -= OnCosmeticChanged;

        [Command]
        private void CmdUpdateValues(string pname, Color pcolor, NetworkConnectionToClient sender = null)
        {
            playerName = pname;
            playerColor = pcolor;
            if (sender != NetworkServer.localConnection) puid = connectionToClient.address;
        }

        [Command]
        private void CmdUpdateProperties(string key, string value)
        {
            if (customProperties.ContainsKey(key)) customProperties[key] = value;
            else customProperties.Add(key, value);
        }

        private void Tick()
        {
            if (isLocalPlayer) CmdUpdateValues(EOSManager.DisplayName, playerColor);

            nameText.text = playerName;
            name = $"Player ({playerName})";
            if (TargetMesh.Count > 0)
            {
                for (int i = 0; i < TargetMesh.Count; i++)
                {
                    var tar = TargetMesh[i];
                    tar.material.color = playerColor;
                }
            }

            if (isServer) clientrtt = netIdentity.connectionToClient.rtt;
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                headRef.position = manager.head.transform.position;
                headRef.rotation = manager.head.transform.rotation;

                bodyRef.position = new Vector3(manager.head.transform.position.x, manager.head.transform.position.y - 0.35f, manager.head.transform.position.z);
                bodyRef.rotation = new Quaternion(0, manager.head.transform.rotation.y, 0, manager.head.transform.rotation.w);

                leftRef.position = manager.leftHand.transform.position;
                leftRef.rotation = manager.leftHand.transform.rotation;

                rightRef.position = manager.rightHand.transform.position;
                rightRef.rotation = manager.rightHand.transform.rotation;

                while (MirrorVRManager.PlayerList == null) return;

                for (int i = 0; i < MirrorVRManager.PlayerList.Count; i++)
                {
                    if (MirrorVRManager.PlayerList[i] == Player) PlayerLobbyId = i;
                }
            }

#if UNITY_EDITOR
            customPropertiesDisplay.Clear();
            foreach (string key in customProperties.Keys) { customPropertiesDisplay.Add($"Key: '{key}', Value: '{customProperties[key]}'"); }
#endif
        }

        private void OnPlayerNameChanged(string _old, string _new) => nameText.text = _new;

        internal void SetPlayerColor(Color color)
        {
            if (isLocalPlayer)
            {
                playerColor = color;
                PlayerPrefs.SetString("PlayerColor", JsonUtility.ToJson(color));
                CmdUpdateValues(EOSManager.DisplayName, color);
            }
        }

        internal void ToggleCosmetic(string category, string cosmeticName, bool forceOn = false)
        {
            if (!isLocalPlayer) return;

            CmdToggleCosmetic(category, cosmeticName, forceOn);
        }

        [Command]
        private void CmdToggleCosmetic(string category, string cosmeticName, bool forceOn, NetworkConnectionToClient sender = null)
        {
            var slot = cosmeticSlots.FirstOrDefault(s => s.name == category);
            var cos = slot.slotObject.Find(cosmeticName);

            if (string.IsNullOrEmpty(slot.name) || string.IsNullOrEmpty(cos.name))
            {
                MirrorVRLogger.LogError($"slot '{category}' or cosmetic '{cosmeticName}' not found");
                return;
            }

            if (forceOn)
            {
                cosmeticsData.RemoveAll(c => c.slot == category);
                cosmeticsData.Add(new Cosmetic() { name = cosmeticName, slot = category });

                SaveCosmetics(sender);
                return;
            }

            if (cosmeticsData.Any(c => c.name == cosmeticName) )
            {
                cosmeticsData.RemoveAll(c => c.name == cosmeticName);
            }
            else
            {
                cosmeticsData.RemoveAll(c => c.slot == category);
                cosmeticsData.Add(new Cosmetic() { name = cosmeticName, slot = category });
            }

            SaveCosmetics(sender);
        }

        private void OnCosmeticChanged(SyncList<Cosmetic>.Operation op, int index, Cosmetic value)
        {
            CosmeticSlot slot = cosmeticSlots.Find(x => x.name == value.slot);
            if (slot.slotObject == null)
            {
                Debug.LogWarning($"slot object for key '{value.slot}' is null");
                return;
            }

            switch (op)
            {
                case SyncList<Cosmetic>.Operation.OP_ADD:
                case SyncList<Cosmetic>.Operation.OP_SET:
                case SyncList<Cosmetic>.Operation.OP_INSERT:

                    var cosObject = slot.slotObject.Find(value.name);
                    if (cosObject == null)
                    {
                        MirrorVRLogger.LogWarn($"cosmetic '{value.name}' not found in slot '{value.slot}'");
                        return;
                    }

                    cosObject.gameObject.SetActive(true);
                    break;

                case SyncList<Cosmetic>.Operation.OP_REMOVEAT:
                case SyncList<Cosmetic>.Operation.OP_CLEAR:

                    var cosObj = slot.slotObject.Find(value.name);
                    if (cosObj == null)
                    {
                        MirrorVRLogger.LogWarn($"cosmetic '{value.name}' not found in slot '{value.slot}'");
                        return;
                    }

                    cosObj.gameObject.SetActive(false);
                    break;
            }
        }

        [TargetRpc]
        private void SaveCosmetics(NetworkConnectionToClient target)
        {
            MirrorVRLogger.LogInfo("Saving cosmetics...");

            if (MirrorVRManager.CurrentInventoryService == InventoryService.PlayerDataStorage) MirrorVRDataStorage.PlayerDataStorageWriteFile("MirrorVREquippedCosmetics.json", JsonConvert.SerializeObject(cosmeticsData.ToList()));
            else
            {
                if (MirrorVRManager.instance.dataService != null) MirrorVRManager.instance.dataService.SetEquippedCosmetics(cosmeticsData.ToList());
                else MirrorVRLogger.LogError("cannot save cosmetics: not custom data provider set!");
            }
        }

        public Result ReportPlayer(PlayerReportsCategory category) => ReportPlayer(category, null);

        public Result ReportPlayer(PlayerReportsCategory category, string message)
        {
            ReportsInterface ri = EOSManager.GetReportsInterface();

            SendPlayerBehaviorReportOptions options = new SendPlayerBehaviorReportOptions()
            {
                ReportedUserId = ProductUserId.FromString(puid),
                ReporterUserId = EOSManager.LocalUserProductID,
                Category = category,
                Message = message,
            };

            Result r = Result.TimedOut;
            ri.SendPlayerBehaviorReport(ref options, null, (ref SendPlayerBehaviorReportCompleteCallbackInfo cb) => { r = cb.ResultCode; });
            return r;
        }


        /// <summary>
        /// Kicks this player from the lobby.
        /// </summary>
        /// <remarks>For the server only.</remarks>
        [Server]
        public void KickPlayer()
        {
            connectionToClient.Disconnect();
        }


        /// <summary>
        /// Sets a custom property for this player.
        /// </summary>
        /// <remarks>For the local player only.</remarks>
        /// <param name="key">The key for the custom property.</param>
        /// <param name="value">The value of the custom property.</param>
        public void SetCustomProperty(string key, string value)
        {
            if (!isLocalPlayer)
            {
                MirrorVRLogger.LogWarn($"cannot set property, player is not local player!");
                return;
            }

            CmdUpdateProperties(key, value);
        }

        /// <summary>
        /// Attempts to get a custom property from this player.
        /// </summary>
        /// <param name="key">The key of the custom propery to attempt to search for.</param>
        /// <param name="value">The value of the custom property. <see langword="null"/> if not found, otherwise has the value if found.</param>
        /// <returns><see langword="true"/> if the custom property was found, and <see langword="false"/> if not found.</returns>
        public bool TryGetCustomProperty(string key, out string value)
        {
            if (customProperties.TryGetValue(key, out string val))
            {
                value = val;
                return true;
            }
            else
            {
                MirrorVRLogger.LogWarn($"cannot get property, property does not exist!");
                value = null;
                return false;
            }
        }
    }

    [Serializable]
    public struct CosmeticSlot
    {
        public string name;
        public Transform slotObject;
    }

    [Serializable]
    public struct Cosmetic
    {
        public Cosmetic(string category, string name)
        {
            this.slot = category;
            this.name = name;
        }

        public string name;
        public string slot;

        public override bool Equals(object obj)
        {
            if (obj is Cosmetic other) return slot == other.slot && name == other.name;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(slot, name);
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(MirrorVRPlayer))]
    public class MirrorVRPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MirrorVRPlayer player = (MirrorVRPlayer)target;

            base.OnInspectorGUI();

            if (Application.isPlaying && MirrorVRManager.ConnectedToLobby)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

                Rect rect = EditorGUILayout.GetControlRect(false, 55);
                rect.width -= 200;

                EditorGUI.HelpBox(rect, $"Name: {player.PlayerName}\n" +
                    $"Color: #{ColorUtility.ToHtmlStringRGB(player.PlayerColor)}\n" +
                    $"PUID: {player.ProductUserID}\n" +
                    $"Connection ID: {player.connectionId}", MessageType.None);
            }
        }
    }
#endif
}