using System.Collections;
using UnityEngine;
using EpicTransport;
using Epic.OnlineServices.Lobby;
using System.Collections.Generic;

namespace Mirror.VR.Extensions.LobbyBrowser
{
    public class MirrorVRRoomBrowser : MonoBehaviour
    {
        public GameObject Prefab;
        public Transform ViewportContent;

        [Header("Settings")]
        public string LobbyNameKey = "LobbyCode";
        public float YStartPosition = 138f;
        public float Offset = 10f;

        public void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            StartCoroutine(LoadLobbies());
        }


        public IEnumerator LoadLobbies()
        {
            yield return new WaitUntil(() => EOSManager.Initialized); // Wait until EOS is initialized. (We can't load lobbies when it isn't.)
            MirrorVRLogger.LogInfo("Loading lobby browser.");

            EOSTransport.FindLobbies(cb => { HandleDetails(cb); });
        }


        public void HandleDetails(List<LobbyDetails> details)
        {
            for (int i = 0; i < details.Count; i++)
            {
                #region Positioning the UI.
                Vector3 Position = new Vector3(0, YStartPosition - Offset * (i + 1), 0);

                GameObject LobbyUI = Instantiate(Prefab);
                LobbyUI.transform.SetParent(ViewportContent);

                LobbyUI.GetComponent<RectTransform>().anchoredPosition3D = Position;
                LobbyUI.GetComponent<RectTransform>().localScale = Vector3.one;
                LobbyUI.GetComponent<RectTransform>().rotation = new Quaternion(0, 0, 0, 0);

                LobbyPrefab PrefabUI = LobbyUI.GetComponentInChildren<LobbyPrefab>();
                #endregion

                #region Getting the lobby's player count + max players.
                // Current players
                LobbyDetailsGetMemberCountOptions MemberCountOptions = new LobbyDetailsGetMemberCountOptions();
                int Players = (int)details[i].GetMemberCount(ref MemberCountOptions);

                // Max players
                LobbyDetailsCopyInfoOptions CopyInfoOptions = new LobbyDetailsCopyInfoOptions();
                LobbyDetailsInfo? Info;

                details[i].CopyInfo(ref CopyInfoOptions, out Info);
                int MaxPlayers = (int)Info.Value.MaxMembers;
                #endregion

                #region Adding Button Listener
                PrefabUI.JoinButton.onClick.AddListener(() => EOSTransport.JoinLobby(details[i]));
                #endregion

                #region Setting text.
                PrefabUI.LobbyName.text = Info.Value.LobbyId;
                PrefabUI.PlayerCount.text = $"{Players}/{MaxPlayers}";
                #endregion
            }
        }
    }
}
