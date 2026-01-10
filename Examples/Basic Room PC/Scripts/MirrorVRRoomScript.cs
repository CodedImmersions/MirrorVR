using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Epic.OnlineServices.Lobby;
using EpicTransport;

namespace Mirror.VR.Extensions.RoomPC
{
    public class MirrorVRRoomScript : MonoBehaviour
    {
        [Header("This is an example PC using MirrorVR.\nFeel free to look at the code and make your own PC out of it!")]
        [Space(15)]
        [SerializeField] private string roomCode;
        [SerializeField, Range(LobbyInterface.MIN_LOBBYIDOVERRIDE_LENGTH, LobbyInterface.MAX_LOBBYIDOVERRIDE_LENGTH)] private int roomCharacterLimit = 12;
        [SerializeField] private TMP_Text roomText;


        private void Awake()
        {
            roomText.text = $"Room: {roomCode}\n\nIn Room: ";
        }

        internal void AddCharacter(string letter)
        {
            if (roomCode.Length >= roomCharacterLimit)
                roomCode = roomCode.Substring(0, roomCharacterLimit);
            else
                roomCode += letter;

            roomText.text = $"Room: {roomCode}\n\nIn Room: {MirrorVRManager.GetLobbyCode()}";
        }

        internal void RemoveCharacter()
        {
            roomCode = roomCode.Substring(0, roomCode.Length - 1);
            roomText.text = $"Room: {roomCode}\n\nIn Room: {MirrorVRManager.GetLobbyCode()}";
        }

        internal void PressJoin()
        {
            if (roomCode.Length < LobbyInterface.MIN_LOBBYIDOVERRIDE_LENGTH)
            {
                MirrorVRLogger.LogWarn($"Failed to join lobby: lobby code must be more than {LobbyInterface.MIN_LOBBYIDOVERRIDE_LENGTH} characters.");
                return;
            }

            MirrorVRManager.JoinOrCreateLobby(roomCode);
            roomText.text = $"Room: {roomCode}\n\nIn Room: {MirrorVRManager.GetLobbyCode()}";
        }

        internal void LeaveLobby()
        {
            MirrorVRManager.Disconnect();
        }

        #region Callbacks

        private void Start()
        {
            EOSTransport.OnJoinedLobby += LobbyJoined;
            EOSTransport.OnLeftLobby += LobbyLeft;
        }

        private void OnDestroy()
        {
            EOSTransport.OnJoinedLobby -= LobbyJoined;
            EOSTransport.OnLeftLobby -= LobbyLeft;
        }

        private void LobbyJoined(string id)
        {
            roomText.text = $"Room: {roomCode}\n\nIn Room: {id}";
        }

        private void LobbyLeft()
        {
            roomText.text = $"Room: {roomCode}\n\nIn Room: ";
        }
        #endregion
    }
}
