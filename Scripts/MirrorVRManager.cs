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

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;


using UnityEngine;
using UnityEngine.Networking;

using kcp2k;
using Mirror.Transports.Encryption;

using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Sanctions;

using Mirror.VR.Attributes;

using Newtonsoft.Json;

using Application = UnityEngine.Application;
using AttributeData = Epic.OnlineServices.Lobby.AttributeData;

#if UNITY_SERVER
using Mirror.VR.Server;
#endif

#if META_XR_SDK
using Oculus.Platform;
using Oculus.Platform.Models;
#endif

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
using System.IO;
using Epic.OnlineServices.Connect;
using Steamworks;
#endif

//all of the things related to the dedicated server system are now hidden from the inspector
namespace Mirror.VR
{
    [DisallowMultipleComponent]
    [HelpURL("https://codedimmersions.gitbook.io/mirrorvr/manual/components/mirror-vr-manager")]
    [AddComponentMenu("MirrorVR/Mirror VR Manager")]

    public sealed class MirrorVRManager : NetworkManager
    {
        public static MirrorVRManager instance { get; private set; }

        [Header("Player Transforms")]
        [Tooltip("The player head, usually the Main Camera.")]
        public Transform head;
        [Tooltip("The player's left hand, usually the Left Hand Controller.")]
        public Transform leftHand;
        [Tooltip("The player's right hand, usually the Right Hand Controller.")]
        public Transform rightHand;

        [Header("API Keys")]
        [SerializeField, PasswordField] private string productName;
        [SerializeField, PasswordField] private string productId;
        [SerializeField, PasswordField] private string sandboxId;
        [SerializeField, PasswordField] private string deploymentId;
        [SerializeField, PasswordField] private string clientId;
        [SerializeField, PasswordField] private string clientSecret;
        [SerializeField, PasswordField, Tooltip("Used to encrypt player data storage")] private string encryptionKey;

        [Header("Settings")]
        [SerializeField] private LogLevel loggerLevel = LogLevel.Warn;
        //[SerializeField] private MirrorVRConnectionType connectionType = MirrorVRConnectionType.P2P;
        private MirrorVRConnectionType connectionType = MirrorVRConnectionType.P2P;

        [Space(10)]

        [SerializeField, HideFieldIf("isUsingP2P", "true")] private P2PSettings P2PSettings = new P2PSettings();
        internal DedicatedServerSettings dedicatedServerSettings = new DedicatedServerSettings();

        [Space(20)]

        [Tooltip("If the user should connect to a room on start.")] public bool automaticallyJoinRoom = false;
        [Tooltip("The default limit of users in a room, the max you can do using EOS Lobbies is 64."), Range(1, 64)] public int roomLimit = 10;
        
        [SerializeField] private string randomUsernamePrefix = "Player";
        [Space]
        [SerializeField] internal Color defaultColor = Color.black;

        [SerializeField] private InventoryService inventoryService = InventoryService.PlayerDataStorage;

        [Space]

        [Header("Authentication Types")]
        [SerializeField] private MirrorVRAuthType windowsEditor = MirrorVRAuthType.Oculus;
        [SerializeField] private MirrorVRAuthType macOSEditor = MirrorVRAuthType.EpicPortal;
        [SerializeField] private MirrorVRAuthType linuxEditor = MirrorVRAuthType.EpicPortal;
        [Space]
        [SerializeField] private MirrorVRAuthType android = MirrorVRAuthType.Oculus;
        [SerializeField] private MirrorVRAuthType windowsRuntime = MirrorVRAuthType.DeviceId;
        [SerializeField] private MirrorVRAuthType macOSRuntime = MirrorVRAuthType.DeviceId;
        [SerializeField] private MirrorVRAuthType linuxRuntime = MirrorVRAuthType.DeviceId;

        [Header("--BETA FEATURES--")]
        [Space]

        [Header("Host Migration (EXPERIMENTAL)")]
        [Tooltip("Enable this if you want to use the host migration feature. (When the original host disconnects, a new host is assigned instead of the whole lobby disconnecting.)")]
        [SerializeField] internal bool hostMigration = false;

        [Tooltip("The way that the next host is selected.\n\nPlayer Order: Always selects the 2nd person down in the player list.\n\nRound Trip Time: Selects the next player based on who has the lowest RTT.")]
        [SerializeField, HideFieldIf("hostMigration", "true")] internal HostMigrationPlayerSelection playerSelection;

        internal static ushort CommsPort => instance.dedicatedServerSettings.commsPort;

        public static MirrorVRPlayer LocalPlayer { get; internal set; }
        public static bool ConnectedToLobby => EOSTransport.ConnectedToLobby;
        public static List<MirrorVRPlayer> PlayerList { get; internal set; } = new List<MirrorVRPlayer>();

        //Attributes

        /// <summary>
        /// Gets the maximum number of players allowed in the current lobby.
        /// </summary>
        public static int RoomLimit => instance.roomLimit;

        /// <summary>
        /// The EOS attribute key to tell MirrorVR that the current room is a public lobby or not.
        /// </summary>
        public const string PublicRoomKey = "PublicRoom";

        /// <summary>
        /// The EOS attribute key to tell MirrorVR the current VR platform.
        /// </summary>
        public const string PlatformPropertyKey = "Platform";

        /// <summary>
        /// The current version of MirrorVR.
        /// </summary>
        public const string Version = "1.0.0";


        //Data
        internal ICustomDataService dataService;


