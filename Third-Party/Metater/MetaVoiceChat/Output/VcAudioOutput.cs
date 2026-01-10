using UnityEngine;
using UnityEngine.Serialization;

namespace MetaVoiceChat.Output
{
    public abstract class VcAudioOutput : MonoBehaviour
    {
        public MetaVc metaVc;
        [Tooltip("The first audio output filter in the pipeline. This can be null.")]
        [FormerlySerializedAs("firstOutputFilter")]
        public VcOutputFilter optionalFirstOutputFilter;

        protected abstract void ReceiveFrame(int index, float[] samples, float targetLatency);

        public void ReceiveAndFilterFrame(int index, float[] samples, float targetLatency)
        {
            if (optionalFirstOutputFilter != null)
            {
                optionalFirstOutputFilter.FilterRecursively(index, samples, targetLatency);
            }

            ReceiveFrame(index, samples, targetLatency);
        }
    }
}