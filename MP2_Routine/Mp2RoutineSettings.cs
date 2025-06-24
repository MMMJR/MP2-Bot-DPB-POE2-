using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MP2
{
    public class StringWrapper
    {
        public string Value { get; set; }
    }
    internal class Mp2RoutineSettings : JsonSettings
	{
        private static Mp2RoutineSettings _instance;
		public static Mp2RoutineSettings Instance => _instance ??= new Mp2RoutineSettings();

        private Mp2RoutineSettings()
			: base(GetSettingsFilePath(Configuration.Instance.Name, $"{nameof(Mp2RoutineSettings)}.json"))
		{
            if (MapperRoutineSelector == null)
                MapperRoutineSelector = SetupMapperRoutineConfiguration();
        }

        private int _combatRange;
        private bool _skipShrines;
        private bool _alwaysAttackInPlace;
        private bool _CastOnMe;
        private bool _Simulacrum;

        private string _userKey;
        public string UserKey
        {
            get { return _userKey; }
            set
            { _userKey = value; NotifyPropertyChanged(() => UserKey); }
        }
        private string _daysLeft;
        public string DaysLeft
        {
            get { return _daysLeft; }
            set
            { _daysLeft = value; NotifyPropertyChanged(() => DaysLeft); }
        }

        [DefaultValue(70)]
        public int CombatRange
        {
            get { return _combatRange; }
            set
            {
                if (value.Equals(_combatRange))
                {
                    return;
                }
                _combatRange = value;
                NotifyPropertyChanged(() => CombatRange);
            }
        }

        [DefaultValue(false)]
        public bool SkipShrines
        {
            get { return _skipShrines; }
            set
            {
                _skipShrines = value;
                NotifyPropertyChanged(() => SkipShrines);
            }
        }

        [DefaultValue(false)]
        public bool Simulacrum
        {
            get { return _Simulacrum; }
            set
            {
                if (value.Equals(_Simulacrum))
                {
                    return;
                }
                _Simulacrum = value;
                NotifyPropertyChanged(() => Simulacrum);
            }
        }

        [DefaultValue(false)]
        public bool AlwaysAttackInPlace
        {
            get { return _alwaysAttackInPlace; }
            set
            {
                if (value.Equals(_alwaysAttackInPlace))
                {
                    return;
                }
                _alwaysAttackInPlace = value;
                NotifyPropertyChanged(() => AlwaysAttackInPlace);
            }
        }

        private ObservableCollection<MapperRoutineSkill> _mapperRoutineSelector;
        public ObservableCollection<MapperRoutineSkill> MapperRoutineSelector
        {
            get => _mapperRoutineSelector;
            set
            {
                _mapperRoutineSelector = value;
                NotifyPropertyChanged(() => MapperRoutineSelector);
            }
        }

        // Settings static types mostly used in the Gui
        [JsonIgnore] 
        private static List<int> _allSkillSlots;

        /// <summary>List of all available skill slots </summary>
        [JsonIgnore]
        public static List<int> AllSkillSlots => _allSkillSlots ?? (_allSkillSlots = new List<int>
        {
            -1,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10,
            11,
            12,
            13
        });

        private ObservableCollection<MapperRoutineSkill> SetupMapperRoutineConfiguration()
        {
            ObservableCollection<MapperRoutineSkill> _modss = new ObservableCollection<MapperRoutineSkill>
            {
                new MapperRoutineSkill("Temporalis Move:", "Space", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "1", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "2", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "3", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "4", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "5", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "6", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "7", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "8", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "9", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "10", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "11", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "12", false, false, "0", 1),
                new MapperRoutineSkill("Skillbar Slot:", "13", false, false, "0", 1)
            };
            return _modss;
        }
    }
}