using NAudio.Mixer;

namespace Flow.Launcher.Plugin.AudioDeviceSelector
{
    public class DeviceInfo
    {
        public string name;
        public string glyph;

        public DeviceInfo(string name)
        {
            this.Set(name);
        }

        public void Set(string name)
        {
            this.name = name;
            this.glyph = GetDeviceTypeGlyph(name);
        }

        private MixerLineComponentType GetDeviceType(string friendlyName)
        {
            foreach (Mixer mixer in Mixer.Mixers)
            {
                if (friendlyName.StartsWith(mixer.Name) && mixer.DestinationCount > 0)
                {
                    return mixer.GetDestination(0).ComponentType;
                }
            }

            return MixerLineComponentType.DestinationUndefined;
        }

        private string GetDeviceTypeGlyph(string friendlyName)
        {
            switch (GetDeviceType(friendlyName))
            {
                case MixerLineComponentType.DestinationDigital:
                case MixerLineComponentType.DestinationLine:
                case MixerLineComponentType.DestinationWaveIn:
                    return "\ue95f"; // Wire glyph
                case MixerLineComponentType.DestinationMonitor:
                    return "\ue7f4"; // TVMonitor glyph
                case MixerLineComponentType.DestinationSpeakers:
                    return "\ue7f5"; // Speakers glyph
                case MixerLineComponentType.DestinationHeadphones:
                    return "\ue7f6"; // Headphone glyph
                case MixerLineComponentType.DestinationTelephone:
                    return "\ue717"; // Phone glyph
                case MixerLineComponentType.DestinationVoiceIn:
                    return "\uefa9"; // Speech glyph
                case MixerLineComponentType.DestinationUndefined:
                default:
                    return "\ue767"; // Volume glyph
            }
        }
    }
}
