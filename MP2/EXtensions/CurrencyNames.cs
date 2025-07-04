﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game.GameData;

namespace MP2.EXtensions
{
    [SuppressMessage("ReSharper", "UnassignedReadonlyField")]
    public static class CurrencyNames
    {
        [ItemMetadata("Metadata/Items/Currency/CurrencyIdentification")]
        public static readonly string Wisdom;

        [ItemMetadata("Metadata/Items/Currency/CurrencyPortal")]
        public static readonly string Portal;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeToMagic")]
        public static readonly string Transmutation;

        [ItemMetadata("Metadata/Items/Currency/CurrencyAddModToMagic")]
        public static readonly string Augmentation;

        [ItemMetadata("Metadata/Items/Currency/CurrencyRerollMagic")]
        public static readonly string Alteration;

        [ItemMetadata("Metadata/Items/Currency/CurrencyArmourQuality")]
        public static readonly string Scrap;

        [ItemMetadata("Metadata/Items/Currency/CurrencyWeaponQuality")]
        public static readonly string Whetstone;

        [ItemMetadata("Metadata/Items/Currency/CurrencyFlaskQuality")]
        public static readonly string Glassblower;

        [ItemMetadata("Metadata/Items/Currency/CurrencyMapQuality")]
        public static readonly string Chisel;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeRandomly")]
        public static readonly string Chance;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeToRare")]
        public static readonly string Alchemy;

        [ItemMetadata("Metadata/Items/Currency/CurrencyConvertToNormal")]
        public static readonly string Scouring;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeMagicToRare")]
        public static readonly string Regal;

        [ItemMetadata("Metadata/Items/Currency/CurrencyRerollRare")]
        public static readonly string Chaos;

        [ItemMetadata("Metadata/Items/Currency/CurrencyCorrupt")]
        public static readonly string Vaal;

        [ItemMetadata("Metadata/Items/Currency/CurrencyGemQuality")]
        public static readonly string Gemcutter;

        [ItemMetadata("Metadata/Items/Currency/CurrencyModValues")]
        public static readonly string Divine;

        [ItemMetadata("Metadata/Items/Currency/CurrencyAddModToRare")]
        public static readonly string Exalted;

        [ItemMetadata("Metadata/Items/Currency/CurrencyDuplicate")]
        public static readonly string Mirror;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachFireShard")]
        public static readonly string SplinterXoph;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachColdShard")]
        public static readonly string SplinterTul;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachLightningShard")]
        public static readonly string SplinterEsh;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachPhysicalShard")]
        public static readonly string SplinterUulNetol;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachChaosShard")]
        public static readonly string SplinterChayula;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachUpgradeUniqueFire")]
        public static readonly string BlessingXoph;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachUpgradeUniqueCold")]
        public static readonly string BlessingTul;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachUpgradeUniqueLightning")]
        public static readonly string BlessingEsh;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachUpgradeUniquePhysical")]
        public static readonly string BlessingUulNetol;

        [ItemMetadata("Metadata/Items/Currency/CurrencyBreachUpgradeUniqueChaos")]
        public static readonly string BlessingChayula;

        [ItemMetadata("Metadata/Items/Currency/CurrencyRemoveMod")]
        public static readonly string Annulment;

        [ItemMetadata("Metadata/Items/Currency/CurrencyIdentificationShard")]
        public static readonly string ScrollFragment;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeToMagicShard")]
        public static readonly string TransmutationShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyRerollMagicShard")]
        public static readonly string AlterationShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeToRareShard")]
        public static readonly string AlchemyShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyRemoveModShard")]
        public static readonly string AnnulmentShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyUpgradeMagicToRareShard")]
        public static readonly string RegalShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyRerollRareShard")]
        public static readonly string ChaosShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyAddModToRareShard")]
        public static readonly string ExaltedShard;

        [ItemMetadata("Metadata/Items/Currency/CurrencyDuplicateShard")]
        public static readonly string MirrorShard;

        internal static Dictionary<string, string> ShardToCurrencyDict;

        static CurrencyNames()
        {
            var fieldDict = new Dictionary<string, FieldInfo>();
            foreach (var field in typeof(CurrencyNames).GetFields())
            {
                var metadataAttribute = field.GetCustomAttribute<ItemMetadata>();
                if (metadataAttribute != null)
                {
                    fieldDict.Add(metadataAttribute.Metadata, field);
                }
            }

            int total = fieldDict.Count;
            int processed = 0;

            foreach (var item in Dat.BaseItemTypeDat.RecordsByMetadata)
            {
                if (processed >= total) break;
                if(item.Key == null || item.Value == null) continue;
                if (fieldDict.TryGetValue(item.Key, out FieldInfo field))
                {
                    field.SetValue(null, item.Value.Name);
                    ++processed;
                }
            }
            if (processed < total)
            {
                GlobalLog.Error("[CurrencyNames] Update is required. Not all fields were initialized.");
                BotManager.Stop();
            }

            ShardToCurrencyDict = new Dictionary<string, string>
            {
                [ScrollFragment] = Wisdom,
                [TransmutationShard] = Transmutation,
                [AlterationShard] = Alteration,
                [AlchemyShard] = Alchemy,
                [AnnulmentShard] = Annulment,
                [RegalShard] = Regal,
                [ChaosShard] = Chaos,
                [ExaltedShard] = Exalted,
                [MirrorShard] = Mirror
            };
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class ItemMetadata : Attribute
        {
            public ItemMetadata(string metadata)
            {
                Metadata = metadata;
            }

            public string Metadata { get; set; }
        }
    }
}