        #region Public Variables
        /// <summary>
        /// Gets whether MirrorVR has been initialized or not.
        /// </summary>
        public static bool Initialized => EOSManager.Initialized;

        /// <summary>
        /// Gets if the player is either an active client or an active host.
        /// </summary>
        public static bool ClientActive => NetworkClient.active;

        /// <summary>
        /// Gets if the player is either an active server or an active host.
        /// </summary>
        public static bool ServerActive => NetworkServer.active;
        public static NetworkManagerMode NetworkState => singleton.mode;

        public static ConnectionQuality ConnectionQuality => NetworkClient.connectionQuality;

        /// <returns>The current player's ping in milliseconds.</returns>
        [Obsolete("Use MirrorVRManager.RTT / 2 instead.")]
        public static double Ping => Math.Round((NetworkTime.rtt / 2) * 1000);

        /// <returns>The current player's round trip time in milliseconds.</returns>
        public static double RTT => Math.Round((NetworkTime.rtt) * 1000);

        /// <returns>True if host migration is enabled on this client.</returns>
        public static bool HostMigrationEnabled => instance.hostMigration;

        private bool isUsingP2P => instance.connectionType is MirrorVRConnectionType.P2P or MirrorVRConnectionType.P2PWithEncryption;
        private bool isUsingDedicatedServer => instance.connectionType is MirrorVRConnectionType.DedicatedServer or MirrorVRConnectionType.DedicatedServerWithEncryption;
        public static MirrorVRConnectionType ConnectionType => instance.connectionType;
        public static bool IsUsingP2P => instance.isUsingP2P;
        public static bool IsUsingDedicatedServer => instance.isUsingDedicatedServer;

        public static InventoryService CurrentInventoryService => instance.inventoryService;

        public static ProductUserId ProductUserID => EOSManager.LocalUserProductID;
        public static string ProductUserIDString => EOSManager.LocalUserProductIDString;

        #endregion

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        private Callback<GetTicketForWebApiResponse_t> getTicket; //IMPORTANT: here to make sure GC doesn't collect this before it's done.
#endif

        public override void Awake()
        {
            switch (connectionType)
            {
                case MirrorVRConnectionType.P2P:
                case MirrorVRConnectionType.P2PWithEncryption:

                    EOSTransport eos = gameObject.AddComponent<EOSTransport>();
                    eos.relayControl = P2PSettings.relayControl;
                    eos.timeout = P2PSettings.timeout;
                    eos.hostMigrationEnabled = hostMigration;

                    transport = SetupTransport(eos);
                    break;

                case MirrorVRConnectionType.DedicatedServer:
                case MirrorVRConnectionType.DedicatedServerWithEncryption:

                    Transport baset = dedicatedServerSettings.transport switch
                    {
                        DedicatedServerTransport.KCP => gameObject.AddComponent<KcpTransport>(),
                        DedicatedServerTransport.SimpleWeb => gameObject.AddComponent<SimpleWeb.SimpleWebTransport>(),
                        _ => throw new NotSupportedException("unsupported transport")
                    };

                    transport = SetupTransport(baset);
                    break;
            }

            base.Awake();

            MirrorVRLogger.Init(loggerLevel);

            if (instance == null) instance = this;
            else throw new NotSupportedException("You already have another MirrorVRManager in the scene. You may only have one per scene.");

            if (CurrentInventoryService == InventoryService.PlayerDataStorage)
                gameObject.AddComponent<MirrorVRDataStorage>();


            if (roomLimit > 64)
                throw new ArgumentOutOfRangeException(nameof(roomLimit), "Your max room limit on MirrorVRManager is over 64 players. You can't have more than 64 players in a lobby. Please fix this value in your MirrorVRManager script.");

            if (new[] { productName, productId, sandboxId, deploymentId, clientId, clientSecret, encryptionKey }.Any(string.IsNullOrEmpty))
                throw new NullReferenceException("One or more of your API Keys on MirrorVRManager is empty. Please make sure you fill them out with the correct values.");

            if (encryptionKey.Length != 64) throw new ArgumentOutOfRangeException(nameof(encryptionKey), $"Your Encryption Key's length is {encryptionKey.Length}, and should be EXACTLY 64 characters (32 bytes).");

            if (hostMigration)
            {
                HostMigrationController hmc = GetComponentInChildren<HostMigrationController>(true);
                if (hmc == null) throw new MissingComponentException("You have host migration enabled, but no Host Migration Controller is found. Please create a child GameObject, and add Host Migration Controller to it.");

                hmc.playerSelectionMethod = playerSelection;
                MirrorVRLogger.LogWarn("Host Migration isn't fully complete. Expect bugs.");
            }

#if UNITY_EDITOR && STEAMWORKS_NET && !DISABLESTEAMWORKS
            if (GetAuthType() == MirrorVRAuthType.SteamSessionTicket)
            {
                //IMPORTANT: fixes a bug where Steam can't find the "steam_appid.txt", because it's not looking in the project root, it's looking in the directory containing Unity.exe. Thanks rxxyn!
                string appid = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt"));
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(UnityEditor.EditorApplication.applicationPath), "steam_appid.txt"), appid);
            } 
#endif
        }

        public override void Start()
        {
            base.Start();

            Login();
#if UNITY_EDITOR || !UNITY_SERVER
            StartCoroutine(FetchBans());
#endif
            StartCoroutine(OnLogin());

            InvokeRepeating(nameof(Tick), 0.2f, 0.2f);
        }

        public override void OnApplicationQuit()
        {
            if (ConnectedToLobby) Disconnect();
            base.OnApplicationQuit();
        }

        private void OnDisable()
        {
            Application.quitting -= OnQuit;
        }

        private void OnEnable()
        {
            Application.quitting += OnQuit;
        }

