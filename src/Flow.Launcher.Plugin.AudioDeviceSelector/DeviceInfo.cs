using Flow.Launcher.Plugin.AudioDeviceSelector.Audio;

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
            this.glyph = AudioDevicesManager.GetDeviceTypeGlyph(name).ToString();
        }
    }
}
