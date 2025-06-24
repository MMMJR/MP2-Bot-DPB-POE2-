using System.ComponentModel;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;

namespace MP2
{
    public class StringWrapper
    {
        public string Value { get; set; }
    }
    class Mp2SkillMoverSettings : JsonSettings
    {
        private static Mp2SkillMoverSettings _instance;
        /// <summary>The current instance for this class. </summary>
        public static Mp2SkillMoverSettings Instance
        {
            get { return _instance ?? (_instance = new Mp2SkillMoverSettings()); }
        }

        /// <summary>The default ctor. Will use the settings path "QuestPlugin".</summary>
        public Mp2SkillMoverSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, string.Format("{0}.json", "Mp2SkillMover")))
        {
        }

        private int _pathRefreshRateMs;

        private bool _debugInputApi;
        private bool _avoidWallHugging;
        private int _moveRange;
        private int _singleUseDistance;

        private int _moveMinMana;

        private bool _ignoreMobs;

        private bool _useBloodMagicValue;

        #region Basic

        /// <summary>
        /// The time in ms to refresh a path that was generated.
        /// </summary>
        [DefaultValue(32)]
        public int PathRefreshRateMs
        {
            get { return _pathRefreshRateMs; }
            set
            {
                if (value.Equals(_pathRefreshRateMs))
                {
                    return;
                }
                _pathRefreshRateMs = value;
                NotifyPropertyChanged(() => PathRefreshRateMs);
            }
        }

        [DefaultValue(false)]
        public bool DebugInputApi
        {
            get { return _debugInputApi; }
            set
            {
                if (value.Equals(_debugInputApi))
                {
                    return;
                }
                _debugInputApi = value;
                NotifyPropertyChanged(() => DebugInputApi);
            }
        }

        [DefaultValue(true)]
        public bool AvoidWallHugging
        {
            get { return _avoidWallHugging; }
            set
            {
                if (value.Equals(_avoidWallHugging))
                {
                    return;
                }
                _avoidWallHugging = value;
                NotifyPropertyChanged(() => AvoidWallHugging);
            }
        }

        [DefaultValue(33)]
        public int MoveRange
        {
            get { return _moveRange; }
            set
            {
                if (value.Equals(_moveRange))
                {
                    return;
                }
                _moveRange = value;
                NotifyPropertyChanged(() => MoveRange);
            }
        }

        [DefaultValue(18)]
        public int SingleUseDistance
        {
            get { return _singleUseDistance; }
            set
            {
                if (value.Equals(_singleUseDistance))
                {
                    return;
                }
                _singleUseDistance = value;
                NotifyPropertyChanged(() => SingleUseDistance);
            }
        }
        #endregion
        [DefaultValue(20)]
        public int MoveMinManaValue
        {
            get { return _moveMinMana; }
            set
            {
                if (value.Equals(_moveMinMana))
                {
                    return;
                }
                _moveMinMana = value;
                NotifyPropertyChanged(() => MoveMinManaValue);
            }
        }


        [DefaultValue(false)]
        public bool IgnoreMobsValue
        {
            get { return _ignoreMobs; }
            set
            {
                if (value.Equals(_ignoreMobs))
                {
                    return;
                }
                _ignoreMobs = value;
                NotifyPropertyChanged(() => IgnoreMobsValue);
            }
        }

        [DefaultValue(false)]
        public bool UseBloodMagicValue
        {
            get { return _useBloodMagicValue; }
            set
            {
                if (value.Equals(_useBloodMagicValue))
                {
                    return;
                }
                _useBloodMagicValue = value;
                NotifyPropertyChanged(() => UseBloodMagicValue);
            }
        }
    }
}
