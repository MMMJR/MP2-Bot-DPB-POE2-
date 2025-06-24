using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using MP2.Class;
using MP2.Helpers;
using MP2.EXtensions;
using FlaskHud = DreamPoeBot.Loki.Game.LokiPoe.InGameState.QuickFlaskHud;
using Message = DreamPoeBot.Loki.Bot.Message;
using Microsoft.VisualBasic.Logging;

namespace MP2.EXtensions.Tasks
{
    public class FlaskTask : ITask
    {
        public const string LifeFlaskEffect = "flask_effect_life";
        public const string ManaFlaskEffect = "flask_effect_mana";
        public static bool ShouldTeleport = false;
        public static bool ShouldOpenPortal = false;

        public FlaskTask(int Index)
        {
            Index = 0;
        }

        private static readonly Dictionary<string, string> FlaskEffects = new Dictionary<string, string>
        {
            [FlaskNames.Diamond] = "flask_utility_critical_strike_chance",
            [FlaskNames.Ruby] = "flask_utility_resist_fire",
            [FlaskNames.Sapphire] = "flask_utility_resist_cold",
            [FlaskNames.Topaz] = "flask_utility_resist_lightning",
            [FlaskNames.Granite] = "flask_utility_ironskin",
            [FlaskNames.Quicksilver] = "flask_utility_sprint",
            [FlaskNames.Amethyst] = "flask_utility_resist_chaos",
            [FlaskNames.Quartz] = "flask_utility_phase",
            [FlaskNames.Jade] = "flask_utility_evasion",
            [FlaskNames.Basalt] = "flask_utility_stone",
            [FlaskNames.Aquamarine] = "flask_utility_aquamarine",
            [FlaskNames.Stibnite] = "flask_utility_smoke",
            [FlaskNames.Sulphur] = "flask_utility_consecrate",
            [FlaskNames.Silver] = "flask_utility_haste",
            [FlaskNames.Bismuth] = "flask_utility_prismatic",


            [FlaskNames.BloodOfKarui] = "unique_flask_blood_of_the_karui",
            [FlaskNames.ZerphiLastBreath] = "unique_flask_zerphis_last_breath",
            [FlaskNames.DivinationDistillate] = "unique_flask_divination_distillate",
            [FlaskNames.CoruscatingElixir] = "unique_flask_chaos_damage_damages_es",
            [FlaskNames.TasteOfHate] = "unique_flask_taste_of_hate",
            [FlaskNames.KiaraDetermination] = "kiaras_determination",
            [FlaskNames.ForbiddenTaste] = "unique_flask_forbidden_taste",
            [FlaskNames.LionRoar] = "unique_flask_lions_roar",
            [FlaskNames.WitchfireBrew] = "unique_flask_witchfire_brew",
            [FlaskNames.AtziriPromise] = "unique_flask_atziris_promise",
            [FlaskNames.DyingSun] = "unique_flask_dying_sun",
            [FlaskNames.RumiConcoction] = "unique_flask_rumis_concoction",
            [FlaskNames.LaviangaSpirit] = "unique_flask_laviangas_cup",
            [FlaskNames.SorrowOfDivine] = "unique_flask_zealots_oath",
            [FlaskNames.OverflowingChalice] = "overflowing_chalice",
            [FlaskNames.SinRebirth] = "unholy_might_from_flask",
            [FlaskNames.VesselOfVinktar] = "lightning_flask",
            [FlaskNames.WiseOak] = "unique_flask_the_basics",
            [FlaskNames.CoralitoSignature] = "unique_flask_gorgon_poison",

            // No unique effects
            [FlaskNames.Rotgut] = " flask_utility_sprint",
            [FlaskNames.SoulCatcher] = "unique_flask_soul_catcher",
            [FlaskNames.DoedreElixir] = string.Empty,
            [FlaskNames.WrithingJar] = string.Empty,
        };

        public static class FlaskNames
        {
            public const string Diamond = "Diamond Flask";
            public const string Ruby = "Ruby Flask";
            public const string Sapphire = "Sapphire Flask";
            public const string Topaz = "Topaz Flask";
            public const string Granite = "Granite Flask";
            public const string Quicksilver = "Quicksilver Flask";
            public const string Amethyst = "Amethyst Flask";
            public const string Quartz = "Quartz Flask";
            public const string Jade = "Jade Flask";
            public const string Basalt = "Basalt Flask";
            public const string Aquamarine = "Aquamarine Flask";
            public const string Stibnite = "Stibnite Flask";
            public const string Sulphur = "Sulphur Flask";
            public const string Silver = "Silver Flask";
            public const string Bismuth = "Bismuth Flask";


