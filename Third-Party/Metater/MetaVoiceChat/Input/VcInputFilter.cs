using UnityEngine;
using UnityEngine.Serialization;

namespace MetaVoiceChat.Input
{
    public abstract class VcInputFilter : MonoBehaviour
    {
        [Tooltip("The next audio input filter in the pipeline. This can be null.")]
        [FormerlySerializedAs("nextInputFilter")]
        public VcInputFilter optionalNextInputFilter;

        /// <summary>
        /// Usage: Setting the samples array to null will stop the pipeline and signal that the samples should not be sent. The incoming samples array may be null.
        /// </summary>
        protected abstract void Filter(int index, ref float[] samples);

        public void FilterRecursively(int index, ref float[] samples)
        {
            VcInputFilter targetInputFilter = this;
            while (targetInputFilter != null && samples != null)
            {
                if (targetInputFilter.isActiveAndEnabled)
                {
                    targetInputFilter.Filter(index, ref samples);
                }

                targetInputFilter = targetInputFilter.optionalNextInputFilter;
            }
        }

        private void OnValidate()
        {
            if (optionalNextInputFilter == this)
            {
                optionalNextInputFilter = null;
                Debug.LogWarning("Next input filter cannot be set to itself. Resetting to null.", this);
            }
        }
    }
}
