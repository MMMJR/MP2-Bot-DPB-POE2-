using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game.GameData;
using MP2.Class;
using MP2.EXtensions;
using MP2.EXtensions.Mapper;
using MP2.EXtensions.Tasks;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace MP2
{
    public class Mp2Settings : JsonSettings
	{
		private static Mp2Settings _instance;
		public static Mp2Settings Instance => _instance ??= new Mp2Settings();
		private Mp2Settings()
			: base(GetSettingsFilePath(Configuration.Instance.Name, $"{nameof(Mp2Settings)}.json"))
		{
            InitList();
            //Load();
            InitDict();

            MapList = MapList.OrderByDescending(m => m.Priority).ToList();

            InitGeneralStashingRules();
            InitCurrencyStashingRules();
            InitChestEntries(ref _chests, GetDefaultChestList);
            InitChestEntries(ref _strongboxes, GetDefaultStrongboxList);
            InitChestEntries(ref _shrines, GetDefaultShrineList);

            if (InventoryCurrencies == null)
            {
                InventoryCurrencies = new ObservableCollection<InventoryCurrency>
                {
                    new InventoryCurrency(CurrencyNames.Wisdom, 5, 12),
                    new InventoryCurrency(CurrencyNames.Portal, 5, 11),
                };
            }

            InitListAffixes();
            //LoadAffixes();
            InitDictAffix();

            //Configuration.OnSaveAll += (sender, args) => { Save(); SaveAffixes(); };

            AffixList = AffixList
                .OrderByDescending(a => a.RerollMagic)
                .ThenByDescending(a => a.RerollRare)
                .ThenBy(a => a.Name)
                .ToList();

            if (Flasks == null)
                Flasks = SetupDefaultFlasks();

            _maxCombatDistance = 70;
            _maxLootDistance = 140;
        }

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

        #region GeneralSettings
        public bool UseHideout { get; set; }
        public int MaxMapTier { get; set; } = 10;
        public int MobRemaining { get; set; } = 20;
        public bool StrictMobRemaining { get; set; }
        public int ExplorationPercent { get; set; } = 85;
        public bool StrictExplorationPercent { get; set; }
        public bool TrackMob { get; set; } = true;
        public bool FastTransition { get; set; } = true;
        public bool RunUnId { get; set; }

        public bool OnlyRunCorrupted { get; set; }
        public bool IgnoreHiddenAuras { get; set; }
        private bool atlasExplorationEnabled;
        public bool AtlasExplorationEnabled
        {
            get { return atlasExplorationEnabled; }
            set
            { atlasExplorationEnabled = value; NotifyPropertyChanged(() => atlasExplorationEnabled); breachRunner = false; NotifyPropertyChanged(() => breachRunner); citadelFinder = false; NotifyPropertyChanged(() => citadelFinder); simulacrumBot = false; NotifyPropertyChanged(() => simulacrumBot); }
        }
        private bool citadelFinder;
        public bool CitadelFinder
        {
            get { return citadelFinder; }
            set
            { citadelFinder = value; NotifyPropertyChanged(() => citadelFinder); breachRunner = false; NotifyPropertyChanged(() => breachRunner); simulacrumBot = false; NotifyPropertyChanged(() => simulacrumBot); atlasExplorationEnabled = false; NotifyPropertyChanged(() => atlasExplorationEnabled); }
        }
        private bool simulacrumBot;

        public bool SimulacrumBot
        {
            get { return simulacrumBot; }
            set
            { simulacrumBot = value; NotifyPropertyChanged(() => simulacrumBot); breachRunner = false; NotifyPropertyChanged(() => breachRunner); citadelFinder = false; NotifyPropertyChanged(() => citadelFinder); atlasExplorationEnabled = false; NotifyPropertyChanged(() => atlasExplorationEnabled); }
        }

        private bool breachRunner;

        public bool BreachRunner
        {
            get { return breachRunner; }
            set
            { breachRunner = value; NotifyPropertyChanged(() => breachRunner); simulacrumBot = false; NotifyPropertyChanged(() => simulacrumBot); citadelFinder = false; NotifyPropertyChanged(() => citadelFinder); atlasExplorationEnabled = false; NotifyPropertyChanged(() => atlasExplorationEnabled); enablebreachs = value; NotifyPropertyChanged(() => enablebreachs); }
        }

        private bool runBreachStone;

        public bool RunBreachStone
        {
            get { return runBreachStone; }
            set
            { runBreachStone = value; NotifyPropertyChanged(() => RunBreachStone); setupBreachTower = false; NotifyPropertyChanged(() => setupBreachTower); }
        }

        private bool setupBreachTower;

        public bool SetupBreachTower
        {
            get { return setupBreachTower; }
            set
            { setupBreachTower = value; NotifyPropertyChanged(() => SetupBreachTower); runBreachStone = false; NotifyPropertyChanged(() => runBreachStone); }
        }

        private bool ritualRunner;

        public bool RitualRunner
        {
            get { return ritualRunner; }
            set
            { ritualRunner = value; NotifyPropertyChanged(() => RitualRunner); }
        }

        private bool enablebreachs;
        [DefaultValue(true)]
        public bool Enablebreachs
        {
            get { return enablebreachs; }
            set
            { enablebreachs = value; NotifyPropertyChanged(() => Enablebreachs); }
        }

        private bool enableRitual;
        public bool EnableRitual
        {
            get { return enableRitual; }
            set
            { enableRitual = value; NotifyPropertyChanged(() => EnableRitual); }
        }

        public Upgrade MagicUpgrade { get; set; } = new Upgrade { TierEnabled = true };
        public Upgrade RareUpgrade { get; set; } = new Upgrade();
        public Upgrade ChiselUpgrade { get; set; } = new Upgrade();
        public Upgrade VaalUpgrade { get; set; } = new Upgrade();
        public Upgrade MagicRareUpgrade { get; set; } = new Upgrade();
        public RareReroll RerollMethod { get; set; }
        public ExistingRares ExistingRares { get; set; }
        public bool OpenPortals { get; set; } = true;

        private bool _stopRequested;
        private int _bossSlot;

        [DefaultValue(-1)]
        public int BossSlot
        {
            get { return _bossSlot; }
            set
            {
                if (value.Equals(_bossSlot))
                {
                    return;
                }
                _bossSlot = value;
                NotifyPropertyChanged(() => BossSlot);
            }
        }

        [JsonIgnore]
        public bool StopRequested
        {
            get => _stopRequested;
            set
            {
                if (value == _stopRequested) return;
                _stopRequested = value;
                NotifyPropertyChanged(() => StopRequested);
            }
        }

        private int _settingNumber;

		[DefaultValue(256)]
		public int SettingNumber
		{
			get => _settingNumber;
			set
			{
				_settingNumber = value;
				NotifyPropertyChanged(() => SettingNumber);
			}
		}

        private int _maxCombatDistance;
        private int _maxLootDistance;
        private ObservableCollection<FlasksClass> _flasks;
        public int MaxCombatDistance
        {
            get { return _maxCombatDistance; }
            set
            { _maxCombatDistance = value; NotifyPropertyChanged(() => MaxCombatDistance); }
        }
        [DefaultValue(800)]
        public int MaxLootDistance
        {
            get { return _maxLootDistance; }
            set
            { _maxLootDistance = value; NotifyPropertyChanged(() => MaxLootDistance); }
        }

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
        #endregion

        #region MapSettings

        public List<MapData> MapList { get; } = new List<MapData>();
        public Dictionary<string, MapData> MapDict { get; } = new Dictionary<string, MapData>();

        public bool OpenOnlyThatMap { get; set; }
        public bool FinishMap { get; set; }
        public string OpenMapName { get; set; }

        private void InitList()
        {
            if (MapList.Count > 0) return;
            //MapList.Add(new MapData(MapNames.Lookout, 1, MapType.Bossroom));
        }

        private void InitDict()
        {
            if (MapList.Count == 0) return;
            if (MapDict.Count > 1) return;
            foreach (var data in MapList)
            {
                MapDict.Add(data.Name, data);
            }
        }

        private void Load()
        {
            foreach (var data in MapList)
            {
                data.Ignored = false;
                data.IgnoredBossroom = false;
                data.MobRemaining = 5;
                data.StrictMobRemaining = true;
                data.ExplorationPercent = 98;
                data.StrictExplorationPercent = true;
                data.TrackMob = true;
                data.FastTransition = true;
            }
        }

        #endregion

        #region Flasks
        public ObservableCollection<FlasksClass> Flasks
        {
            get => _flasks;//?? (_flasks = new ObservableCollection<FlasksClass>());
            set
            {
                _flasks = value;
                NotifyPropertyChanged(() => Flasks);
            }
        }
        
        private ObservableCollection<FlasksClass> SetupDefaultFlasks()
        {
            ObservableCollection<FlasksClass> flasks = new ObservableCollection<FlasksClass>();

            flasks.Add(new FlasksClass(true, 1, false, false, 50, 1000, false));
            flasks.Add(new FlasksClass(true, 2, false, true, 30, 1000, false));
            return flasks;
        }
        #endregion

        #region Stashing

        public List<StashingRule> GeneralStashingRules { get; private set; } = new List<StashingRule>();
        public List<TogglableStashingRule> CurrencyStashingRules { get; private set; } = new List<TogglableStashingRule>();

        public readonly List<FullTabInfo> FullTabs = new List<FullTabInfo>();

        public List<string> GetTabsForCategory(string categoryName)
        {
            var rule = GeneralStashingRules.Find(r => r.Name == categoryName);
            if (rule == null)
            {
                GlobalLog.Error($"[EXtensions] Stashing rule requested for unknown name: \"{categoryName}\".");
                return GeneralStashingRules.Find(r => r.Name == StashingCategory.Other).TabList;
            }
            return rule.TabList;
        }

        public List<string> GetTabsForCurrency(string currencyName)
        {
            if (CurrencyNames.ShardToCurrencyDict.TryGetValue(currencyName, out var notShard))
                currencyName = notShard;

            return GetIndividualOrDefault(currencyName);
        }

        private List<string> GetIndividualOrDefault(string currencyName)
        {
            var rule = CurrencyStashingRules.Find(r => r.Enabled && r.Name == currencyName);
            if (rule != null) return rule.TabList;
            return GeneralStashingRules.Find(r => r.Name == StashingCategory.Currency).TabList;
        }

        public bool IsTabFull(string tabName, string itemMetadata)
        {
            foreach (var tab in FullTabs)
            {
                if (tab.Name != tabName)
                    continue;

                var metadata = tab.ControlsMetadata;
                return metadata.Count == 0 || metadata.Contains(itemMetadata);
            }
            return false;
        }

        public void MarkTabAsFull(string tabName, string itemMetadata)
        {
            var tab = FullTabs.Find(t => t.Name == tabName);
            if (tab == null)
            {
                FullTabs.Add(new FullTabInfo(tabName, itemMetadata));
                GlobalLog.Debug($"[MarkTabAsFull] New tab added. Name: \"{tabName}\". Metadata: \"{itemMetadata ?? "null"}\".");
            }
            else if (itemMetadata != null)
            {
                tab.ControlsMetadata.Add(itemMetadata);
                GlobalLog.Debug($"[MarkTabAsFull] Existing tab updated. Name: \"{tabName}\". Metadata: \"{itemMetadata}\".");
            }
            else
            {
                GlobalLog.Debug($"[MarkTabAsFull] \"{tabName}\" is already marked as full.");
            }
        }

        private static List<StashingRule> GetDefaultGeneralStashingRules()
        {
            return new List<StashingRule>
            {
                new StashingRule(StashingCategory.Currency, "1"),
                new StashingRule(StashingCategory.Rare, "2"),
                new StashingRule(StashingCategory.Unique, "2"),
                new StashingRule(StashingCategory.Gem, "3"),
                new StashingRule(StashingCategory.Essence, "3"),
                new StashingRule(StashingCategory.Map, "4"),
                new StashingRule(StashingCategory.Fragment, "4"),
                new StashingRule(StashingCategory.Other, "4"),
                new StashingRule(StashingCategory.Tablets, "5")
            };
        }

        private static List<TogglableStashingRule> GetDefaultCurrencyStashingRules()
        {
            return new List<TogglableStashingRule>
            {
                new TogglableStashingRule(CurrencyGroup.Breach, "1"),
                new TogglableStashingRule(CurrencyNames.Wisdom, "1"),
                new TogglableStashingRule(CurrencyNames.Portal, "1"),
                new TogglableStashingRule(CurrencyNames.Transmutation, "1"),
                new TogglableStashingRule(CurrencyNames.Augmentation, "1"),
                new TogglableStashingRule(CurrencyNames.Alteration, "1"),
                new TogglableStashingRule(CurrencyNames.Scrap, "1"),
                new TogglableStashingRule(CurrencyNames.Whetstone, "1"),
                new TogglableStashingRule(CurrencyNames.Glassblower, "1"),
                new TogglableStashingRule(CurrencyNames.Chisel, "1"),
                new TogglableStashingRule(CurrencyNames.Chance, "1"),
                new TogglableStashingRule(CurrencyNames.Alchemy, "1"),
                new TogglableStashingRule(CurrencyNames.Scouring, "1"),
                new TogglableStashingRule(CurrencyNames.Regal, "1"),
                new TogglableStashingRule(CurrencyNames.Chaos, "1"),
                new TogglableStashingRule(CurrencyNames.Vaal, "1"),
                new TogglableStashingRule(CurrencyNames.Gemcutter, "1"),
                new TogglableStashingRule(CurrencyNames.Divine, "1"),
                new TogglableStashingRule(CurrencyNames.Exalted, "1"),
                new TogglableStashingRule(CurrencyNames.Mirror, "1"),
                new TogglableStashingRule(CurrencyNames.Annulment, "1")
            };
        }

        private void InitGeneralStashingRules()
        {
            if (GeneralStashingRules.Count == 0)
            {
                GeneralStashingRules = GetDefaultGeneralStashingRules();
            }
            else
            {
                var defaultRules = GetDefaultGeneralStashingRules();
                foreach (var defaultRule in defaultRules)
                {
                    var jsonRule = GeneralStashingRules.Find(c => c.Name == defaultRule.Name);
                    if (jsonRule != null) defaultRule.CopyContents(jsonRule);
                }
                GeneralStashingRules = defaultRules;
            }
            foreach (var rule in GeneralStashingRules)
            {
                rule.FillTabList();
            }
        }

        private void InitCurrencyStashingRules()
        {
            if (CurrencyStashingRules.Count == 0)
            {
                CurrencyStashingRules = GetDefaultCurrencyStashingRules();
            }
            else
            {
                var defaultRules = GetDefaultCurrencyStashingRules();
                foreach (var defaultRule in defaultRules)
                {
                    var jsonRule = CurrencyStashingRules.Find(c => c.Name == defaultRule.Name);
                    if (jsonRule != null) defaultRule.CopyContents(jsonRule);
                }
                CurrencyStashingRules = defaultRules;
            }
            foreach (var rule in CurrencyStashingRules)
            {
                rule.FillTabList();
            }
        }

        public class StashingRule
        {
            public string Name { get; }

            // ReSharper disable once InconsistentNaming
            protected string _tabs;

            public string Tabs
            {
                get => _tabs;
                set
                {
                    if (value == _tabs) return;
                    _tabs = value;
                    FillTabList();
                    StashTask.RequestInvalidTabCheck();
                }
            }

            [JsonIgnore]
            public List<string> TabList { get; set; }

            public StashingRule(string name, string tabs)
            {
                Name = name;
                _tabs = tabs;
                TabList = new List<string>();
            }

            public void FillTabList()
            {
                try
                {
                    Parse(_tabs, TabList);
                }
                catch (Exception ex)
                {
                    if (BotManager.IsRunning)
                    {
                        GlobalLog.Error($"Parsing error in \"{_tabs}\".");
                        GlobalLog.Error(ex.Message);
                        BotManager.Stop();
                    }
                    else
                    {
                        MessageBoxes.Error($"Parsing error in \"{_tabs}\".\n{ex.Message}");
                    }
                }
            }

            private static void Parse(string str, ICollection<string> list)
            {
                if (str == string.Empty)
                    throw new Exception("Stashing setting cannot be empty.");

                list.Clear();

                var commaParams = str.Split(',');
                foreach (var param in commaParams)
                {
                    var trimmed = param.Trim();
                    if (trimmed == string.Empty)
                        throw new Exception("Remove double commas and/or commas from the start/end of the string.");

                    if (!ParseRange(trimmed, list))
                    {
                        list.Add(trimmed);
                    }
                }
            }

            private static bool ParseRange(string str, ICollection<string> list)
            {
                var hyphenParams = str.Split('-');
                if (hyphenParams.Length == 2)
                {
                    var start = hyphenParams[0].Trim();
                    var end = hyphenParams[1].Trim();

                    if (!int.TryParse(start, out var first))
                        throw new Exception($"Invalid parameter \"{start}\". Only numeric values are supported with range delimiter.");

                    if (!int.TryParse(end, out var last))
                        throw new Exception($"Invalid parameter \"{end}\". Only numeric values are supported with range delimiter.");

                    list.Add(start);

                    for (int i = first + 1; i < last; ++i)
                    {
                        list.Add(i.ToString());
                    }
                    list.Add(end);
                    return true;
                }
                if (hyphenParams.Length == 1) return false;
                throw new Exception($"Invalid range string: \"{str}\". Supported format: \"X-Y\".");
            }

            internal void CopyContents(StashingRule other)
            {
                _tabs = other._tabs;
            }
        }

        public class TogglableStashingRule : StashingRule
        {
            public bool Enabled { get; set; }

            public TogglableStashingRule(string name, string tabs, bool enabled = false)
                : base(name, tabs)
            {
                Enabled = enabled;
            }

            internal void CopyContents(TogglableStashingRule other)
            {
                _tabs = other._tabs;
                Enabled = other.Enabled;
            }
        }

        public class FullTabInfo
        {
            public readonly string Name;
            public readonly List<string> ControlsMetadata = new List<string>();

            public FullTabInfo(string name, string metadata)
            {
                Name = name;
                if (metadata != null)
                    ControlsMetadata.Add(metadata);
            }
        }

        #endregion

        #region Chests
        public int ChestOpenRange { get; set; } = 170;
        public int StrongboxOpenRange { get; set; } = 170;
        public int ShrineOpenRange { get; set; } = 170;
        public Rarity MaxStrongboxRarity { get; set; } = Rarity.Unique;

        private readonly List<ChestEntry> _chests = new List<ChestEntry>();
        private readonly List<ChestEntry> _strongboxes = new List<ChestEntry>();
        private readonly List<ChestEntry> _shrines = new List<ChestEntry>();

        public List<ChestEntry> Chests => _chests;
        public List<ChestEntry> Strongboxes => _strongboxes;
        public List<ChestEntry> Shrines => _shrines;

        private static List<ChestEntry> GetDefaultChestList()
        {
            // !OpensOnDamage only
            return new List<ChestEntry>
            {
                new ChestEntry("Chest"),
                new ChestEntry("Bone Chest"),
                new ChestEntry("Golden Chest"),
                new ChestEntry("Tribal Chest"),
                new ChestEntry("Sarcophagus"),
                new ChestEntry("Boulder"),
                new ChestEntry("Trunk"),
                new ChestEntry("Cocoon"),
                new ChestEntry("Corpse"),
                new ChestEntry("Bound Corpse"),
                new ChestEntry("Crucified Corpse"),
                new ChestEntry("Impaled Corpse"),
                new ChestEntry("Armour Rack"),
                new ChestEntry("Weapon Rack"),
                new ChestEntry("Scribe's Rack")
            };
        }

        private static List<ChestEntry> GetDefaultStrongboxList()
        {
            return new List<ChestEntry>
            {
                new ChestEntry("Arcanist's Strongbox"),
                new ChestEntry("Armourer's Strongbox"),
                new ChestEntry("Artisan's Strongbox"),
                new ChestEntry("Blacksmith's Strongbox"),
                new ChestEntry("Cartographer's Strongbox"),
                new ChestEntry("Diviner's Strongbox"),
                new ChestEntry("Gemcutter's Strongbox"),
                new ChestEntry("Jeweller's Strongbox"),
                new ChestEntry("Large Strongbox"),
                new ChestEntry("Ornate Strongbox"),
                new ChestEntry("Strongbox")
            };
        }

        private static List<ChestEntry> GetDefaultShrineList()
        {
            return new List<ChestEntry>
            {
                new ChestEntry("Acceleration Shrine"),
                new ChestEntry("Brutal Shrine"),
                new ChestEntry("Diamond Shrine"),
                new ChestEntry("Divine Shrine", false),
                new ChestEntry("Echoing Shrine"),
                new ChestEntry("Freezing Shrine"),
                new ChestEntry("Impenetrable Shrine"),
                new ChestEntry("Lightning Shrine"),
                new ChestEntry("Massive Shrine"),
                new ChestEntry("Replenishing Shrine"),
                new ChestEntry("Resistance Shrine"),
                new ChestEntry("Shrouded Shrine"),
                new ChestEntry("Skeletal Shrine")
            };
        }

        private static void InitChestEntries(ref List<ChestEntry> jsonList, Func<List<ChestEntry>> getDefaulList)
        {
            if (jsonList.Count == 0)
            {
                jsonList = getDefaulList();
            }
            else
            {
                var defaultList = getDefaulList();
                foreach (var defaultEntry in defaultList)
                {
                    var jsonEntry = jsonList.Find(c => c.Name == defaultEntry.Name);
                    if (jsonEntry != null) defaultEntry.Enabled = jsonEntry.Enabled;
                }
                jsonList = defaultList;
            }
        }

        public class ChestEntry
        {
            public string Name { get; }
            public bool Enabled { get; set; }

            public ChestEntry(string name, bool enabled = true)
            {
                Name = name;
                Enabled = enabled;
            }
        }

#endregion

        #region Affixes
        public List<AffixData> AffixList { get; } = new List<AffixData>();
        public Dictionary<string, AffixData> AffixDict { get; } = new Dictionary<string, AffixData>();

        private void InitListAffixes()
        {
            var br = Environment.NewLine;
            if (AffixList.Count > 0) return;
            AffixList.Add(new AffixData("Abhorrent", "Area is inhabited by Abominations"));
            AffixList.Add(new AffixData("Anarchic", "Area is inhabited by 2 additional Rogue Exiles"));
            AffixList.Add(new AffixData("Antagonist's", "Rare Monsters each have a Nemesis Mod" + br + "X% more Rare Monsters"));
            AffixList.Add(new AffixData("Bipedal", "Area is inhabited by Humanoids"));
            AffixList.Add(new AffixData("Capricious", "Area is inhabited by Goatmen"));
            AffixList.Add(new AffixData("Ceremonial", "Area contains many Totems"));
            AffixList.Add(new AffixData("Chaining", "Monsters' skills Chain 2 additional times"));
            AffixList.Add(new AffixData("Conflagrating", "All Monster Damage from Hits always Ignites"));
            AffixList.Add(new AffixData("Demonic", "Area is inhabited by Demons"));
            AffixList.Add(new AffixData("Emanant", "Area is inhabited by ranged monsters"));
            AffixList.Add(new AffixData("Feasting", "Area is inhabited by Cultists of Kitava"));
            AffixList.Add(new AffixData("Feral", "Area is inhabited by Animals"));
            AffixList.Add(new AffixData("Haunting", "Area is inhabited by Ghosts"));
            AffixList.Add(new AffixData("Lunar", "Area is inhabited by Lunaris fanatics"));
            AffixList.Add(new AffixData("Multifarious", "Area has increased monster variety"));
            AffixList.Add(new AffixData("Otherworldly", "Slaying Enemies close together can attract monsters from Beyond"));
            AffixList.Add(new AffixData("Skeletal", "Area is inhabited by Skeletons"));
            AffixList.Add(new AffixData("Slithering", "Area is inhabited by Sea Witches and their Spawn"));
            AffixList.Add(new AffixData("Solar", "Area is inhabited by Solaris fanatics"));
            AffixList.Add(new AffixData("Twinned", "Area contains two Unique Bosses"));
            AffixList.Add(new AffixData("Undead", "Area is inhabited by Undead"));
            AffixList.Add(new AffixData("Unstoppable", "Monsters cannot be slowed below base speed" + br + "Monsters cannot be Taunted"));
            AffixList.Add(new AffixData("Armoured", "+X% Monster Physical Damage Reduction"));
            AffixList.Add(new AffixData("Burning", "Monsters deal X% extra Damage as Fire"));
            AffixList.Add(new AffixData("Fecund", "X% more Monster Life"));
            AffixList.Add(new AffixData("Fleet", "X% increased Monster Movement Speed" + br + "X% increased Monster Attack Speed" + br + "X% increased Monster Cast Speed"));
            AffixList.Add(new AffixData("Freezing", "Monsters deal X% extra Damage as Cold"));
            AffixList.Add(new AffixData("Hexwarded", "X% less effect of Curses on Monsters"));
            AffixList.Add(new AffixData("Hexproof", "Monsters are Hexproof"));
            AffixList.Add(new AffixData("Impervious", "Monsters have a X% chance to avoid Poison, Blind, and Bleed"));
            AffixList.Add(new AffixData("Mirrored", "Monsters reflect X% of Elemental Damage"));
            AffixList.Add(new AffixData("Overlord's", "Unique Boss deals X% increased Damage" + br + "Unique Boss has X% increased Attack and Cast Speed"));
            AffixList.Add(new AffixData("Punishing", "Monsters reflect X% of Physical Damage"));
            AffixList.Add(new AffixData("Resistant", "+X% Monster Chaos Resistance" + br + "+X% Monster Elemental Resistance"));
            AffixList.Add(new AffixData("Savage", "X% increased Monster Damage"));
            AffixList.Add(new AffixData("Shocking", "Monsters deal X% extra Damage as Lightning"));
            AffixList.Add(new AffixData("Splitting", "Monsters fire 2 additional Projectiles"));
            AffixList.Add(new AffixData("Titan's", "Unique Boss has X% increased Life" + br + "Unique Boss has X% increased Area of Effect"));
            AffixList.Add(new AffixData("Unwavering", "Monsters cannot be Stunned" + br + "X% more Monster Life"));
            AffixList.Add(new AffixData("Empowered", "Monsters have a X% chance to cause Elemental Ailments on Hit"));
            AffixList.Add(new AffixData("of Balance", "Players have Elemental Equilibrium"));
            AffixList.Add(new AffixData("of Bloodlines", "Magic Monster Packs each have a Bloodline Mod" + br + "X% more Magic Monsters"));
            AffixList.Add(new AffixData("of Endurance", "Monsters gain an Endurance Charge on Hit"));
            AffixList.Add(new AffixData("of Frenzy", "Monsters gain a Frenzy Charge on Hit"));
            AffixList.Add(new AffixData("of Power", "Monsters gain a Power Charge on Hit"));
            AffixList.Add(new AffixData("of Skirmishing", "Players have Point Blank"));
            AffixList.Add(new AffixData("of Venom", "Monsters Poison on Hit"));
            AffixList.Add(new AffixData("of Deadliness", "Monsters have X% increased Critical Strike Chance" + br + "+X% to Monster Critical Strike Multiplier"));
            AffixList.Add(new AffixData("of Desecration", "Area has patches of desecrated ground"));
            AffixList.Add(new AffixData("of Drought", "Players gain X% reduced Flask Charges"));
            AffixList.Add(new AffixData("of Flames", "Area has patches of burning ground"));
            AffixList.Add(new AffixData("of Giants", "Monsters have X% increased Area of Effect"));
            AffixList.Add(new AffixData("of Ice", "Area has patches of chilled ground"));
            AffixList.Add(new AffixData("of Impotence", "Players have X% less Area of Effect"));
            AffixList.Add(new AffixData("of Insulation", "Monsters have X% chance to Avoid Elemental Status Ailments"));
            AffixList.Add(new AffixData("of Lightning", "Area has patches of shocking ground"));
            AffixList.Add(new AffixData("of Miring", "Player Dodge chance is Unlucky" + br + "Monsters have X% increased Accuracy Rating"));
            AffixList.Add(new AffixData("of Rust", "Players have X% reduced Block Chance" + br + "Players have X% less Armour"));
            AffixList.Add(new AffixData("of Smothering", "Players have X% less Recovery Rate of Life and Energy Shield"));
            AffixList.Add(new AffixData("of Toughness", "Monsters take X% reduced Extra Damage from Critical Strikes"));
            AffixList.Add(new AffixData("of Elemental Weakness", "Players are Cursed with Elemental Weakness"));
            AffixList.Add(new AffixData("of Enfeeblement", "Players are Cursed with Enfeeble"));
            AffixList.Add(new AffixData("of Exposure", "-X% maximum Player Resistances"));
            AffixList.Add(new AffixData("of Stasis", "Players cannot Regenerate Life, Mana or Energy Shield"));
            AffixList.Add(new AffixData("of Temporal Chains", "Players are Cursed with Temporal Chains"));
            AffixList.Add(new AffixData("of Vulnerability", "Players are Cursed with Vulnerability"));
            AffixList.Add(new AffixData("of Congealment", "Cannot Leech Life from Monsters" + br + "Cannot Leech Mana from Monsters"));
        }

        private void InitDictAffix()
        {
            if (AffixDict.Count > 1) return;
            foreach (var data in AffixList)
            {
                AffixDict.Add(data.Name, data);
            }
        }

        private void LoadAffixes()
        {
            if (!File.Exists(SettingsPath))
                return;

            var json = File.ReadAllText(SettingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                GlobalLog.Error("[MapBot] Fail to load \"AffixSettings.json\". File is empty.");
                return;
            }
            var parts = JsonConvert.DeserializeObject<Dictionary<string, EditablePart>>(json);
            if (parts == null)
            {
                GlobalLog.Error("[MapBot] Fail to load \"AffixSettings.json\". Json deserealizer returned null.");
                return;
            }
            foreach (var data in AffixList)
            {
                if (parts.TryGetValue(data.Name, out EditablePart part))
                {
                    data.RerollMagic = part.RerollMagic;
                    data.RerollRare = part.RerollRare;
                }
            }
        }

        private void SaveAffixes()
        {
            var parts = new Dictionary<string, EditablePart>(AffixList.Count);

            foreach (var data in AffixList)
            {
                var part = new EditablePart
                {
                    RerollMagic = data.RerollMagic,
                    RerollRare = data.RerollRare
                };
                parts.Add(data.Name, part);
            }
            var json = JsonConvert.SerializeObject(parts, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }

        private class EditablePart
        {
            public bool RerollMagic;
            public bool RerollRare;
        }
#endregion

        #region Stuck detection

        public bool StuckDetectionEnabled { get; set; } = true;
        public int MaxStucksPerInstance { get; set; } = 3;
        public int MaxStuckCountSmall { get; set; } = 9;
        public int MaxStuckCountMedium { get; set; } = 12;
        public int MaxStuckCountLong { get; set; } = 16;

        #endregion

        #region Misc
        [DefaultValue(true)]
        public bool LootVisibleItems { get; set; }
        public bool UseChatForHideout { get; set; }
        public int MinInventorySquares { get; set; } = 0;

        public bool ArtificialDelays { get; set; } = true;
        public int MinArtificialDelay { get; set; } = 200;
        public int MaxArtificialDelay { get; set; } = 300;
        public bool AutoDnd { get; set; }
        public string DndMessage { get; set; }
        public ObservableCollection<InventoryCurrency> InventoryCurrencies { get; set; }

        public class InventoryCurrency
        {
            public const string DefaultName = "CurrencyName";

            private string _name;
            private int _row;
            private int _column;

            public string Name
            {
                get => _name;
                set => _name = string.IsNullOrWhiteSpace(value) ? DefaultName : value.Trim();
            }

            public int Row
            {
                get => _row;
                set
                {
                    if (value == 0)
                    {
                        _row = _row == 1 ? -1 : 1;
                    }
                    else
                    {
                        _row = value;
                    }
                }
            }

            public int Column
            {
                get => _column;
                set
                {
                    if (value == 0)
                    {
                        _column = _column == 1 ? -1 : 1;
                    }
                    else
                    {
                        _column = value;
                    }
                }
            }

            public int Restock { get; set; }

            public InventoryCurrency()
            {
                Name = DefaultName;
                Row = -1;
                Column = -1;
                Restock = -1;
            }

            public InventoryCurrency(string name, int row, int column, int restock = -1)
            {
                Name = name;
                Row = row;
                Column = column;
                Restock = restock;
            }
        }

        #endregion

        #region Overlay

        private bool _enableOverlay;
        private bool _drawInBackground;
        private bool _drawMobs;
        private bool _drawCorpses;
        private int _fps;
        private int _overlayXCoord;
        private int _overlayYCoord;
        private int _overlayTransparency;

        [DefaultValue(false)]
        public bool EnableOverlay
        {
            get => _enableOverlay;
            set
            {
                if (value == _enableOverlay) return;
                _enableOverlay = value;
                NotifyPropertyChanged(() => EnableOverlay);
            }
        }
        [DefaultValue(false)]
        public bool DrawInBackground
        {
            get => _drawInBackground;
            set
            {
                if (value == _drawInBackground) return;
                _drawInBackground = value;
                NotifyPropertyChanged(() => DrawInBackground);
            }
        }
        [DefaultValue(false)]
        public bool DrawMobs
        {
            get => _drawMobs;
            set
            {
                if (value == _drawMobs) return;
                _drawMobs = value;
                NotifyPropertyChanged(() => DrawMobs);
            }
        }
        [DefaultValue(false)]
        public bool DrawCorpses
        {
            get => _drawCorpses;
            set
            {
                if (value == _drawCorpses) return;
                _drawCorpses = value;
                NotifyPropertyChanged(() => DrawCorpses);
            }
        }

        [DefaultValue(30)]
        public int FPS
        {
            get => _fps;
            set
            {
                if (value == _fps) return;
                _fps = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetFps(_fps);
                NotifyPropertyChanged(() => FPS);
            }
        }

        [DefaultValue(15)]
        public int OverlayXCoord
        {
            get => _overlayXCoord;
            set
            {
                if (value == _overlayXCoord) return;
                _overlayXCoord = value;
                NotifyPropertyChanged(() => OverlayXCoord);
            }
        }

        [DefaultValue(70)]
        public int OverlayYCoord
        {
            get => _overlayYCoord;
            set
            {
                if (value == _overlayYCoord) return;
                _overlayYCoord = value;
                NotifyPropertyChanged(() => OverlayYCoord);
            }
        }

        [DefaultValue(70)]
        public int OverlayTransparency
        {
            get => _overlayTransparency;
            set
            {
                if (value == _overlayTransparency) return;
                _overlayTransparency = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetTransparency(_overlayTransparency);
                NotifyPropertyChanged(() => OverlayTransparency);
            }
        }

        #endregion

    }
    public class Upgrade
    {
        public bool TierEnabled { get; set; }
        public int Tier { get; set; } = 1;
        public bool PriorityEnabled { get; set; }
        public int Priority { get; set; }
    }

    public enum RareReroll
    {
        ScourAlch,
        Chaos
    }

    public enum ExistingRares
    {
        Run,
        NoRun,
        NoReroll,
        Downgrade
    }

    public static class StashingCategory
    {
        public const string Currency = "Currency";
        public const string Rare = "Rares";
        public const string Unique = "Uniques";
        public const string Gem = "Gems";
        public const string Card = "Cards";
        public const string Prophecy = "Prophecies";
        public const string Essence = "Essences";
        public const string Jewel = "Jewels";
        public const string Map = "Maps";
        public const string Fragment = "Fragments";
        public const string Leaguestone = "Leaguestones";
        public const string Tablets = "Tablets";
        public const string Breachstone = "Breachstone";
        public const string Other = "Other";
    }

    public static class CurrencyGroup
    {
        public const string Sextant = "Sextants";
        public const string Breach = "Breach";
    }
}