#if !UNITY_SERVER || UNITY_EDITOR
        private IEnumerator OnLogin()
        {
            yield return new WaitUntil(() => EOSManager.Initialized);

            if (automaticallyJoinRoom) JoinRandomLobby();
        }

#elif UNITY_SERVER
        private MirrorVRServerCommunications comms;

        private IEnumerator OnLogin()
        {
            yield return new WaitUntil(() => EOSManager.Initialized);

            hostMigration = false;

            string roomname = UnityEngine.Random.Range(10000, 99999).ToString();
            int maxplayers = RoomLimit;
            string ip = string.Empty;
            ushort port = 40000;

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-roomname": roomname = args[++i]; break;
                    case "-maxplayers": maxplayers = int.Parse(args[++i]); break;
                    case "-ip": ip = args[++i]; break;
                    case "-port": port = ushort.Parse(args[++i]); break;
                }
            }

            networkAddress = ip;
            maxConnections = maxplayers;
            GetComponent<PortTransport>().Port = port;

            MirrorVRLogger.LogInfo($"Starting Room '{roomname}' on port {port} with {maxplayers} max connections.");

            StartServer();
            comms = gameObject.AddComponent<MirrorVRServerCommunications>();  
        }
#endif

        private IEnumerator FetchBans()
        {
            yield return new WaitUntil(() => EOSManager.Initialized);

            SanctionsInterface SInterface = EOSManager.GetSanctionsInterface();

            QueryActivePlayerSanctionsOptions ops = new QueryActivePlayerSanctionsOptions
            {
                LocalUserId = EOSManager.LocalUserProductID,
                TargetUserId = EOSManager.LocalUserProductID,
            };

            object returnedData = new object();

            SInterface.QueryActivePlayerSanctions(ref ops, returnedData, (ref Epic.OnlineServices.Sanctions.QueryActivePlayerSanctionsCallbackInfo bbb) =>
            {
                if (bbb.ResultCode == Result.Success)
                {
                    MirrorVRLogger.LogInfo("Retrieved active sanctions");
                    GetPlayerSanctionCountOptions o = new GetPlayerSanctionCountOptions
                    {
                        TargetUserId = EOSManager.LocalUserProductID,
                    };
                    if (SInterface.GetPlayerSanctionCount(ref o) > 0)
                    {
                        DestroyImmediate(EOSManager.instance);
                        UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.FatalError);
                    }
                }

                if (bbb.ResultCode == Result.UserBanned)
                {
                    UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.FatalError);
                }
            });
        }

        private void Tick()
        {
            if (ConnectedToLobby)
            {
#if UNITY_2023_1_OR_NEWER
                PlayerList = FindObjectsByType<MirrorVRPlayer>(FindObjectsSortMode.None).OrderBy(p => p.connectionId).ToList();
#else
                PlayerList = FindObjectsOfType<MirrorVRPlayer>().OrderBy(p => p.connectionId).ToList();
#endif
            }
        }

        private void Login()
        {
            MirrorVRAuthType authtype = GetAuthType();

            switch (authtype)
            {
                case MirrorVRAuthType.Oculus:
#if META_XR_SDK
                    Core.AsyncInitialize().OnComplete(result1 =>
                    {
                        if (!result1.IsError)
                        {
                            Entitlements.IsUserEntitledToApplication().OnComplete(result2 =>
                            {
                                if (!result2.IsError)
                                {
                                    Users.GetLoggedInUser().OnComplete(result3 =>
                                    {
                                        if (!result3.IsError)
                                        {
                                            User userr = result3.GetUser();
                                            if (string.IsNullOrEmpty(PlayerPrefs.GetString("Username"))) PlayerPrefs.SetString("Username", userr.OculusID);

                                            Users.GetUserProof().OnComplete(result4 =>
                                            {
                                                if (!result4.IsError)
                                                {
                                                    MirrorVRLogger.LogInfo($"Login to account oculus {userr.OculusID} success");
                                                    EOSManager.Initialize(new TransportInitializeOptions()
                                                    {
                                                        AuthInterfaceCredentialType = LoginCredentialType.ExternalAuth,
                                                        ConnectInterfaceCredentialType = ExternalCredentialType.OculusUseridNonce,

                                                        ProductName = productName,
                                                        ProductId = productId,
                                                        ClientId = clientId,
                                                        ClientSecret = clientSecret,
                                                        SandboxId = sandboxId,
                                                        DeploymentId = deploymentId,
                                                        EncryptionKey = encryptionKey,

                                                        DisplayName = PlayerPrefs.GetString("Username", userr.OculusID),
                                                        LoginToken = $"{userr.ID.ToString()}|{result4.Data.Value}"
                                                    });
                                                }
                                                else MirrorVRLogger.LogError($"Oculus nonce validation failed with message: {result4.GetError().Message}");
                                            });
                                        }
                                        else MirrorVRLogger.LogError($"Oculus login failed with message: {result3.GetError().Message}");
                                    });
                                }
                                else MirrorVRLogger.LogError($"Entitlement check failed with message: {result2.GetError().Message}");
                            });
                        }
                        else MirrorVRLogger.LogError($"Oculus init failed with message: {result1.GetError().Message}");
                    });
#endif
                    break;

                case MirrorVRAuthType.DeviceId:
                    string dname = PlayerPrefs.GetString("Username");
                    if (string.IsNullOrEmpty(dname))
                    {
                        dname = string.Concat(randomUsernamePrefix, UnityEngine.Random.Range(10000, 99999));
                        PlayerPrefs.SetString("Username", dname);
                    }

                    EOSManager.Initialize(new TransportInitializeOptions()
                    {
                        AuthInterfaceCredentialType = LoginCredentialType.ExternalAuth,
                        ConnectInterfaceCredentialType = ExternalCredentialType.DeviceidAccessToken,

                        ProductName = productName,
                        ProductId = productId,
                        ClientId = clientId,
                        ClientSecret = clientSecret,
                        SandboxId = sandboxId,
                        DeploymentId = deploymentId,
                        EncryptionKey = encryptionKey,

                        DisplayName = dname
                    });

                    break;

                case MirrorVRAuthType.EpicPortal:
                    EOSManager.Initialize(new TransportInitializeOptions()
                    {
                        AuthInterfaceCredentialType = LoginCredentialType.AccountPortal,
                        ConnectInterfaceCredentialType = ExternalCredentialType.Epic,

                        ProductName = productName,
                        ProductId = productId,
                        ClientId = clientId,
                        ClientSecret = clientSecret,
                        SandboxId = sandboxId,
                        DeploymentId = deploymentId,
                        EncryptionKey = encryptionKey
                    });
                    break;


                case MirrorVRAuthType.SteamSessionTicket:
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
                    if (SteamManager.Initialized)
                    {
                        MirrorVRLogger.LogInfo("Getting steam auth ticket");
                        SteamUser.GetAuthTicketForWebApi("epiconlineservices");
                        getTicket = Callback<GetTicketForWebApiResponse_t>.Create(OnWebTicketLoaded);
                    }
#endif
                    break;

            }
        }

        #region Steam
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        //some of the steam code is by rxxyn (https://github.com/rxxyn). Thank you rxxyn for saving us hours of work!
        private void OnWebTicketLoaded(GetTicketForWebApiResponse_t cb)
        {
            if (cb.m_eResult != EResult.k_EResultOK)
            {
                MirrorVRLogger.LogError($"Failed to get web ticket. error: {cb.m_eResult}");
                return;
            }

            MirrorVRLogger.LogInfo("Got auth ticket");

            byte[] bytes = cb.m_rgubTicket;
            StringBuilder sb = new();
            foreach (var b in bytes) sb.AppendFormat("{0:x2}", b);
            string ticket = sb.ToString();

            string dname = PlayerPrefs.GetString("Username");
            if (string.IsNullOrEmpty(dname))
            {
                //NOTE: the regex removes any characters that aren't A-Z, a-z, or 0-9. ex 'Ste@mU$er1' would become 'StemUer1'.
                dname = System.Text.RegularExpressions.Regex.Replace(SteamFriends.GetPersonaName(), @"[^A-Za-z0-9]", "");

                if (dname.Length > ConnectInterface.USERLOGININFO_DISPLAYNAME_MAX_LENGTH)
                    dname = dname.Substring(0, ConnectInterface.USERLOGININFO_DISPLAYNAME_MAX_LENGTH);

                PlayerPrefs.SetString("Username", dname);
            }

            EOSManager.Initialize(new TransportInitializeOptions()
            {
                AuthInterfaceCredentialType = LoginCredentialType.ExternalAuth,
                ConnectInterfaceCredentialType = ExternalCredentialType.SteamSessionTicket,

                ProductName = productName,
                ProductId = productId,
                ClientId = clientId,
                ClientSecret = clientSecret,
                SandboxId = sandboxId,
                DeploymentId = deploymentId,
                EncryptionKey = encryptionKey,

                DisplayName = dname,
                LoginToken = ticket,
            });
        }
