using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MetaVoiceChat.Input
{
    public abstract class VcAudioInput : MonoBehaviour
    {
        public MetaVc metaVc;
        [Tooltip("The first audio input filter in the pipeline. This can be null.")]
        [FormerlySerializedAs("firstInputFilter")]
        public VcInputFilter optionalFirstInputFilter;

        public event Action<int, float[]> OnFrameReady;

        public abstract void StartLocalPlayer();

        protected void SendAndFilterFrame(int index, float[] samples)
        {
            if (optionalFirstInputFilter != null)
            {
                optionalFirstInputFilter.FilterRecursively(index, ref samples);
            }

            OnFrameReady?.Invoke(index, samples);
        }
    }
}