namespace MetaVoiceChat.Output.Multicast
{
    public class MulticastVcAudioOutput : VcAudioOutput
    {
        public VcAudioOutput[] multicastOutputs;

        protected override void ReceiveFrame(int index, float[] samples, float targetLatency)
        {
            if (multicastOutputs == null)
            {
                return;
            }

            foreach (var output in multicastOutputs)
            {
                if (output != null)
                {
                    output.ReceiveAndFilterFrame(index, samples, targetLatency);
                }
            }
        }
    }
}