#endif
        #endregion

        #region Lobby System

        /// <summary>
        /// A combination of <see cref="JoinLobby(string)"/> and <see cref="CreateLobby(string, int, AttributeData[])"/> that will first check if that room exists already, and if it does join it, but if it doesn't, create a new room.
        /// </summary>
        /// <param name="lobbycode">The lobby code entered.</param>
        /// <param name="maxPlayers">The max amount of players that can join this room. Set in-code as -1, but is actually <see cref="instance.roomLimit"/>. </param>
        /// <param name="ExtraAttributes">Optional attributes, will be used for searching and creating.</param>

        public static void JoinOrCreateLobby(string lobbycode, int maxPlayers = -1, AttributeData[] ExtraAttributes = null)
        {
            if (lobbycode.Length < LobbyInterface.MIN_LOBBYIDOVERRIDE_LENGTH) throw new ArgumentException($"lobbycode must be more than {LobbyInterface.MIN_LOBBYIDOVERRIDE_LENGTH} characters.");

            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn($"{nameof(JoinOrCreateLobby)} failed. You must be logged in to call this method.");
                return;
            }

            MirrorVRLogger.LogInfo($"Joining or creating lobby: {lobbycode}.");
            if (ConnectedToLobby) Disconnect();

            if (maxPlayers == -1) maxPlayers = instance.roomLimit;

            if (IsUsingP2P)
            {
                EOSTransport.SearchForLobbiesByID(lobbycode, cb =>
                {
                    if (cb.Count > 0)
                    {
                        List<LobbyDetails> newdetails = new List<LobbyDetails>();
                        foreach (LobbyDetails details in cb)
                        {
                            LobbyDetailsGetMemberCountOptions getcountopt = new LobbyDetailsGetMemberCountOptions();
                            if (details.GetMemberCount(ref getcountopt) <= 0) continue;
                            newdetails.Add(details);
                        }

                        if (newdetails.Count > 0) EOSTransport.JoinLobby(newdetails[0]);
                        else EOSTransport.CreateLobby(lobbycode, (uint)maxPlayers, ExtraAttributes != null ? ExtraAttributes.ToList() : null);
                    }
                    else EOSTransport.CreateLobby(lobbycode, (uint)maxPlayers, ExtraAttributes != null ? ExtraAttributes.ToList() : null);
                });
            }
            else if (IsUsingDedicatedServer)
            {
                instance.StartCoroutine(instance.GetDedicatedServerInstance(lobbycode, maxPlayers, res =>
                {
                    ushort port = res;
                    if (port == 0) return;

                    singleton.networkAddress = instance.dedicatedServerSettings.serverAddress;
                    instance.GetComponent<PortTransport>().Port = port;
                    singleton.StartClient();
                }));
            }
        }

        /// <summary>
        /// Joins a lobby using the provided room code. Though it is strongly recommended to use <see cref="JoinOrCreateLobby(string, int, AttributeData[])"/> instead.
        /// </summary>
        /// <param name="lobbycode">The room code entered.</param>
        public static void JoinLobby(string lobbycode)
        {
            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn($"{nameof(JoinLobby)} failed. You must be logged in to call this method.");
                return;
            }

            if (singleton.isNetworkActive) Disconnect();
            
            if (IsUsingP2P)
            {
                EOSTransport.JoinLobbyByID(lobbycode);
            }
            else if (IsUsingDedicatedServer)
            {
                MirrorVRLogger.LogWarn($"{nameof(JoinLobby)} is not supported with Dedicated Server mode. Please use {nameof(JoinOrCreateLobby)} instead.");
                return;
            }
        }

        /// <summary>
        /// Creates a new room. Though it is strongly recommended to use <see cref="JoinOrCreateLobby(string, int, AttributeData[])"/> instead.
        /// </summary>
        /// <param name="lobbycode">The room code entered.</param>
        /// /// <param name="maxPlayers">The max amount of players that can join this room. Set in-code as -1, but is actually <see cref="instance.roomLimit"/>. </param>
        /// <param name="ExtraAttributes">Optional attributes for LobbyInterface</param>
        public static void CreateLobby(string lobbycode, int maxPlayers = -1, AttributeData[] ExtraAttributes = null)
        {
            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn($"{nameof(CreateLobby)} failed. You must be logged in to call this method.");
                return;
            }

            if (IsUsingP2P)
            {
                EOSTransport.CreateLobby(lobbycode, (uint)maxPlayers, ExtraAttributes != null ? ExtraAttributes.ToList() : null);
            }
            else if (IsUsingDedicatedServer)
            {
                MirrorVRLogger.LogWarn($"{nameof(CreateLobby)} is not supported with Dedicated Server mode. Please use {nameof(JoinOrCreateLobby)} instead.");
                return;
            }
        }

        /// <summary>
        /// Joins a random lobby.
        /// </summary>
        /// <param name="ExtraAttributes">Optional attributes, used for searching and both creating.</param>
        /// <param name="maxPlayers">The max amount of players that can join this room. Set in-code as -1, but is actually <see cref="instance.roomLimit"/>.</param>
        /// <param name="createLobbyWhenNoMatchesFound">If <see langword="true"/>, a new lobby will be created if no matches are found. If <see langword="false"/>, nothing will happen if no matches are found.</param>
        public static void JoinRandomLobby(int maxPlayers = -1, AttributeData[] ExtraAttributes = null, bool createLobbyWhenNoMatchesFound = true)
        {
            MirrorVRLogger.LogInfo("Joining random lobby...");
            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn($"{nameof(CreateLobby)} failed. You must be logged in to call this method.");
                return;
            }

            EOSTransport.SearchForLobbiesByAttribute(new AttributeData { Key = PublicRoomKey, Value = true }, 200, cb =>
            {
                if (cb.Count > 0)
                {
                    List<(LobbyDetails details, uint playerCount)> candidates = new List<(LobbyDetails details, uint playerCount)>();

                    foreach (LobbyDetails details in cb)
                    {
                        LobbyDetailsCopyInfoOptions infoopt = new LobbyDetailsCopyInfoOptions();
                        if (details.CopyInfo(ref infoopt, out LobbyDetailsInfo? info) != Result.Success || !info.HasValue) continue;

                        uint current = info.Value.MaxMembers - info.Value.AvailableSlots;

                        if (current <= 0) continue;
                        if (info.Value.AvailableSlots <= 0) continue;

                        candidates.Add((details, current));
                    }

                    candidates.Sort((a, b) => a.playerCount.CompareTo(b.playerCount));

                    if (candidates.Count > 0) EOSTransport.JoinLobby(candidates[0].details);
                    else Create(maxPlayers, ExtraAttributes, createLobbyWhenNoMatchesFound);
                }
                else Create(maxPlayers, ExtraAttributes, createLobbyWhenNoMatchesFound);
            });

            static void Create(int maxPlayers = -1, AttributeData[] ExtraAttributes = null, bool createLobbyWhenNoMatchesFound = true)
            {
                if (!createLobbyWhenNoMatchesFound)
                {
                    MirrorVRLogger.LogInfo($"{nameof(JoinRandomLobby)}: No matching lobbies found.");  //nothing to join, and we aren't allowed to create a new one.
                    return;
                }

                MirrorVRLogger.LogInfo($"{nameof(JoinRandomLobby)}: No matching lobbies found, creating a new one...");
                AttributeData dat = new AttributeData() { Key = PublicRoomKey, Value = true };
                if (ExtraAttributes == null) ExtraAttributes = new AttributeData[] { dat };
                else ExtraAttributes = ExtraAttributes.Append(dat).ToArray();

                if (maxPlayers == -1) maxPlayers = instance.roomLimit;
                CreateLobby(UnityEngine.Random.Range(10000, 99999).ToString(), maxPlayers, ExtraAttributes);
            }
        }

        /// <summary>
        /// Disconnects from the current lobby.
        /// </summary>
        public static void Disconnect()
        {
            if (!singleton.isNetworkActive)
            {
                MirrorVRLogger.LogWarn("Disconnection Failed. You are not in a lobby.");
                return;
            }

            if (IsUsingP2P) EOSTransport.LeaveLobby();
            else if (IsUsingDedicatedServer) singleton.StopClient();
        }

        /// <summary>
        /// Attempts to kick the provided player from the server. Requires being the server in order to work.
        /// </summary>
        /// <param name="player">The <see cref="ProductUserId"/> of the player to kick.</param>
        public static void KickPlayer(ProductUserId player)
        {
            if (!NetworkServer.active)
            {
                MirrorVRLogger.LogWarn($"{nameof(KickPlayer)} failed. You must be in a lobby AND be the server to call this method.");
                return;
            }

            if (IsUsingDedicatedServer)
            {
                MirrorVRLogger.LogError($"{nameof(KickPlayer)}(ProductUserId) failed. You must be using P2P mode to call this variation of the method. Please use {nameof(KickPlayer)}(int) instead.");
                return;
            }

            var options = new KickMemberOptions
            {
                LobbyId = EOSTransport.ConnectedLobbyInfo.LobbyId,
                LocalUserId = EOSManager.LocalUserProductID,
                TargetUserId = player
            };

            EOSManager.GetLobbyInterface().KickMember(ref options, null, delegate (ref KickMemberCallbackInfo result)
            {
                if (result.ResultCode == Result.Success)
                {
                    MirrorVRLogger.LogInfo($"Kicked Player {player.ToString()}");
                }
            });
        }

        /// <summary>
        /// Attempts to kick the provided player from the server. Requires being the server in order to work.
        /// </summary>
        /// <param name="connectionId">The Mirror Connection ID of the player to kick.</param>
        public static void KickPlayer(int connectionId)
        {
            if (!NetworkServer.active)
            {
                MirrorVRLogger.LogWarn($"{nameof(KickPlayer)} failed. You must be in a lobby AND be the server to call this method.");
                return;
            }

            if (IsUsingP2P)
            {
                MirrorVRLogger.LogError($"{nameof(KickPlayer)}(int) failed. You must be using Dedicated Server mode to call this variation of the method. Please use {nameof(KickPlayer)}(ProductUserId) instead.");
                return;
            }

            NetworkServer.connections[connectionId].Disconnect();
        }

