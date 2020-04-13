using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace LibAlerts
{
    public class VoiceSynth
    {
        private SpeechSynthesizer synth;

        public VoiceSynth()
        {
            // Initialize a new instance of the SpeechSynthesizer.  
            synth = new SpeechSynthesizer();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();

            synth.Volume = 5;

            synth.SelectVoiceByHints(VoiceGender.Female);
        }

        public void Speak(string text)
        {
            // Speak a string.  
            synth.SpeakAsync(text);
        }
    }
}
