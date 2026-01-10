#if MIRROR_VR && PLAYFAB
using EpicTransport;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mirror.VR.Extensions.PlayFab
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MirrorVR/MirrorVR PlayFab")]
    public class MirrorVRPlayFab : MonoBehaviour, ICustomDataService
    {
        [SerializeField] private bool logIn = true;
        [SerializeField] private string currencyCode = "AB";
        [SerializeField] private float currencyRefreshInterval = 30;
        [SerializeField] private bool autoSetDisplayName = true;
        [SerializeField] private string cosmeticsCatalogName = "Cosmetics";

        #region Statics
        public static MirrorVRPlayFab instance;
        public static bool initialized { get; private set; } = false;

        public static string PlayfabId { get; private set; }
        public static int Currency { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                MirrorVRLogger.LogWarn($"Multiple {nameof(MirrorVRPlayFab)} instances detected. Destroying duplicate.");
                Destroy(this);
                return;
            }
            instance = this;
            StartCoroutine(SetService());
            if (logIn)
            { 
                //here to remove CS0414 warning until the below is fixed.
            }
            //if (logIn) Init();
            //commented out due to PUID thing below
        }

        private IEnumerator SetService()
        {
            yield return new WaitUntil(() => MirrorVRManager.instance != null);
            if (MirrorVRManager.CurrentInventoryService == InventoryService.Playfab) MirrorVRManager.SetCustomDataHandler(this);
        }
        #endregion

        private void Init()
        {
            //TODO: move AWAY from PUID, NOT secure
            var req = new LoginWithCustomIDRequest()
            {
                CustomId = EOSManager.LocalUserProductID.ToString(),
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                },
                
            };

            PlayFabClientAPI.LoginWithCustomID(req, 
            res =>
            {
                initialized = true;

                if (autoSetDisplayName) UpdateDisplayName(EOSManager.DisplayName, null);

                InvokeRepeating(nameof(RefreshCurrency), 0, currencyRefreshInterval);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} Initialization Error: {error.GenerateErrorReport()}");
            });
        }

        #region Helper Methods
        public static void GetInventory(Action<List<ItemInstance>> completed)
        {
            var req = new GetUserInventoryRequest();
            PlayFabClientAPI.GetUserInventory(req,
            res =>
            {
                completed?.Invoke(res.Inventory);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(GetInventory)} Error: {error.GenerateErrorReport()}");
                completed?.Invoke(null);
            });
        }

        public static void GetCatalogItems(Action<List<CatalogItem>> completed)
        {
            var req = new GetCatalogItemsRequest() { CatalogVersion = instance.cosmeticsCatalogName };
            PlayFabClientAPI.GetCatalogItems(req,
            res =>
            {
                completed?.Invoke(res.Catalog);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(GetCatalogItems)} Error: {error.GenerateErrorReport()}");
                completed?.Invoke(null);
            });
        }

        public static void SetPlayerDataValue(string key, string value, UserDataPermission permission = UserDataPermission.Private)
        {
            var req = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { key, value } }, Permission = permission };
            PlayFabClientAPI.UpdateUserData(req, null,
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(SetPlayerDataValue)} Error: {error.GenerateErrorReport()}");
            });
        }

        public static void TryGetPlayerDataValue(string key, Action<string, bool> completed)
        {
            var req = new GetUserDataRequest() { Keys = new List<string> { key }, PlayFabId = PlayfabId };
            PlayFabClientAPI.GetUserData(req,
            res =>
            {
                if (res.Data.TryGetValue(key, out UserDataRecord value)) completed?.Invoke(value.Value, true);
                else completed?.Invoke(null, false);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(TryGetPlayerDataValue)} Error: {error.GenerateErrorReport()}");
                completed?.Invoke(null, false);
            });
        }

        public static void UpdateDisplayName(string displayName, Action<bool> completed)
        {
            var req = new UpdateUserTitleDisplayNameRequest()
            {
                DisplayName = displayName
            };
            PlayFabClientAPI.UpdateUserTitleDisplayName(req,
            res =>
            {
                completed?.Invoke(true);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(UpdateDisplayName)} Error: {error.GenerateErrorReport()}");
                completed?.Invoke(false);
            });
        }


        public static void RefreshCurrency(Action<int> completed)
        {
            var req = new GetUserInventoryRequest();
            PlayFabClientAPI.GetUserInventory(req,
            res =>
            {
                if (res.VirtualCurrency.TryGetValue(instance.currencyCode, out int value))
                {
                    Currency = value;
                    completed?.Invoke(value);
                }
                else completed?.Invoke(-1);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(RefreshCurrency)} Error: {error.GenerateErrorReport()}");
                completed?.Invoke(-1);
            });
        }

        public static void TryGetTitleDataValue(string key, Action<string, bool> completed)
        {
            var req = new GetTitleDataRequest { Keys = new List<string> { key } };
            PlayFabClientAPI.GetTitleData(req,
            res =>
            {
                if (res.Data.TryGetValue(key, out string value)) completed?.Invoke(value, true);
                else completed?.Invoke(null, false);
            },
            error =>
            {
                MirrorVRLogger.LogError($"{nameof(MirrorVRPlayFab)} {nameof(TryGetTitleDataValue)} Error: {error.GenerateErrorReport()}");
                completed?.Invoke(null, false);
            });
        }

        #endregion

        #region Internal Methods
        public void SetValue(string key, string value)
        {
            SetPlayerDataValue(key, value);
        }

        public void TryGetValue(string key, Action<bool, string> callback)
        {
            TryGetPlayerDataValue(key, (value, success) => { callback.Invoke(success, value);  });
        }

        public void TryGetGlobalValue(string key, Action<bool, string> callback)
        {
            TryGetTitleDataValue(key, (value, success) => { callback.Invoke(success, value); });
        }

        public void GetInventory(Action<List<Cosmetic>> callback)
        {
            List<Cosmetic> cb = new List<Cosmetic>();
            MirrorVRPlayFab.GetInventory(i =>
            {
                GetCatalogItems(c =>
                {
                    foreach (ItemInstance item in i)
                    {
                        Cosmetic cos = new Cosmetic();
                        cos.name = item.ItemId;
                        cos.slot = c.Find(cc => cc.ItemId == item.ItemId).ItemClass;
                        cb.Add(cos);
                    }
                    
                    callback?.Invoke(cb);
                });
            });
        }

        public void GetEquippedCosmetics(Action<List<Cosmetic>, bool> callback)
        {
            TryGetPlayerDataValue("Equipped Cosmetics", (value, success) => { callback.Invoke(value != null ? JsonConvert.DeserializeObject<List<Cosmetic>>(value) : new List<Cosmetic>(), success); });
        }

        public void SetEquippedCosmetics(List<Cosmetic> cosmetics)
        {
            SetPlayerDataValue("Equipped Cosmetics", JsonConvert.SerializeObject(cosmetics));
        }

        public void CosmeticOwned(Cosmetic cosmetic, Action<bool> callback)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (currencyCode.Length > 1) currencyCode = currencyCode.Substring(0, 2);
        }
        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MirrorVRPlayFab))]
    public class MirrorVRPlayFabEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (!string.IsNullOrEmpty(PlayFabSettings.DeveloperSecretKey))
            {
                EditorGUILayout.HelpBox("Developer Secret Key is set in PlayFabSharedSettings. It is recommended to delete it from your Shared Settings, as hackers are able to do destructive things with it. Note that this will stop the PlayFab Admin API from working.", MessageType.Warning);
                if (GUILayout.Button("Delete Secret Key", GUILayout.Width(150))) PlayFabSettings.DeveloperSecretKey = string.Empty;
            }

            GUILayout.Space(15);

            GUIStyle labelstyle = new GUIStyle(GUI.skin.label);
            labelstyle.alignment = TextAnchor.MiddleCenter;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 18;
            GUILayout.Label("MirrorVR PlayFab", labelstyle);

            GUILayout.Space(10);

            base.OnInspectorGUI();

            GUILayout.Space(10);

            if (GUILayout.Button("Shared Settings", GUILayout.Width(120)))
            {
                EditorGUIUtility.PingObject(Resources.Load<PlayFabSharedSettings>("PlayFabSharedSettings"));
            }
        }
    }
#endif
}
#endif