#endregion

        #region Lobby Info
        public static string GetLobbyCode()
        {
            //TODO: implement method for 
            if (ConnectedToLobby)
            {
                return EOSTransport.ConnectedLobbyInfo.LobbyId;
            }
            else return null;
        }

        /// <summary>
        /// ***OBSOLETE*** Checks if the local player is the lobby host.
        /// </summary>
        [Obsolete("Use 'Mirror.NetworkServer.active' instead.")]
        public static bool LocalPlayerIsHost()
        {
            if (ConnectedToLobby && UnityEngine.Application.isPlaying)
                return GetLobbyHost() == EOSManager.LocalUserProductID;
            else
                return false;

        }

        /// <summary>
        /// Gets the current owner of the lobby.
        /// </summary>
        public static ProductUserId GetLobbyHost()
        {
            if (!quitting)
            {
                if (ConnectedToLobby)
                {
                    LobbyDetailsGetLobbyOwnerOptions sdsd = new LobbyDetailsGetLobbyOwnerOptions();
                    return EOSTransport.ConnectedLobbyInfo.CurrentLobbyDetails.GetLobbyOwner(ref sdsd);
                }
                else return null;
            }
            else return null;

        }

        /// <summary>
        /// Get the number of members associated with this lobby.
        /// </summary>
        public static int GetMemberCount()
        {
            if (ConnectedToLobby)
            {
                return (int)EOSTransport.ConnectedLobbyInfo.GetPlayerCount();
            }
            else
            {
                MirrorVRLogger.LogError("Player is not in a lobby, can not retrieve member count.");
                return 0;
            }
        }

        /// <summary>
        /// <see cref="GetMemberByIndex" /> is used to immediately retrieve individual members registered with a lobby.
        /// </summary>
        public static ProductUserId GetMemberByIndex(int index)
        {
            if (ConnectedToLobby)
            {
                LobbyDetailsGetMemberByIndexOptions o = new LobbyDetailsGetMemberByIndexOptions
                {
                    MemberIndex = (uint)index
                };

                return EOSTransport.ConnectedLobbyInfo.CurrentLobbyDetails.GetMemberByIndex(ref o);
            }
            else
            {
                MirrorVRLogger.LogError("Player is not in a lobby, can not retrieve member by index.");
                return null;
            }
        }
