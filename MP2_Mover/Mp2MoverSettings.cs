using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MP2
{
    public class StringWrapper
    {
        public string Value { get; set; }
    }
    internal class Mp2MoverSettings : JsonSettings
	{
        private static Mp2MoverSettings _instance;
		public static Mp2MoverSettings Instance => _instance ??= new Mp2MoverSettings();

        private int _pathRefreshRateMs;
        private int _UseDashInterval;
        private ObservableCollection<StringWrapper> _forcedAdjustmentAreas;
        private bool _forceAdjustCombatAreas;
        private bool _forceAdjustMousePointer;
        private bool _avoidWallHugging;
        private int _moveRange;
        private int _singleUseDistance;
        private bool _UseDash;

        private Mp2MoverSettings()
			: base(GetSettingsFilePath(Configuration.Instance.Name, $"{nameof(Mp2MoverSettings)}.json"))
		{
            if (_forcedAdjustmentAreas == null)
            {
                _forcedAdjustmentAreas = new ObservableCollection<StringWrapper> {
                    new StringWrapper { Value = "The City of Sarn" } };
            }
        }

        [DefaultValue(false)]
        public bool ForceAdjustCombatAreas
        {
            get { return _forceAdjustCombatAreas; }
            set
            {
                if (value.Equals(_forceAdjustCombatAreas))
                {
                    return;
                }
                _forceAdjustCombatAreas = value;
                NotifyPropertyChanged(() => ForceAdjustCombatAreas);
            }
        }

        [DefaultValue(true)]
        public bool ForceAdjustMousePointer
        {
            get { return _forceAdjustMousePointer; }
            set
            {
                if (value.Equals(_forceAdjustMousePointer))
                {
                    return;
                }
                _forceAdjustMousePointer = value;
                NotifyPropertyChanged(() => _forceAdjustMousePointer);
            }
        }

        /// <summary>
        /// A list of areas to force movement adjustments on.
        /// </summary>
        public ObservableCollection<StringWrapper> ForcedAdjustmentAreas
        {
            get
            {
                return _forcedAdjustmentAreas;
            }
            set
            {
                if (value.Equals(_forcedAdjustmentAreas))
                {
                    return;
                }
                _forcedAdjustmentAreas = value;
                NotifyPropertyChanged(() => ForcedAdjustmentAreas);
            }
        }

        /// <summary>
        /// The time in ms to refresh a path that was generated.
        /// </summary>
        [DefaultValue(30)]
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

        [DefaultValue(1600)]
        public int UseDashInterval
        {
            get { return _UseDashInterval; }
            set
            {
                if (value.Equals(_UseDashInterval))
                {
                    return;
                }
                _UseDashInterval = value;
                NotifyPropertyChanged(() => UseDashInterval);
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

        [DefaultValue(35)]
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

        [DefaultValue(true)]
        public bool UseDash
        {
            get { return _UseDash; }
            set
            {
                if (value.Equals(_UseDash))
                {
                    return;
                }
                _UseDash = value;
                NotifyPropertyChanged(() => UseDash);
            }
        }
    }
}