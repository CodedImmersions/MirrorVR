using UnityEngine;
using UnityEngine.Serialization;

namespace MetaVoiceChat.Output
{
    public abstract class VcOutputFilter : MonoBehaviour
    {
        [Tooltip("The next audio output filter in the pipeline. This can be null.")]
        [FormerlySerializedAs("nextOutputFilter")]
        public VcOutputFilter optionalNextOutputFilter;

        /// <summary>
        /// Usage: Directly modify the samples array to achieve the desired filter. The incoming samples array may be null.
        /// </summary>
        protected abstract void Filter(int index, float[] samples, float targetLatency);

        public void FilterRecursively(int index, float[] samples, float targetLatency)
        {
            VcOutputFilter targetOutputFilter = this;
            while (targetOutputFilter != null && samples != null)
            {
                if (targetOutputFilter.isActiveAndEnabled)
                {
                    targetOutputFilter.Filter(index, samples, targetLatency);
                }

                targetOutputFilter = targetOutputFilter.optionalNextOutputFilter;
            }
        }

        private void OnValidate()
        {
            if (optionalNextOutputFilter == this)
            {
                optionalNextOutputFilter = null;
                Debug.LogWarning("Next output filter cannot be set to itself. Resetting to null.", this);
            }
        }
    }
}