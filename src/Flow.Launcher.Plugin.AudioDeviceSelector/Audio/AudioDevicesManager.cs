using Flow.Launcher.Plugin.AudioDeviceSelector.Audio.Interop;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Flow.Launcher.Plugin.AudioDeviceSelector.Components;
using NAudio.CoreAudioApi.Interfaces;
using PropertyKeys = NAudio.CoreAudioApi.PropertyKeys;
using NAudio.Mixer;

namespace Flow.Launcher.Plugin.AudioDeviceSelector.Audio
{
    public class AudioDevicesManager : IMMNotificationClient, IDisposable
    {
        public Settings Settings { get; }
        
        private List<MMDevice> devices = null;
        private DateTime lastDeviceUpdateTimeStamp = DateTime.Now;
        private int updateIntervalSeconds = 5;
        private MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
        private readonly Dictionary<string, DeviceInfo> deviceCacheMap = new();

        public List<MMDevice> Devices 
        { 
            get {
                if (devices == null)
                    devices = GetDevices();

                return devices; 
            } 
        }

        public AudioDevicesManager(Settings settings) {
            Settings = settings;
            if (Settings.CacheDeviceNames) {
                deviceEnumerator.RegisterEndpointNotificationCallback(this);
            }

            Settings.PropertyChanged += SettingsOnPropertyChanged;
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(Settings.CacheDeviceNames)) return;
            if (Settings.CacheDeviceNames)
                deviceEnumerator.RegisterEndpointNotificationCallback(this);
            else {
                deviceEnumerator.UnregisterEndpointNotificationCallback(this);
                deviceCacheMap.Clear();
            }
        }

        public DeviceInfo GetDeviceInfoFromCache(MMDevice device) {
            var id = device.ID;
            if (deviceCacheMap.ContainsKey(id)) {
                return deviceCacheMap[id];
            }

            deviceCacheMap[id] = new DeviceInfo(device.FriendlyName);
            return deviceCacheMap[id];
        }
        
        public List<MMDevice> GetDevices()
        {
            var datetime1 = DateTime.Now;
            var devices = new List<MMDevice>();

            var endpoints = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var endpoint in endpoints)
            {
                if (Settings.CacheDeviceNames) {
                    // Puts the name in cache if it's not there yet
                    GetDeviceInfoFromCache(endpoint);
                }

                devices.Add(endpoint);
            }

            return devices;
        }

        public void UpdateDevices()
        {
            DateTime currentTime = DateTime.Now;
            if (devices == null || (currentTime - lastDeviceUpdateTimeStamp).TotalSeconds > updateIntervalSeconds)
            {
                devices = GetDevices();
                lastDeviceUpdateTimeStamp = currentTime;
            }
        }

        public bool SetDevice(string deviceFriendlyName)
        {
            var devices = GetDevices();
            var device = devices.Find(d => d.FriendlyName == deviceFriendlyName);
            if (device == null)
                return false;

            var policy = new PolicyConfigClientWin7();
            policy.SetDefaultEndpoint(device.ID, ERole.eMultimedia);

            return true;
        }

        public string GetDeviceTitle(string friendyName, TitleTypeSettings titleType)
        {
            Regex regex = new Regex("^(?<deviceName>.*?)\\s?(?:\\((?<description>.*?)\\))?$");
            MatchCollection matches = regex.Matches(friendyName);

            if (matches.Count > 0)
            {
                var match = matches[0];

                if (match.Success)
                {
                    switch (titleType)
                    {
                        case TitleTypeSettings.FriendlyName:
                            return friendyName;
                        case TitleTypeSettings.DeviceName:
                            var title = match.Groups["deviceName"].Value;
                            if (!string.IsNullOrEmpty(title))
                            {
                                return match.Groups["deviceName"].Value;
                            }
                            break;
                        case TitleTypeSettings.DeviceDescription:
                            title = match.Groups["description"].Value;
                            if (string.IsNullOrEmpty(title))
                            {
                                return match.Groups["deviceName"].Value;
                            }

                            return title;
                    }
                }
            }

            return friendyName;
        }

        public static MixerLineComponentType GetDeviceType(string friendlyName)
        {
            foreach (Mixer mixer in Mixer.Mixers)
            {
                // mixer.Name length is limited by MAXPNAMELEN=32 cuz windows stuff, thats why it's not Equals comparison
                if (friendlyName.StartsWith(mixer.Name) && mixer.DestinationCount > 0)
                {
                    return mixer.GetDestination(0).ComponentType;
                }
            }

            return MixerLineComponentType.DestinationUndefined;
        }

        public static string GetDeviceTypeUnicode(string friendlyName)
        {
            switch (GetDeviceType(friendlyName))
            {
                case MixerLineComponentType.DestinationDigital:
                case MixerLineComponentType.DestinationLine:
                case MixerLineComponentType.DestinationWaveIn:
                    return "e95f"; // Wire glyph
                case MixerLineComponentType.DestinationMonitor:
                    return "e7f4"; // TVMonitor glyph
                case MixerLineComponentType.DestinationSpeakers:
                    return "e7f5"; // Speakers glyph
                case MixerLineComponentType.DestinationHeadphones:
                    return "e7f6"; // Headphone glyph
                case MixerLineComponentType.DestinationTelephone:
                    return "e717"; // Phone glyph
                case MixerLineComponentType.DestinationVoiceIn:
                    return "efa9"; // Speech glyph
                case MixerLineComponentType.DestinationUndefined:
                    return "e783"; // Error glyph
                default:
                    return "e767"; // Volume glyph
            }
        }

        public static char GetDeviceTypeGlyph(string friendlyName)
        {
            int codePoint = int.Parse(GetDeviceTypeUnicode(friendlyName), System.Globalization.NumberStyles.HexNumber);
            return (char)codePoint;
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

        public void OnDeviceAdded(string pwstrDeviceId) { }

        public void OnDeviceRemoved(string deviceId) {
            deviceCacheMap.Remove(deviceId);
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) { }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) {
            if (!Settings.CacheDeviceNames) return;
            if (key.propertyId != PropertyKeys.PKEY_Device_FriendlyName.propertyId &&
                key.propertyId != PropertyKeys.PKEY_DeviceInterface_FriendlyName.propertyId)
                return;
            var device = deviceEnumerator.GetDevice(pwstrDeviceId);
            deviceCacheMap[device.ID].Set(device.FriendlyName);
        }

        public void Dispose() {
            Settings.PropertyChanged -= SettingsOnPropertyChanged;
            if (!Settings.CacheDeviceNames) return;
            deviceEnumerator.UnregisterEndpointNotificationCallback(this);
        }
    }
}
