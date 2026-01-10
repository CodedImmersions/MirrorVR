using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace MetaVoiceChat.Input.Mic
{
    public class VcMicAudioInput : VcAudioInput
    {
        public event Action<string> OnActiveDeviceChanged;

        public string ActiveDevice => Mic?.ActiveDevice ?? null;

        public VcMic Mic { get; private set; } = null;

        public bool IsInitialized => Mic != null;

        public override void StartLocalPlayer()
        {
            int samplesPerFrame = metaVc.config.samplesPerFrame;

            Mic = new(this, samplesPerFrame);
            Mic.OnFrameReady += SendAndFilterFrame;
            Mic.OnActiveDeviceChanged += Mic_OnActiveDeviceChanged;

            if (Mic.Devices.Length > 0)
            {
                Mic.StartRecording();
            }

            StartCoroutine(CoReconnect());
        }

        private void OnDestroy()
        {
            if (Mic == null)
            {
                return;
            }

            Mic.OnFrameReady -= SendAndFilterFrame;
            Mic.OnActiveDeviceChanged -= Mic_OnActiveDeviceChanged;
            Mic.Dispose();
            Mic = null;

            StopAllCoroutines();
        }

        private IEnumerator CoReconnect()
        {
            yield return new WaitForSecondsRealtime(1f);

            while (Mic != null)
            {
                while (!ShouldReconnect())
                {
                    yield return null;
                }

                if (Mic == null)
                {
                    yield break;
                }

                Mic.StopRecording();

                yield return null;
                yield return null;

                if (Mic == null)
                {
                    yield break;
                }

                if (Mic.Devices.Length > 0)
                {
                    bool success = Mic.StartRecording();
                    if (!success)
                    {
                        // Wait a long time before trying again to avoid spamming warnings
                        yield return new WaitForSecondsRealtime(4f);
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(1f);
                }

                yield return null;
                yield return null;
            }
        }

        private void Mic_OnActiveDeviceChanged(string device)
        {
            OnActiveDeviceChanged?.Invoke(device);
        }

        public void SetSelectedDevice(string device)
        {
            if (Mic == null)
            {
                return;
            }

            Mic.SetSelectedDevice(device);
        }

        private bool ShouldReconnect()
        {
            if (Mic == null)
            {
                return true;
            }

            if (!Mic.IsRecording || !Mic.Devices.Contains(Mic.ActiveDevice))
            {
                return true;
            }

            if (Mic.SelectedDevice != Mic.ActiveDevice && Mic.Devices.Contains(Mic.SelectedDevice))
            {
                return true;
            }

            return false;
        }
    }
}