#endregion

        #region Player Info
        /// <summary>
        /// Sets the player username based on the provided string.
        /// </summary>
        /// <param name="username">The new player username.</param>
        public static void SetUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                MirrorVRLogger.LogWarn("username cannot be set to nothing.");
                return;
            }

            EOSManager.DisplayName = username;
            PlayerPrefs.SetString("Username", username);
            MirrorVRLogger.LogInfo($"Username set to '{username}'");
        }

        /// <summary>
        /// Returns the current player username.
        /// </summary>
        public static string GetUsername() => EOSManager.DisplayName;

        /// <summary>
        /// Sets the player color based on the color provided.
        /// </summary>
        /// <param name="color">The new color to set the player to.</param>
        public static void SetColor(Color color) 
        {
            if (!ConnectedToLobby) PlayerPrefs.SetString("PlayerColor", JsonUtility.ToJson(color));
            else LocalPlayer.SetPlayerColor(color);
        }

        /// <summary>
        /// Toggles the specified cosmetic for the local player.
        /// </summary>
        /// <param name="category">The cosmetic type.</param>
        /// <param name="cosmeticName">The name of the cosmetic. Set to <see cref="string.Empty"/> to unequip.</param>
        public static void SetCosmetic(string category, string cosmeticName)
        {
            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn($"{nameof(SetCosmetic)} failed. You must be logged in to call this method.");
                return;
            }

            if (!ConnectedToLobby)
            {
                GetEquippedCosmetics(cb1 =>
                {
                    if (cb1 == null) return;

                    if (cb1.Any(c => c.slot == category && c.name == cosmeticName))
                    {
                        cb1.RemoveAll(c => c.slot == category && c.name == cosmeticName);
                        SaveCosmetics(cb1);
                    }
                    else
                    {
                        cb1.Add(new Cosmetic(category, cosmeticName));
                        SaveCosmetics(cb1);
                    }
                });
                return;
            }

            LocalPlayer.ToggleCosmetic(category, cosmeticName);
        }

        /// <summary>
        /// An easy method to return the player's inventory in one place.
        /// </summary>
        /// <param name="result">A lambda expression for when the list is ready to be sent back.</param>
        public static void GetInventory(Action<List<Cosmetic>> result)
        {
            if (instance.dataService != null)
            {
                instance.dataService.GetInventory(cb => result?.Invoke(cb));
            }
            else
            {
                MirrorVRLogger.LogError("cannot get equipped cosmetics: custom data service not set!");
            }
        }

        public static void GetEquippedCosmetics(Action<List<Cosmetic>> result)
        {
            if (CurrentInventoryService == InventoryService.PlayerDataStorage)
            {
                if (MirrorVRDataStorage.FileExists("MirrorVREquippedCosmetics.json"))
                {
                    MirrorVRDataStorage.PlayerDataStorageRetrieveContent("MirrorVREquippedCosmetics.json", res =>
                    {
                        if (res != null)
                        {
                            if (!string.IsNullOrEmpty(res))
                            {
                                List<Cosmetic> cosmetics = JsonConvert.DeserializeObject<List<Cosmetic>>(res);
                                result?.Invoke(cosmetics);
                            }
                        }
                    });
                }
                else MirrorVRDataStorage.PlayerDataStorageWriteFile("MirrorVREquippedCosmetics.json", "");
            }
            else
            {
                if (instance.dataService != null)
                {
                    instance.dataService.GetEquippedCosmetics((cb, success) => result?.Invoke(cb));
                }
                else
                {
                    MirrorVRLogger.LogError("cannot get equipped cosmetics: custom data service not set!");
                }
            }
        }

        #endregion

        #region Other
        /// <summary>
        /// Changes the current network scene. Server only method.
        /// </summary>
        public static void ChangeScene(string sceneName, SceneOperation operation)
        {
            if (NetworkState is NetworkManagerMode.ClientOnly or NetworkManagerMode.Offline) return;

            SceneMessage scenemessage = new SceneMessage()
            {
                sceneName = sceneName,
                sceneOperation = operation
            };

            NetworkServer.SendToAll(scenemessage, 0, true);
        }

        public static void SetCustomDataHandler(ICustomDataService service)
        {
            if (instance == null) return;
            instance.dataService = service;

            MirrorVRLogger.LogInfo("successfully set custom data service.");
        }
        #endregion

        #region Internal Methods
        public override void OnServerTransportException(NetworkConnectionToClient conn, Exception exception)
        {
            string connection = conn == null ? "connection(unknown)" : conn.ToString();
            MirrorVRLogger.LogError($"TransportException: from {connection}, '{exception.ToString()}'");
        }

