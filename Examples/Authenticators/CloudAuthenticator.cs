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
using UnityEngine.Networking;

namespace Mirror.VR.Authentication
{
    /// <summary>
    /// Custom API Endpoint Authenticator to authenticate your players through the cloud.
    /// </summary>
    public class CloudAuthenticator : MirrorVRAuthenticatorBase
    {
        protected override List<short> otherSuccessCodes => new List<short>() { }; //put your extra success codes here.

        private const string URL = "https://httpbin.org/status/200"; //put your own endpoint here.
        private const string HTTPMethod = "POST"; //put your own endpoint here.

        protected override void ClientOnServerResponse(MirrorVRAuthResponse response)
        {
            /*
             * Interpret your own return code here.
             * ClientAccept/ClientReject are automatically called, based on whether the code is either "200" or any code in "otherSuccessCodes".
             * 
             * So yes, all this is really used for is if you want to do your own custom logic, but this isn't required.
             */
        }

        protected override Task<string> ClientSendAuthMessage()
        {
            /*
             * Insert your own data string here, to be sent to the server for authentication.
             * This is a REQUIRED method, as the server checks for a null, empty, or white space string automatically, and will fail if either of those are true.
             * 
             * This is a task, in case you want to send a web request somewhere.
             * If a request usually takes longer than 30 seconds (or even 25, just to be safe), override ClientTimeout from MirrorVRAuthenticatorBase to set how long the client has before being auto-kicked by the server.
             */

            throw new System.NotImplementedException();
        }

        protected override void ServerOnClientAuthRequest(NetworkConnectionToClient conn, MirrorVRAuthMessage message)
        {
            UnityWebRequest uwr = new UnityWebRequest(URL, HTTPMethod);
            uwr.timeout = 5;

            AsyncOperation op = uwr.SendWebRequest();
            while (!op.isDone) Task.Yield();
            //interpret uwr.responseCode here, and then send a response back to the client with ServerAcceptClient() and ServerRejectClient().
        }

        /* EXTRA NOTES
         * 
         * 1. You can get the client (NOT host)'s Product User ID automatically from conn.address. Don't use for host client, as it will just return 'localhost'.
         * 2. You can get the client/host's version from message.version automatically.
         * 3. Use ServerBanConnection() to prevent conn from joining back to this lobby session.
         */

    }
}
