using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace MP2
{
    public class StringWrapper
    {
        public string Value { get; set; }
    }
    internal class Mp2AutoLoginSettings : JsonSettings
	{
        private static Mp2AutoLoginSettings _instance;
		public static Mp2AutoLoginSettings Instance => _instance ??= new Mp2AutoLoginSettings();

        private Mp2AutoLoginSettings()
			: base(GetSettingsFilePath(Configuration.Instance.Name, $"{nameof(Mp2AutoLoginSettings)}.json"))
		{

        }

        private string _character;

        public string Character
        {
            get => _character;
            set
            {
                if (value == _character) return;
                _character = value;
                NotifyPropertyChanged(() => Character);
            }
        }

        public float LoginDelayInitial { get; set; } = 0.5f;
        public float LoginDelayStep { get; set; } = 3;
        public float LoginDelayFinal { get; set; } = 300;
        public int LoginDelayRandPct { get; set; } = 15;
        public float CharSelectDelay { get; set; } = 0.5f;

        public bool LoginUsingUserCredentials { get; set; }
        public bool LoginUsingGateway { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Gateway { get; set; }


        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (string.IsNullOrEmpty(Password))
                return;

            // Encrypt the key when serializing to file.
            Password = GlobalSettings.Crypto.EncryptStringAes(Password, "autologinsharedsecret");
        }

        [OnSerialized]
        internal void OnSerialized(StreamingContext context)
        {
            if (string.IsNullOrEmpty(Password))
                return;

            // Decrypt the key when we're done serializing, so we can have the plain-text version back.
            Password = GlobalSettings.Crypto.DecryptStringAes(Password, "autologinsharedsecret");
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            // Make sure we decrypt the license key, so we can use it.
            OnSerialized(context);
        }

        public static readonly string[] GatewayList =
        {
            "Auto-select Gateway",
            "Texas (US)",
            "Washington, D.C. (US)",
            "California (US)",
            "Amsterdam (EU)",
            "London (EU)",
            "Frankfurt (EU)",
            "Milan (EU)",
            "Singapore",
            "Australia",
            "Sao Paulo (BR)",
            "Paris (EU)",
            "Moscow (RU)",
            "Japan"
        };
    }
}