#if UNITY_SERVER && !UNITY_EDITOR
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            comms.UpdatePlayerInfo();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            comms.UpdatePlayerInfo();
        }
#endif

        private MirrorVRAuthType GetAuthType()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor) return windowsEditor;
            if (Application.platform == RuntimePlatform.OSXEditor) return macOSEditor;
            if (Application.platform == RuntimePlatform.LinuxEditor) return linuxEditor;

            if (Application.platform == RuntimePlatform.Android) return android;
            if (Application.platform == RuntimePlatform.WindowsPlayer) return windowsRuntime;
            if (Application.platform == RuntimePlatform.OSXPlayer) return macOSRuntime;
            if (Application.platform == RuntimePlatform.LinuxPlayer) return linuxRuntime;

            if (Application.platform == RuntimePlatform.WindowsServer
                || Application.platform == RuntimePlatform.OSXServer
                || Application.platform == RuntimePlatform.LinuxServer) return MirrorVRAuthType.DeviceId;

            return MirrorVRAuthType.DeviceId;
        }

        private IEnumerator GetDedicatedServerInstance(string lobbyname, int maxplayers, Action<ushort> callback)
        {
            string json = JsonUtility.ToJson(new ServerRequestData()
            {
                RoomName = lobbyname,
                MaxPlayers = maxplayers
            });

            /*string json = JsonConvert.SerializeObject(new ServerRequestData()
            {
                RoomName = lobbyname,
                MaxPlayers = maxplayers
            });*/

            string scheme = instance.dedicatedServerSettings.HTTPMode switch
            {
                HTTPMode.HTTP => "http",
                HTTPMode.HTTPS => "https",
                _ => "http"
            };

            string host = instance.dedicatedServerSettings.serverAddress;
            int port = instance.dedicatedServerSettings.mainPort;

            string url = $"{scheme}://{host}:{port}/";

            UnityWebRequest req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 10;

            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ProductUserIDString}:{instance.dedicatedServerSettings.authKey}"));

            //https://developer.mozilla.org/docs/Web/HTTP/Reference/Headers/Authorization#basic_authentication
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Basic {auth}");
            req.SetRequestHeader("Version", Application.version);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                MirrorVRLogger.LogError($"Error while Joining or Creating a Lobby: {req.error}");
                callback?.Invoke(0);
            }
            else callback?.Invoke(ushort.Parse(req.downloadHandler.text));
        }

        public override void OnValidate()
        {
            base.OnValidate();

#if UNITY_EDITOR
            if (Application.isPlaying) return;
            if (connectionType is MirrorVRConnectionType.P2PWithEncryption or MirrorVRConnectionType.DedicatedServerWithEncryption)
            {
                if (!TryGetComponent(out EncryptionTransport et))
                {
                    var et2 = gameObject.AddComponent<EncryptionTransport>();
                    transport = et2;
                }
            }
            else
            {
                if (TryGetComponent(out EncryptionTransport et))
                {
                    //https://discussions.unity.com/t/onvalidate-and-destroying-objects/544819/14
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        UnityEditor.Undo.DestroyObjectImmediate(et);
                        transport = null;
                    };
                }
            }
