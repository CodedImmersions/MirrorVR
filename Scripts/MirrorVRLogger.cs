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

using UnityEngine;

namespace Mirror.VR
{
    public static class MirrorVRLogger
    {
        public static LogLevel level {  get; private set; }
        internal static bool HasInit;

        internal static void Init(LogLevel loggerLevel)
        {
            level = loggerLevel;
            HasInit = true;
        }

        internal static void LogInfo(string message)
        {
            if (!HasInit) return;

            if (level == LogLevel.Info)
            {
                if (Application.isEditor) Debug.Log($"<color=#0390fc>[MirrorVR]</color> {message}");
                else Debug.Log($"[MirrorVR] {message}");
            }
        }

        internal static void LogWarn(string message)
        {
            if (!HasInit) return;

            if (level == LogLevel.Warn
                || level == LogLevel.Info)
            {
                if (Application.isEditor) Debug.LogWarning($"<color=#0390fc>[MirrorVR]</color> {message}");
                else Debug.LogWarning($"[MirrorVR] {message}");
            }
        }

        internal static void LogError(string message)
        {
            if (!HasInit) return;

            if (level == LogLevel.Error
                || level == LogLevel.Warn
                || level == LogLevel.Info)
            {
                if (Application.isEditor) Debug.LogError($"<color=#0390fc>[MirrorVR]</color> {message}");
                else Debug.LogError($"[MirrorVR] {message}");
            }
        }
    }

    public enum LogLevel { Info, Warn, Error, None }
}
