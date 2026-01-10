using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetaVoiceChat.Utils
{
    public class MicrophoneDevicesListener
    {
        private readonly HashSet<string> devices = new();
        private readonly Action onDevicesChanged;

        public MicrophoneDevicesListener(Action onDevicesChanged)
        {
            this.onDevicesChanged = onDevicesChanged;
        }

        public void Poll()
        {
            var actualDevices = Microphone.devices;
            if (!HasChanged(actualDevices))
            {
                return;
            }

            devices.Clear();
            foreach (var device in actualDevices)
            {
                devices.Add(device);
            }

            onDevicesChanged?.Invoke();
        }

        private bool HasChanged(string[] actualDevices)
        {
            if (actualDevices.Length != devices.Count)
            {
                return true;
            }

            foreach (var device in actualDevices)
            {
                if (!devices.Contains(device))
                {
                    return true;
                }
            }

            return false;
        }
    }
}