            public const string BloodOfKarui = "Blood of the Karui";
            public const string DoedreElixir = "Doedre's Elixir";
            public const string ZerphiLastBreath = "Zerphi's Last Breath";
            public const string LaviangaSpirit = "Lavianga's Spirit";
            public const string DivinationDistillate = "Divination Distillate";
            public const string WrithingJar = "The Writhing Jar";
            public const string WiseOak = "The Wise Oak";
            public const string SinRebirth = "Sin's Rebirth";
            public const string CoruscatingElixir = "Coruscating Elixir";
            public const string TasteOfHate = "Taste of Hate";
            public const string KiaraDetermination = "Kiara's Determination";
            public const string ForbiddenTaste = "Forbidden Taste";
            public const string LionRoar = "Lion's Roar";
            public const string OverflowingChalice = "The Overflowing Chalice";
            public const string SorrowOfDivine = "The Sorrow of the Divine";
            public const string Rotgut = "Rotgut";
            public const string WitchfireBrew = "Witchfire Brew";
            public const string AtziriPromise = "Atziri's Promise";
            public const string DyingSun = "Dying Sun";
            public const string RumiConcoction = "Rumi's Concoction";
            public const string VesselOfVinktar = "Vessel of Vinktar";
            public const string CoralitoSignature = "Coralito's Signature";
            public const string SoulCatcher = "Soul Catcher";
        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame) return false;

            if (LokiPoe.CurrentWorldArea.IsTown) return false;
            if (!LokiPoe.CurrentWorldArea.IsCombatArea) return false;
            var hpPct = LokiPoe.Me.HealthPercent;
            var esPct = LokiPoe.Me.EnergyShieldPercent;
            var manaPct = LokiPoe.Me.ManaPercent;
            GlobalLog.Debug("Flasks");
            #region Flasks
            foreach (var flask in Mp2Settings.Instance.Flasks)
            {
                if(flask == null) continue;
                if (!flask.Enabled) continue;
                var postUseDelay = flask.PostUseDelay.ElapsedMilliseconds;
                if (postUseDelay < 100) continue;
                if (postUseDelay < flask.Cooldown) continue;
                var thisflask = FlaskHud.InventoryControl.Inventory.Items.FirstOrDefault(x => x.LocationTopLeft.X == flask.Slot - 1);
                if (thisflask == null) continue;
                if (!flask.IgnoreEffect && !thisflask.Components.FlaskComponent.IsInstantRecovery && flask.PostUseDelay.ElapsedMilliseconds < thisflask.Components.FlaskComponent.RecoveryTime.TotalMilliseconds) continue;
                if (!flask.IgnoreEffect && HasFlaskEffect(thisflask)) continue;
                var threshold = flask.UseEs ? esPct : flask.UseMana ? manaPct : hpPct;
                if (threshold < flask.Threshold)
                {
                    if (UseFlask(thisflask, flask.Slot))
                    {
                        flask.PostUseDelay.Restart();
                        return true;
                    }
                }
            }
            #endregion

            return false;
        }

        private bool HasFlaskEffect(Item thisflask)
        {
            string name = thisflask.ProperName();
            string effect = Flasks.GetEffect(name);
            if (effect == null) return false;
            return LokiPoe.Me.HasAura(effect);
        }

        private static void CastDefensiveSkill(DefensiveSkillsClass skillClass)
        {
            if (skillClass == null) return;
            var skills = LokiPoe.InGameState.SkillBarHud.Skills;
            if (skills == null) return;
            Skill skill = skills.FirstOrDefault(s => s.Name == skillClass.Name);
            if (skill != null && skill.CanUse())
            {
                LokiPoe.InGameState.SkillBarHud.Use(skill.Slot, false, false);
                skillClass.Casted();
            }
        }

        private static bool UseFlask(Item thisflask, int slot)
        {
            if (!thisflask.CanUse) return false;

            if (!FlaskHud.UseFlaskInSlot(slot)) return false;

            if (!MP2.IsInteracting && slot == 1)
                LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);

            return true;
        }

        #region Unused interface methods

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public void Start()
        {

        }

        public void Tick()
        {
        }

        public void Stop()
        {
        }

        public string Name => "FlaskTask";
        public string Description => "This task provides a logic to use flasks.";
        public string Author => "Alcor75";
        public string Version => "1.0";

        #endregion
    }
}

