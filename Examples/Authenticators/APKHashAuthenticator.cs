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
using System.Threading.Tasks;
using UnityEngine;


namespace Mirror.VR.Authentication
{
    /// <summary>
    /// For P2P only. Mirror Network Authenticator Example that checks if you have the same APK before connecting.
    /// </summary>
    public class APKHashAuthenticator : MirrorVRAuthenticatorBase
    {
        private static string APKHash;
        private const string EditorHash = "Editor"; //change this to whatever you want, it does not matter what this is

        protected override List<short> otherSuccessCodes => new List<short>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void GetHash()
        {
#if UNITY_EDITOR
            APKHash = EditorHash;
#elif UNITY_ANDROID
            Task.Run(() =>
            {
                using var stream = System.IO.File.OpenRead(Application.dataPath);
                using var sha = System.Security.Cryptography.SHA256.Create();
                APKHash = System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            });
#endif
        }

        protected override Task<string> ClientSendAuthMessage()
        {
            return Task.FromResult(APKHash);
        }

        protected override void ServerOnClientAuthRequest(NetworkConnectionToClient conn, MirrorVRAuthMessage message)
        {
#if UNITY_EDITOR
            ServerAcceptClient(conn, 200);
#elif UNITY_ANDROID

            if (conn == NetworkServer.localConnection || message.data == EditorHash) ServerAcceptClient(conn, 200);
            else
            {
                if (message.data == APKHash) ServerAcceptClient(conn, 200);
                else ServerRejectClient(conn, AuthFailedStatusCode, "Invalid version detected");
            }
#endif
        }

        protected override void ClientOnServerResponse(MirrorVRAuthResponse response)
        {
            //no further action needed, base already handles most of it.
        }
    }
}
