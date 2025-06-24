using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP2.EXtensions;
using MP2.EXtensions.CachedObjects;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using InventoryUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.InventoryUi;
using StashUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.StashUi;
using ExSettings = MP2.Mp2Settings;
using Settings = MP2.Mp2Settings;
using MP2.EXtensions.Mapper;
using DreamPoeBot.Common;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    public class TakeMapTask : ITask
    {
        private static readonly Dictionary<string, bool> AvailableCurrency = new Dictionary<string, bool>
        {
            [CurrencyNames.Transmutation] = true,
            [CurrencyNames.Augmentation] = true,
            [CurrencyNames.Alteration] = true,
            [CurrencyNames.Alchemy] = true,
            [CurrencyNames.Chaos] = true,
            [CurrencyNames.Scouring] = true,
            [CurrencyNames.Chisel] = true,
            [CurrencyNames.Vaal] = true
        };

        private static readonly Dictionary<string, int> AmountForAvailable = new Dictionary<string, int>
        {
            [CurrencyNames.Transmutation] = 1,
            [CurrencyNames.Augmentation] = 5,
            [CurrencyNames.Alteration] = 5,
            [CurrencyNames.Alchemy] = 5,
            [CurrencyNames.Chaos] = 5,
            [CurrencyNames.Scouring] = 5,
            [CurrencyNames.Chisel] = 4,
            [CurrencyNames.Vaal] = 1
        };

        private static bool _hasFragments = true;

        public async Task<bool> Run()
        {
            if (MP2.IsOnRun || DeviceAreaTask._toMap)
                return false;

            var area = World.CurrentArea;

            if (!area.IsTown && !area.IsHideoutArea)
                return false;

            GlobalLog.Debug("TakeMap");
            await Coroutines.CloseBlockingWindows();

            if (Settings.Instance.StopRequested)
            {
                GlobalLog.Warn("Stopping the bot by a user's request (stop after current map)");
                Settings.Instance.StopRequested = false;
                BotManager.Stop();
                return true;
            }
            Item map = null;

            var mapTabs = ExSettings.Instance.GetTabsForCategory(StashingCategory.Map);

            foreach (var tab in mapTabs)
            {
                if (!await Inventories.OpenStashTab(tab))
                {
                    ErrorManager.ReportError();
                    return true;
                }
                if ((map = FindProperMapInInventory()) != null)
                {
                    goto hasProperMapInv;
                }

                if (StashUi.StashTabInfo.IsPremiumMap && !StashUi.StashTabInfo.IsPremiumQuad)
                {
                    GlobalLog.Error("Map stash tab is unsupported and there are no plans to support it in the future. Please remove it from stashing settings.");
                    BotManager.Stop();
                    return true;
                }

                if ((map = FindProperMap()) != null)
                    goto hasProperMap;

                GlobalLog.Debug($"[TakeMapTask] Fail to find a proper map in \"{tab}\" tab.");
            }

            GlobalLog.Error("[TakeMapTask] Fail to find a proper map in all map tabs. Now stopping the bot because it cannot continue.");
            BotManager.Stop();
            return true;

            hasProperMap:
            GlobalLog.Info($"[TakeMapTask] Map of choice is \"{map.Name}\" (Tier: {map.MapTier})");

            if (!await Inventories.FastMoveFromStashTab(map.LocationTopLeft))
            {
                ErrorManager.ReportError();
                return true;
            }
            if(Mp2Settings.Instance.SimulacrumBot)
            {
                if (!await Wait.For(() => (map = Inventories.InventoryItems.Find(i => i.Name == "Simulacrum")) != null, "map appear in inventory"))
                {
                    GlobalLog.Error("[TakeMapTask] Unexpected error. Map did not appear in player's inventory after fast move from stash.");
                    return true;
                }
            }
            else if (Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
            {
                if (!await Wait.For(() => (map = Inventories.InventoryItems.Find(i => i.Metadata == "Metadata/Items/MapFragments/CurrencyBreachFragment")) != null, "map appear in inventory"))
                {
                    GlobalLog.Error("[TakeMapTask] Unexpected error. Map did not appear in player's inventory after fast move from stash.");
                    return true;
                }
            }
            else
            {
                if (!await Wait.For(() => (map = Inventories.InventoryItems.Find(i => i.IsMap())) != null, "map appear in inventory"))
                {
                    GlobalLog.Error("[TakeMapTask] Unexpected error. Map did not appear in player's inventory after fast move from stash.");
                    return true;
                }
            }
            
            hasProperMapInv:
            var mapPos = map.LocationTopLeft;
            var mapRarity = map.RarityLite();

            if (mapRarity == Rarity.Unique || !map.IsIdentified || map.IsMirrored || map.IsCorrupted)
            {
                ChooseMap(mapPos);
                return false;
            }

            switch (mapRarity)
            {
                case Rarity.Normal:
                    if (!await HandleNormalMap(mapPos)) return true;
                    break;

                case Rarity.Magic:
                    if (!await HandleMagicMap(mapPos)) return true;
                    break;

                case Rarity.Rare:
                    if (!await HandleRareMap(mapPos)) return true;
                    break;

                default:
                    GlobalLog.Error($"[TakeMapTask] Unknown map rarity: \"{mapRarity}\".");
                    ErrorManager.ReportCriticalError();
                    return true;
            }

            UpdateMapReference(mapPos, ref map);

            if (map.ShouldUpgrade(Settings.Instance.VaalUpgrade) && HasCurrency(CurrencyNames.Vaal))
            {
                if (!await CorruptMap(mapPos))
                    return true;

                UpdateMapReference(mapPos, ref map);
            }
            ChooseMap(mapPos);

            if(Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.SetupBreachTower)
            {
                await Coroutines.CloseBlockingWindows();
                var tabletTabs = Mp2Settings.Instance.GetTabsForCategory(StashingCategory.Tablets);
                for (int x = 0; x < 2; x++)
                {
                    Item tablet = null;
                    foreach (var tab in tabletTabs)
                    {
                        if (!await Inventories.OpenStashTab(tab))
                        {
                            if (x == 0)
                            {
                                Mp2Settings.Instance.SetupBreachTower = false;
                            }
                            ErrorManager.ReportError();
                            return false;
                        }

                        if ((tablet = FindProperTablet(true)) != null)
                            break;
                    }
                    if (tablet == null)
                    {
                        GlobalLog.Error($"[HandleFinishedTowersTask] Fail to find a proper tablet in \"{tabletTabs}\" tabs.");
                        if(x == 0)
                        {
                            Mp2Settings.Instance.SetupBreachTower = false;
                        }
                        return false;
                    }
                    if (!await Inventories.FastMoveFromStashTab(tablet.LocationTopLeft))
                    {
                        if (x == 0)
                        {
                            Mp2Settings.Instance.SetupBreachTower = false;
                        }
                        GlobalLog.Error($"[HandleFinishedTowersTask] Fail to move tablet from stash.");
                        return false;
                    }
                }
            }
            return false;
        }

        private static Item FindProperTablet(bool inStash = true)
        {
            var inv = inStash ? Inventories.StashTabItems : Inventories.InventoryItems;

            foreach (var tablet in inv.Where(i => i.Metadata.StartsWith("Metadata/Items/TowerAugment/")))
            {
                if (tablet.Metadata == "Metadata/Items/TowerAugment/RitualAugment")
                {
                    return tablet;
                }
            }
            return null;
        }
        private static Item FindProperMapInInventory()
        {
            var maps = new List<Item>();

            if (Mp2Settings.Instance.SimulacrumBot)
            {
                Item s = Inventories.InventoryItems.Find(i => i.Name == "Simulacrum");
                if (s != null) return s;
                return null;
            }
            if (Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
            {
                Item s = Inventories.InventoryItems.Find(i => i.Metadata == "Metadata/Items/MapFragments/CurrencyBreachFragment");
                if (s != null) return s;
                return null;
            }

            foreach (var map in Inventories.InventoryItems.Where(i => i.IsMap()))
            {
                var rarity = map.RarityLite();

                if (rarity == Rarity.Unique)
                {
                    maps.Add(map);
                    continue;
                }

                if (Mp2Settings.Instance.OnlyRunCorrupted && map.IsCorrupted)
                {
                    maps.Add(map);
                    continue;
                }

                if (!map.BelowTierLimit())
                    continue;

                if (rarity == Rarity.Rare && Settings.Instance.ExistingRares == ExistingRares.NoRun && NoRareUpgrade(map))
                    continue;

                if (!Settings.Instance.RunUnId && !map.IsIdentified)
                    continue;

                if (map.HasBannedAffix())
                {
                    if (map.IsCorrupted || map.IsMirrored)
                        continue;

                    if (rarity == Rarity.Magic && !HasMagicOrbs)
                        continue;

                    if (rarity == Rarity.Rare)
                    {
                        if (NoRareUpgrade(map))
                        {
                            if (Settings.Instance.ExistingRares == ExistingRares.NoReroll)
                                continue;

                            if (Settings.Instance.ExistingRares == ExistingRares.Downgrade)
                            {
                                if (HasScourTransmute) maps.Add(map);
                                continue;
                            }
                        }

                        if (!HasRareOrbs)
                            continue;
                    }
                }
                if (Mp2Settings.Instance.OnlyRunCorrupted && !map.IsCorrupted)
                    continue;

                maps.Add(map);
            }

            if (maps.Count == 0)
                return null;

            var sortedMaps = maps
                .OrderByDescending(m => m.MapTier)
                .ThenByDescending(m => m.RarityLite())
                .ThenByDescending(m => m.Quality)
                .ToList();

            var unique = sortedMaps.Find(m => m.RarityLite() == Rarity.Unique);
            if (unique != null)
                return unique;

            if (Settings.Instance.RunUnId)
            {
                var unId = sortedMaps.Find(m => !m.IsIdentified);
                if (unId != null)
                    return unId;
            }
            return sortedMaps[0];
        }
        private static Item FindProperMap()
        {
            var maps = new List<Item>();

            if (Mp2Settings.Instance.SimulacrumBot)
            {
                Item s = Inventories.StashTabItems.Find(i => i.Name == "Simulacrum");
                if (s != null) return s;
                return null;
            }

            if (Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
            {
                Item s = Inventories.StashTabItems.Find(i => i.Metadata == "Metadata/Items/MapFragments/CurrencyBreachFragment");
                if (s != null) return s;
                return null;
            }

            foreach (var map in Inventories.StashTabItems.Where(i => i.IsMap()))
            {
                var rarity = map.RarityLite();    

                if (rarity == Rarity.Unique)
                {
                    maps.Add(map);
                    continue;
                }
                

                if (Mp2Settings.Instance.OnlyRunCorrupted && map.IsCorrupted)
                {
                    maps.Add(map);
                    continue;
                }

                if (!map.BelowTierLimit())
                    continue;

                if (rarity == Rarity.Rare && Settings.Instance.ExistingRares == ExistingRares.NoRun && NoRareUpgrade(map))
                    continue;

                if (!Settings.Instance.RunUnId && !map.IsIdentified)
                    continue;

                if (map.HasBannedAffix())
                {
                    if (map.IsCorrupted || map.IsMirrored)
                        continue;

                    if (rarity == Rarity.Magic && !HasMagicOrbs)
                        continue;

                    if (rarity == Rarity.Rare)
                    {
                        if (NoRareUpgrade(map))
                        {
                            if (Settings.Instance.ExistingRares == ExistingRares.NoReroll)
                                continue;

                            if (Settings.Instance.ExistingRares == ExistingRares.Downgrade)
                            {
                                if (HasScourTransmute) maps.Add(map);
                                continue;
                            }
                        }

                        if (!HasRareOrbs)
                            continue;
                    }
                }
                if (Mp2Settings.Instance.OnlyRunCorrupted && !map.IsCorrupted)
                    continue;

                maps.Add(map);
            }

            if (maps.Count == 0)
                return null;

            var sortedMaps = maps
                .OrderByDescending(m => m.MapTier)
                .ThenByDescending(m => m.RarityLite())
                .ThenByDescending(m => m.Quality)
                .ToList();

            var unique = sortedMaps.Find(m => m.RarityLite() == Rarity.Unique);
            if (unique != null)
                return unique;

            if (Settings.Instance.RunUnId)
            {
                var unId = sortedMaps.Find(m => !m.IsIdentified);
                if (unId != null)
                    return unId;
            }
            return sortedMaps[0];
        }

        private static async Task<bool> HandleNormalMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (map == null)
            {
                GlobalLog.Error($"[HandleNormalMap] Fail to find a map at {mapPos}.");
                return false;
            }
            if (map.ShouldUpgrade(Settings.Instance.RareUpgrade) && HasRareOrbs)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Alchemy))
                    return false;

                return await RerollRare(mapPos);
            }
            if (map.ShouldUpgrade(Settings.Instance.MagicUpgrade) && HasMagicOrbs)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Transmutation))
                    return false;

                return await RerollMagic(mapPos);
            }
            return true;
        }

        private static async Task<bool> HandleMagicMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (map == null)
            {
                GlobalLog.Error($"[HandleMagicMap] Fail to find map at {mapPos}.");
                return false;
            }
            if (map.ShouldUpgrade(Settings.Instance.MagicRareUpgrade) && HasMagicToRareOrbs)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Scouring))
                    return false;

                if (!await ApplyOrb(mapPos, CurrencyNames.Alchemy))
                    return false;

                return await RerollRare(mapPos);
            }
            return await RerollMagic(mapPos);
        }

        private static async Task<bool> HandleRareMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (map == null)
            {
                GlobalLog.Error($"[HandleRareMap] Fail to find map at {mapPos}.");
                return false;
            }
            if (Settings.Instance.ExistingRares == ExistingRares.Downgrade && map.HasBannedAffix() && NoRareUpgrade(map) && HasScourTransmute)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Scouring))
                    return false;

                if (!await ApplyOrb(mapPos, CurrencyNames.Transmutation))
                    return false;

                return await RerollMagic(mapPos);
            }
            return await RerollRare(mapPos);
        }

        public static async Task<bool> RerollMagic(Vector2i mapPos)
        {
            while (true)
            {
                var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
                if (map == null)
                {
                    GlobalLog.Error($"[RerollMagic] Fail to find a map at {mapPos}.");
                    return false;
                }
                var rarity = map.RarityLite();
                if (rarity != Rarity.Magic)
                {
                    GlobalLog.Error($"[TakeMapTask] RerollMagic is called on {rarity} map.");
                    return false;
                }
                var affix = map.GetBannedAffix();
                if (affix != null)
                {
                    GlobalLog.Info($"[RerollMagic] Rerolling banned \"{affix}\" affix.");

                    if (!await ApplyOrb(mapPos, CurrencyNames.Alteration))
                        return false;

                    continue;
                }
                if (map.CanAugment() && HasCurrency(CurrencyNames.Augmentation))
                {
                    if (!await ApplyOrb(mapPos, CurrencyNames.Augmentation))
                        return false;

                    continue;
                }
                return true;
            }
        }

        public static async Task<bool> RerollRare(Vector2i mapPos)
        {
            while (true)
            {
                var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
                if (map == null)
                {
                    GlobalLog.Error($"[RerollRare] Fail to find a map at {mapPos}.");
                    return false;
                }
                var rarity = map.RarityLite();
                if (rarity != Rarity.Rare)
                {
                    GlobalLog.Error($"[TakeMapTask] RerollRare is called on {rarity} map.");
                    return false;
                }

                var affix = map.GetBannedAffix();

                if (affix == null)
                    return true;

                GlobalLog.Info($"[RerollRare] Rerolling banned \"{affix}\" affix.");

                if (Settings.Instance.RerollMethod == RareReroll.Chaos)
                {
                    if (!await ApplyOrb(mapPos, CurrencyNames.Chaos))
                        return false;
                }
                else
                {
                    if (!await ApplyOrb(mapPos, CurrencyNames.Scouring))
                        return false;

                    if (!await ApplyOrb(mapPos, CurrencyNames.Alchemy))
                        return false;
                }
            }
        }

        public static async Task<bool> ApplyChisels(Vector2i mapPos)
        {
            while (true)
            {
                var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
                if (map == null)
                {
                    GlobalLog.Error($"[ApplyChisels] Fail to find a map at {mapPos}.");
                    return false;
                }

                if (map.Quality >= 18)
                    return true;

                if (!await ApplyOrb(mapPos, CurrencyNames.Chisel))
                    return false;
            }
        }

        private static async Task<bool> CorruptMap(Vector2i mapPos)
        {
            if (!await ApplyOrb(mapPos, CurrencyNames.Vaal))
                return false;

            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (!map.IsIdentified)
            {
                GlobalLog.Warn("[CorruptMap] Unidentified corrupted map retains it's original affixes. We are good to go.");
                return true;
            }
            if (!map.BelowTierLimit())
            {
                GlobalLog.Warn("[CorruptMap] Map tier has been increased beyond tier limit in settings.");
                return false;
            }
            var affix = map.GetBannedAffix();
            if (affix != null)
            {
                GlobalLog.Warn($"[CorruptMap] Banned \"{affix}\" has been spawned.");
                return false;
            }
            GlobalLog.Warn("[CorruptMap] Resulting corrupted map fits all requirements. We are good to go.");
            return true;
        }

        private static async Task<bool> ApplyOrb(Vector2i targetPos, string orbName)
        {
            if (!await Inventories.FindTabWithCurrency(orbName))
            {
                GlobalLog.Warn($"[TakeMapTask] There are no \"{orbName}\" in all tabs assigned to them. Now marking this currency as unavailable.");
                AvailableCurrency[orbName] = false;
                return false;
            }

            if (StashUi.StashTabInfo.IsPremiumCurrency)
            {
                var control = Inventories.GetControlWithCurrency(orbName);
                if (!await control.PickItemToCursor(true))
                {
                    ErrorManager.ReportError();
                    return false;
                }
            }
            else
            {
                var orb = Inventories.StashTabItems.Find(i => i.Name == orbName);
                if (!await StashUi.InventoryControl.PickItemToCursor(orb.LocationTopLeft, true))
                {
                    ErrorManager.ReportError();
                    return false;
                }
            }
            if (!await InventoryUi.InventoryControl_Main.PlaceItemFromCursor(targetPos))
            {
                ErrorManager.ReportError();
                return false;
            }
            return true;
        }

        private static void ChooseMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            OpenMapTask.Enabled = true;
            GlobalLog.Warn($"[TakeMapTask] Now going to \"{map.FullName}\".");
        }

        // ReSharper disable once RedundantAssignment
        private static void UpdateMapReference(Vector2i mapPos, ref Item map)
        {
            map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
        }

        private static bool NoRareUpgrade(Item map)
        {
            return !map.ShouldUpgrade(Settings.Instance.RareUpgrade) && !map.ShouldUpgrade(Settings.Instance.MagicRareUpgrade);
        }

        private static bool HasCurrency(string name)
        {
            if (AvailableCurrency[name])
                return true;

            GlobalLog.Debug($"[TakeMapTask] HasCurrency is false for {name}.");
            return false;
        }

        private static bool HasMagicOrbs
        {
            get
            {
                return HasCurrency(CurrencyNames.Alteration) &&
                       HasCurrency(CurrencyNames.Augmentation) &&
                       HasCurrency(CurrencyNames.Transmutation);
            }
        }

        private static bool HasRareOrbs
        {
            get
            {
                if (Settings.Instance.RerollMethod == RareReroll.ScourAlch)
                    return HasScourAlchemy;

                return HasCurrency(CurrencyNames.Alchemy) &&
                       HasCurrency(CurrencyNames.Chaos);
            }
        }

        private static bool HasMagicToRareOrbs
        {
            get
            {
                if (Settings.Instance.RerollMethod == RareReroll.ScourAlch)
                    return HasScourAlchemy;

                return HasScourAlchemy && HasCurrency(CurrencyNames.Chaos);
            }
        }

        private static bool HasScourAlchemy
        {
            get
            {
                return HasCurrency(CurrencyNames.Scouring) &&
                       HasCurrency(CurrencyNames.Alchemy);
            }
        }

        private static bool HasScourTransmute
        {
            get
            {
                return HasCurrency(CurrencyNames.Scouring) &&
                       HasCurrency(CurrencyNames.Transmutation);
            }
        }

        private static void UpdateAvailableCurrency(string currencyName)
        {
            if (!AvailableCurrency.TryGetValue(currencyName, out bool available))
                return;

            if (available)
                return;

            var amount = Inventories.GetCurrencyAmountInStashTab(currencyName);
            if (amount >= AmountForAvailable[currencyName])
            {
                GlobalLog.Info($"[TakeMapTask] There are {amount} \"{currencyName}\" in current stash tab. Now marking this currency as available.");
                AvailableCurrency[currencyName] = true;
            }
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.ItemStashedEvent)
            {
                var item = message.GetInput<CachedItem>();
                var itemType = item.Type.ItemType;
                if (itemType == ItemTypes.Currency)
                {
                    UpdateAvailableCurrency(item.Name);
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        // Reset every Start in case user added something manually
        public void Start()
        {
            foreach (var key in AvailableCurrency.Keys.ToList())
            {
                AvailableCurrency[key] = true;
            }
            _hasFragments = true;
        }

        #region Unused interface methods

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public void Tick()
        {
        }

        public void Stop()
        {
        }

        public string Name => "TakeMapTask";

        public string Author => "ExVault";

        public string Description => "Task for taking maps from the stash.";

        public string Version => "1.0";

        #endregion
    }
}