#endif
        }

        private Transport SetupTransport(Transport baset)
        {
            if (TryGetComponent(out EncryptionTransport encryption))
            {
                encryption.Inner = baset;
                return encryption;
            }

            return baset;
        }

        internal static void SaveCosmetics(List<Cosmetic> data)
        {
            if (CurrentInventoryService == InventoryService.PlayerDataStorage) MirrorVRDataStorage.PlayerDataStorageWriteFile("MirrorVREquippedCosmetics.json", JsonConvert.SerializeObject(data));
            else
            {
                if (instance.dataService != null) instance.dataService.SetEquippedCosmetics(data);
                else MirrorVRLogger.LogError("cannot save cosmetics: not custom data provider set!");
            }
        }


        #region Quitting Detection
        internal static bool quitting = false;

        private void OnQuit()
        {
            quitting = true;
        }
        #endregion

        #endregion
    }

    public enum MirrorVRAuthType : byte
    {
        [InspectorName("Oculus")] Oculus = 0,
        [InspectorName("Device ID")] DeviceId = 1,
        [InspectorName("EOS Account Portal")] EpicPortal = 2,
        [InspectorName("Steam (Session Ticket)")] SteamSessionTicket = 3
    }

    public enum MirrorVRConnectionType : byte
    {
        P2P,
        P2PWithEncryption,

        DedicatedServer,
        DedicatedServerWithEncryption
    }

    public enum InventoryService : byte
    {
        [InspectorName("EOS Player Data Storage")] PlayerDataStorage = 0,
        [InspectorName("PlayFab")] Playfab = 1,
        [InspectorName("Custom Service")] Custom  = 2
    }

    [Serializable]
    public class P2PSettings
    {
        //public PacketReliability[] Channels = new PacketReliability[2] { PacketReliability.ReliableOrdered, PacketReliability.UnreliableUnordered };
        [Tooltip("Timeout for connecting in seconds.")] public int timeout = 25;
        //[Tooltip("The max fragments used in fragmentation before throwing an error.")] public int maxFragments = 55;
        public RelayControl relayControl = RelayControl.AllowRelays;
    }

    [Serializable]
    public class DedicatedServerSettings
    {
        public string serverAddress = "0.0.0.0";
        public AddressType addressType = AddressType.IPv4;
        public HTTPMode HTTPMode = HTTPMode.HTTP;
        [Space]
        public ushort mainPort = 7777;
        public ushort commsPort = 7778;
        [SerializeField] internal string authKey = "AuthKey123!@#";
        [SerializeField] internal DedicatedServerTransport transport = DedicatedServerTransport.KCP;
    }

    [Serializable]
    public class ServerRequestData
    {
        public string RoomName;
        public int MaxPlayers;
    }

    public enum DedicatedServerTransport { KCP, SimpleWeb }
    public enum AddressType
    {
        [InspectorName("IPv4")] IPv4,
        [InspectorName("IPv6")] IPv6,
        [InspectorName("Domain (No Port)")] DomainWithoutPort,
        [InspectorName("Domain (With Port)")] DomainWithPort
    }

    public enum HTTPMode { HTTP, HTTPS }
}