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
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Mirror.VR
{
    //TODO: add more summaries
    public abstract class MirrorVRAuthenticatorBase : NetworkAuthenticator
    {
        protected Dictionary<NetworkConnectionToClient, DateTime> expectedClients { get; private set; } = new Dictionary<NetworkConnectionToClient, DateTime>();
        protected HashSet<string> bannedConnections { get; private set; } = new HashSet<string>();

        protected abstract List<short> otherSuccessCodes { get; }

        protected virtual TimeSpan ClientTimeout { get; set; } = TimeSpan.FromSeconds(30); //If the client does not authenticate in this amount of time, they will be disconnected.

        public const short SuccessStatusCode = 200; //200 - OK
        public const short MissingAuthValuesStatusCode = 401; //401 - Unauthorized
        public const short AuthFailedStatusCode = 403; //403 - Forbidden
        public const short ClientTimeoutStatusCode = 408; //408 - Request Timeout
        public const short BannedStatusCode = 423; //423 - Locked

        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<MirrorVRAuthMessage>(ServerOnClientAuthRequestInternal, false);
            InvokeRepeating(nameof(ServerTick), 0, 0.5f);
        }

        public override void OnStopServer()
        {
            NetworkServer.UnregisterHandler<MirrorVRAuthMessage>();
            CancelInvoke(nameof(ServerTick));
        }

        public override void OnStartClient() => NetworkClient.RegisterHandler<MirrorVRAuthResponse>(ClientOnServerResponseInternal, false);
        public override void OnStopClient() => NetworkClient.UnregisterHandler<MirrorVRAuthResponse>();

        public override void OnServerAuthenticate(NetworkConnectionToClient conn) => expectedClients.Add(conn, DateTime.UtcNow);

        public async override void OnClientAuthenticate()
        {
            MirrorVRAuthMessage msg = new MirrorVRAuthMessage(await ClientSendAuthMessage());
            NetworkClient.Send(msg);
        }

        protected abstract Task<string> ClientSendAuthMessage();

        protected abstract void ServerOnClientAuthRequest(NetworkConnectionToClient conn, MirrorVRAuthMessage message);

        /// <summary>
        /// Called on the client when it receives an auth response message from the server.
        /// </summary>
        /// <param name="response">The <see langword="struct"/> containing the server's response.</param>
        protected abstract void ClientOnServerResponse(MirrorVRAuthResponse response);

        protected void ServerAcceptClient(NetworkConnectionToClient conn, short code, string data = null)
        {
            string dat = data != null ? $"{code} - {data}" : code.ToString();
            MirrorVRLogger.LogInfo($"Accepting connection '{conn.address}': {dat}");

            MirrorVRAuthResponse msg = new MirrorVRAuthResponse(code, data);
            conn.Send(msg);

            ServerAccept(conn);
        }

        protected void ServerRejectClient(NetworkConnectionToClient conn, short code, string data = null)
        {
            string dat = data != null ? $"{code} - {data}" : code.ToString();
            MirrorVRLogger.LogInfo($"Rejecting connection '{conn.address}': {dat}");

            MirrorVRAuthResponse msg = new MirrorVRAuthResponse(code, data);
            conn.Send(msg);

            StartCoroutine(DelayedReject(conn, 1));
        }

        protected void ServerBanConnection(NetworkConnectionToClient conn)
        {
            MirrorVRLogger.LogInfo($"Banning connection '{conn.address}'.");
            bannedConnections.Add(conn.address);
            ServerRejectClient(conn, BannedStatusCode, "Banned");
        }


        #region Internal
        private void ServerOnClientAuthRequestInternal(NetworkConnectionToClient conn, MirrorVRAuthMessage message)
        {
            MirrorVRLogger.LogInfo($"Connection '{conn.address}' has requested to authenticate.");
            expectedClients.Remove(conn);

            if (bannedConnections.Contains(conn.address))
            {
                MirrorVRLogger.LogInfo($"Kicking banned connection '{conn.address}'.");
                ServerRejectClient(conn, BannedStatusCode, "Banned");
                return;
            }

            if (string.IsNullOrWhiteSpace(message.data))
            {
                MirrorVRLogger.LogWarn($"Rejecting connection '{conn.address}' due to empty data.");
                ServerRejectClient(conn, MissingAuthValuesStatusCode, "Empty data value");
                return;
            }

            ServerOnClientAuthRequest(conn, message);
        }

        private void ClientOnServerResponseInternal(MirrorVRAuthResponse response)
        {
            string dat = response.data != null ? $"{response.code} - {response.data}" : response.code.ToString();
            MirrorVRLogger.LogInfo($"Authentication with server returned with code: {dat}");

            if (response.code == SuccessStatusCode || otherSuccessCodes.Contains(response.code)) ClientAccept();
            else ClientReject();

            ClientOnServerResponse(response);
        }

        private void ServerTick()
        {
            foreach (KeyValuePair<NetworkConnectionToClient, DateTime> client in expectedClients)
            {
                if (DateTime.UtcNow - client.Value >= ClientTimeout) ServerRejectClient(client.Key, ClientTimeoutStatusCode, "Request Time Out");
            }
        }

        private IEnumerator DelayedReject(NetworkConnectionToClient conn, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            ServerReject(conn);
        }
        #endregion
    }

    public struct MirrorVRAuthMessage : NetworkMessage
    {
        public MirrorVRAuthMessage(string data)
        {
            this.data = data;
            this.version = Application.version;
        }

        public string data;

        public string version { get; }
    }

    public struct MirrorVRAuthResponse : NetworkMessage
    {
        public MirrorVRAuthResponse(short code, string data)
        {
            this.code = code;
            this.data = data;
            this.version = Application.version;
        }

        public short code;
        public string data;

        public string version { get; }
    }
}
