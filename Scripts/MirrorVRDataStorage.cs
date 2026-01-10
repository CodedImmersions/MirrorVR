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

using Epic.OnlineServices;
using EpicTransport;
using PlayEveryWare.EpicOnlineServices.Samples;
using System;
using System.Collections;
using UnityEngine;

namespace Mirror.VR
{
    public class MirrorVRDataStorage : MonoBehaviour
    {
        public static MirrorVRDataStorage instance { get; private set; }
        private static bool cancontinue = false;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                throw new NotSupportedException("You already have another MirrorVRDataStorage in the scene. You may only have one per scene.");

            StartCoroutine(DataFix());
        }

        private IEnumerator DataFix()
        {
            yield return new WaitUntil(() => EOSManager.Initialized);

           PlayerDataStorageService.Instance.QueryFileList();
        }


        /// <summary>
        /// Retrieves the content of a file in string format.
        /// </summary>
        /// <param name="FileName">The name of the file you want to retrieve. Includes file extension.</param>
        /// <param name="Callback">The callback that will contain your content.</param>
        /// <returns>The content of the file in string format</returns>
        public static void PlayerDataStorageRetrieveContent(string FileName, Action<string> Callback)
        {
            MirrorVRLogger.LogInfo($"Attempting to retrieve file \"{FileName}\"");
            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn("Cannot use Player Data Storage before EOSManager is initialized. Please wait until EOSManager is initialized.");
                return;
            }

            MirrorVRLogger.LogInfo($"Downloading file \"{FileName}\"");

            

            string RetrievedContent = "";

            
            PlayerDataStorageService.Instance.DownloadFile(FileName);

            instance.StartCoroutine(WaitForDownload(FileName, (res) =>
            {
                RetrievedContent = PlayerDataStorageService.Instance.GetCachedFileContent(FileName);

                Callback.Invoke(RetrievedContent);
                MirrorVRLogger.LogInfo($"Succesfully retrieved content for {FileName}");
            }));
        }

        private static IEnumerator WaitForDownload(string FileName, Action<bool> Callback)
        {
            PlayerDataStorageService.Instance.OnFileDownloaded += OnFileDownloaded;
            yield return new WaitUntil(() => cancontinue);
            PlayerDataStorageService.Instance.OnFileDownloaded -= OnFileDownloaded;

            Callback.Invoke(true);
        }

        /// <summary>
        /// Write to a file if it exists, if it doesn't it will create it.
        /// </summary>
        /// <param name="FileName">The name of the file you want to write.</param>
        /// <param name="Content">The content of the file you want to write.</param>
        public static void PlayerDataStorageWriteFile(string FileName, string Content)
        {
            if (!EOSManager.Initialized)
            {
                MirrorVRLogger.LogWarn("Cannot use Player Data Storage before EOSManager is initialized. Please wait until EOSManager is initialized.");
                return;
            }

            MirrorVRLogger.LogInfo("Writing file");
            PlayerDataStorageService.Instance.AddFile(FileName, Content, EOSManager.LocalUserProductID);
        }

        public static bool FileExists(string filename) => PlayerDataStorageService.Instance.GetLocallyCachedData().ContainsKey(filename);

        private static void OnFileDownloaded(Result result)
        {
            if (result == Result.Success) cancontinue = true;
            else MirrorVRLogger.LogWarn($"Couldn't download file. Result: {result}");
        }
    }
}

