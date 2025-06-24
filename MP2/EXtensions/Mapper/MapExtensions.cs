using System.Collections.Generic;
using System.Linq;
using MP2.EXtensions;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;

namespace MP2.EXtensions.Mapper
{
    public static class MapExtensions
    {
        private static readonly Mp2Settings GeneralSettings = Mp2Settings.Instance;
        private static readonly Dictionary<string, MapData> MapDict = Mp2Settings.Instance.MapDict;
        private static readonly Dictionary<string, AffixData> AffixDict = Mp2Settings.Instance.AffixDict;

        public static bool IsMap(this Item item)
        {
            return item.Class == ItemClasses.Map;
        }

        public static string CleanName(this Item map)
        {
            return map.CleanName();
        }

        public static bool BelowTierLimit(this Item map)
        {
            return map.MapTier <= GeneralSettings.MaxMapTier;
        }

        public static int Priority(this Item map)
        {
            
            var cleanName = map.CleanName();

            if (!MapDict.TryGetValue(cleanName, out var data))
                return int.MinValue;

            if (GeneralSettings.AtlasExplorationEnabled &&
                !AtlasData.IsCompleted(cleanName))
                return int.MaxValue;

            var priority = data.Priority;

            return priority;
        }

        public static bool Ignored(this Item map)
        {
            return !MapDict.TryGetValue(map.CleanName(), out MapData data) || data.Ignored;
        }

        public static string GetBannedAffix(this Item map)
        {
            var rarity = map.RarityLite();

            if (rarity != Rarity.Magic && rarity != Rarity.Rare)
                return null;

            foreach (var affix in map.ExplicitAffixes)
            {
                string affixName = affix.DisplayName;

                if (affixName == "Twinned")
                    return affixName;

                if (AffixDict.TryGetValue(affixName, out var data))
                {
                    if (rarity == Rarity.Magic)
                    {
                        if (data.RerollMagic)
                            return affixName;
                    }
                    else
                    {
                        if (data.RerollRare)
                            return affixName;
                    }
                }
                else
                {
                    GlobalLog.Debug($"[GetBannedAffix] Unknown map affix \"{affixName}\".");
                }
            }
            return null;
        }

        public static bool HasBannedAffix(this Item map)
        {
            return map.GetBannedAffix() != null;
        }

        public static bool CanAugment(this Item map)
        {
            return map.ExplicitAffixes.Count() < 2;
        }

        public static bool ShouldUpgrade(this Item map, Upgrade upgrade)
        {
            var tier = map.MapTier;

            if (GeneralSettings.AtlasExplorationEnabled)
            {
                if (tier >= 6 && (upgrade == GeneralSettings.RareUpgrade || upgrade == GeneralSettings.MagicRareUpgrade))
                    return true;

                if (tier >= 11 && upgrade == GeneralSettings.VaalUpgrade)
                    return true;
            }

            if (upgrade.TierEnabled && tier >= upgrade.Tier)
                return true;

            return false;
        }

        public static bool IsSacrificeFragment(this Item item)
        {
            return item.Metadata.Contains("CurrencyVaalFragment1");
        }

        internal class AtlasData
        {
            private static readonly HashSet<string> BonusCompletedAreas = new HashSet<string>();

            internal static bool IsCompleted(string name)
            {
                return BonusCompletedAreas.Contains(name);
            }

            internal static void Update()
            {
                BonusCompletedAreas.Clear();

                foreach (var area in LokiPoe.InGameState.AtlasUi.CompletedAtlasNodes)
                {
                    BonusCompletedAreas.Add(area.ToString());
                }
            }
        }
    }
}