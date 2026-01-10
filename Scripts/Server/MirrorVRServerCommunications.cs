/*
* MIT License
* Copyright (c) 2025 Coded Immersions
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

//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Mirror.VR.Server
{
    public class MirrorVRServerCommunications : MonoBehaviour
    {
#if UNITY_SERVER && !UNITY_EDITOR

        private ClientWebSocket ws;
        private CancellationTokenSource cts;

        private async void Start()
        {
            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();
            Uri uri = new Uri($"ws://127.0.0.1:{MirrorVRManager.CommsPort}/ws/");

            await ws.ConnectAsync(uri, CancellationToken.None);
            MirrorVRLogger.LogInfo("instance now connected to main websocket.");

            await Send(CommandType.HANDSHAKE, GetComponent<PortTransport>().Port, "");

            _ = Task.Run(() => ReceiveLoop(cts.Token));
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                        break;
                    }

                    string msgText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var req = JsonUtility.FromJson<CommsRequest>(msgText);
                    //var req = JsonConvert.DeserializeObject<CommsRequest>(msgText);

                    switch (req.type)
                    {
                        case CommandType.UPDATE_MAX_CONNECTIONS:
                            if (int.TryParse(req.data, out int newMax)) NetworkManager.singleton.maxConnections = newMax;
                            break;

                        case CommandType.KICK_PLAYER:
                            NetworkServer.connections[int.Parse(req.data)]?.Disconnect();
                            break;
                    }
                }
                catch (Exception e) { MirrorVRLogger.LogError($"WebSocket error: {e.Message}"); }
            }
        }

        public async Task Send(CommandType type, ushort port, string data)
        {
            if (ws.State != WebSocketState.Open) return;

            var msg = JsonUtility.ToJson(new CommsRequest()
            {
                type = type,
                assignedport = port,
                data = data
            });

            /*var msg = JsonConvert.SerializeObject(new CommsRequest()
            {
                type = type,
                assignedport = port,
                data = data
            });*/

            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void OnApplicationQuit()
        {
            cts?.Cancel();
            ws?.Dispose();
        }

        internal void UpdatePlayerInfo()
        {
            _ = Task.Run(() => Send(CommandType.UPDATE_PLAYER_COUNT, GetComponent<PortTransport>().Port, NetworkManager.singleton.numPlayers.ToString()));

            List<PlayerInfo> players = new List<PlayerInfo>();
            foreach (NetworkConnectionToClient client in NetworkServer.connections.Values)
            {
                if (client.identity == null) continue;

                if (client.identity.TryGetComponent(out MirrorVRPlayer player))
                {
                    players.Add(new PlayerInfo()
                    {
                        Name = player.playerName,
                        ConnectionID = player.connectionToClient.connectionId,
                        PUID = player.ProductUserID,
                        NativeID = null //TODO: add native ids
                    });
                }
            }

            _ = Task.Run(() => Send(CommandType.UPDATE_PLAYER_INFO, GetComponent<PortTransport>().Port, JsonUtility.ToJson(players)));
            //_ = Task.Run(() => Send(CommandType.UPDATE_PLAYER_INFO, GetComponent<PortTransport>().Port, JsonConvert.SerializeObject(players)));
        }
#endif
    }

    public class CommsRequest
    {
        public CommandType type;
        public ushort assignedport;
        public string data;

        public CommsRequest() { }
    }

    public enum CommandType
    {
        HANDSHAKE, //outbound
        UPDATE_PLAYER_COUNT, //outbound
        UPDATE_PLAYER_INFO, //outbound
        UPDATE_MAX_CONNECTIONS, //inbound
        KICK_PLAYER //inbound
    }

    public class PlayerInfo
    {
        public string Name;
        public string NativeID;
        public string PUID;
        public int ConnectionID;
    }
}