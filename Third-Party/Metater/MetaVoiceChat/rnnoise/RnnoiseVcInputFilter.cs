// To use rnnoise in MetaVoiceChat:
// 1. Install Adrenak's RNNoise4Unity using the instructons here: https://github.com/adrenak/RNNoise4Unity
// 2. Uncomment #define ENABLE_RNNOISE_FOR_META_VOICE_CHAT below

//#define ENABLE_RNNOISE_FOR_META_VOICE_CHAT

#if ENABLE_RNNOISE_FOR_META_VOICE_CHAT
using Adrenak.RNNoise4Unity;
using UnityEngine;
using System;
#endif

using MetaVoiceChat.Input;

namespace MetaVoiceChat.Rnnoise
{
    public class RnnoiseVcInputFilter : VcInputFilter
    {
        public MetaVc metaVc;

#if ENABLE_RNNOISE_FOR_META_VOICE_CHAT
        private const int DenoiserFramesize = 480;

        private Denoiser denoiser;
        private int multiples = 0;

        private readonly float[] buffer = new float[DenoiserFramesize];

        private void OnEnable()
        {
            if (metaVc == null)
            {
                Debug.LogError("MetaVc is not assigned. Please assign it in the inspector.");
                return;
            }

            var config = metaVc.config;
            if (config.samplesPerFrame % DenoiserFramesize != 0)
            {
                Debug.LogError($"RnnoiseVcInputFilter requires samplesPerFrame to be a multiple of {DenoiserFramesize}. Please adjust the configuration.");
                return;
            }

            multiples = config.samplesPerFrame / DenoiserFramesize;
            denoiser = new Denoiser();
        }

        private void OnDisable()
        {
            denoiser?.Dispose();
            denoiser = null;
            multiples = 0;
        }
#endif

        protected override void Filter(int index, ref float[] samples)
        {
#if ENABLE_RNNOISE_FOR_META_VOICE_CHAT
            if (denoiser == null || multiples == 0)
            {
                return;
            }

            if (samples == null || samples.Length == 0)
            {
                return;
            }

            if (samples.Length != multiples * DenoiserFramesize)
            {
                Debug.LogWarning($"RnnoiseVcInputFilter requires samples to be of length {multiples * DenoiserFramesize}. Please adjust the configuration.");
                return;
            }

            for (int i = 0; i < multiples; i++)
            {
                //var buffer = FixedLengthArrayPool<float>.Rent(DenoiserFramesize);

                // Copy the samples into the buffer
                Array.Copy(samples, i * DenoiserFramesize, buffer, 0, DenoiserFramesize);

                denoiser.Denoise(buffer);

                // Copy the denoised samples back to the original samples array
                Array.Copy(buffer, 0, samples, i * DenoiserFramesize, DenoiserFramesize);

                //FixedLengthArrayPool<float>.Return(buffer);
            }
#endif
        }
    }
}