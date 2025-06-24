using MP2.EXtensions.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Common;
using DreamPoeBot;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using DreamPoeBot.Loki.Bot.Pathfinding;

namespace MP2.EXtensions.Global
{
    public static class StuckDetection
    {
        public const string TriggeredMessage = "stuck_detection_triggered_event";
        public static event Action<int> Triggered;

        private static readonly Interval ScanInterval = new Interval(2000);
        private const int MobScanRange = 100;

        private const int SmallStuckRange = 30;
        private const int MediumStuckRange = 45;
        private const int LongStuckRange = 70;

        private static int _smallStuckCount;
        private static int _mediumStuckCount;
        private static int _longStuckCount;

        private static readonly Dictionary<int, int> MobData = new Dictionary<int, int>();

        private static Vector2i _myLastPos;
        private static int _lastWorldItemCount;

        static StuckDetection()
        {
            Events.AreaChanged += OnAreaChanged;
        }

        private static void OnAreaChanged(object sender, AreaChangedArgs args)
        {
            Reset();
            _lastWorldItemCount = 0;
            _myLastPos = Vector2i.Zero;
            MobData.Clear();
        }

        public static void Reset()
        {
            _smallStuckCount = 0;
            _mediumStuckCount = 0;
            _longStuckCount = 0;
        }

        public static async void Tick()
        {
            if (!Mp2Settings.Instance.StuckDetectionEnabled || !ScanInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || !World.CurrentArea.IsCombatArea)
                return;

            FillMobData();

            if (SimulacrumTask.BotState == SimulacrumBotState.Wave) return;

            //do not perform stuck check if something was changed
            if (CheckMobHpDecrease() || CheckWorldItemDecrease())
                return;

            var myPos = LokiPoe.MyPosition;

            if (myPos.Distance(_myLastPos) < SmallStuckRange)
            {
                ++_smallStuckCount;

                if (_smallStuckCount >= 1)
                    HandleBlockingChestsTask.Enabled = true;

                if (_smallStuckCount >= 3)
                {
                    if (MP2.IsInteracting)
                    {
                        return;
                    }

                    if(Mp2Settings.Instance.SimulacrumBot && SimulacrumTask.BotState == SimulacrumBotState.Initiator)
                        LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);

                    await Coroutines.CloseBlockingWindows();
                    
                    GlobalLog.Debug($"[StuckDetection] Small range stuck count: {_smallStuckCount}");
                }

                if (_smallStuckCount >= Mp2Settings.Instance.MaxStuckCountSmall)
                {
                    GlobalLog.Info($"[StuckDetection] Small range stuck count finish: {_smallStuckCount}.");
                    if (TrackMobLogic.CurrentTarget != null)
                        Blacklist.Add(TrackMobLogic.CurrentTarget.Id, TimeSpan.FromSeconds(15), "Blocked By door.");

                    LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                }

                if (_smallStuckCount >= 12)
                {
                    GlobalLog.Info($"[StuckDetection] Small range stuck count finish: {_smallStuckCount}.");

                    //HandleStuck();

                    if (MapExplorationTask.CompletionPointReached && World.CurrentArea.IsMap && MapExplorationTask.MapCompleted)
                    {
                        await PlayerAction.Logout();
                        return;
                    }
                }

                if (_smallStuckCount >= 14)
                {
                    GlobalLog.Info($"[StuckDetection] Small range stuck count finish: {_smallStuckCount}.");
                    
                    await PlayerAction.Logout();

                    HandleStuck();
                    return;
                }
            }
            else
            {
                _smallStuckCount = 0;
            }

            if (myPos.Distance(_myLastPos) < MediumStuckRange)
            {
                ++_mediumStuckCount;

                if (_mediumStuckCount >= Mp2Settings.Instance.MaxStuckCountSmall)
                {
                    GlobalLog.Debug($"[StuckDetection] Medium range stuck count: {_mediumStuckCount}");
                    GlobalLog.Error($"[StuckDetection] Medium range stuck count: {_mediumStuckCount}.");
                    if (KillBossTask._currentTarget != null)
                        Blacklist.Add(KillBossTask._currentTarget.Id, TimeSpan.FromSeconds(15), "Blocked By door.");
                    if (TrackMobLogic.CurrentTarget != null)
                        Blacklist.Add(TrackMobLogic.CurrentTarget.Id, TimeSpan.FromSeconds(15), "Blocked By door.");

                    if (!MP2.IsInteracting)
                    {
                        await Coroutines.CloseBlockingWindows();
                        LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                    }
                }

                if (_mediumStuckCount >= Mp2Settings.Instance.MaxStuckCountMedium)
                {
                    if (MapExplorationTask.CompletionPointReached && World.CurrentArea.IsMap && MapExplorationTask.MapCompleted)
                    {
                        await PlayerAction.Logout();
                        return;
                    }
                }

                if (_mediumStuckCount >= 20)
                {
                    await PlayerAction.Logout();
                    HandleStuck();
                    return;
                }
            }
            else
            {
                _mediumStuckCount = 0;
            }

            _myLastPos = myPos;
        }

        private static void FillMobData()
        {
            foreach (var obj in LokiPoe.ObjectManager.Objects)
            {
                var mob = obj as Monster;

                if (mob == null || !IsMonsterForScan(mob))
                    continue;

                var id = mob.Id;
                if (!MobData.ContainsKey(id))
                    MobData.Add(id, mob.Health);
            }
        }

        private static bool CheckMobHpDecrease()
        {
            bool decreased = false;
            var toRemove = new List<int>();
            var decreasedHealth = new Dictionary<int, int>();

            foreach (var kvp in MobData)
            {
                var mob = LokiPoe.ObjectManager.Objects.FirstOrDefault<Monster>(m => m.Id == kvp.Key);
                if (mob == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }
                if (mob.IsDead)
                {
                    toRemove.Add(kvp.Key);
                    decreased = true;
                    continue;
                }
                if (mob.Health < kvp.Value)
                {
                    decreased = true;
                    decreasedHealth.Add(kvp.Key, mob.Health);
                }
            }
            foreach (var kvp in decreasedHealth)
            {
                MobData[kvp.Key] = kvp.Value;
            }
            foreach (var id in toRemove)
            {
                MobData.Remove(id);
            }
            return decreased;
        }

        private static bool CheckWorldItemDecrease()
        {
            var itemCount = CombatAreaCache.Current.Items.Count;
            bool decreased = itemCount < _lastWorldItemCount;
            _lastWorldItemCount = itemCount;
            return decreased;
        }

        private static void HandleStuck()
        {
            if (StopBotOnStuck)
            {
                GlobalLog.Warn("[StuckDetection] StopBotOnStuck is true. Now reseting stuck counters and stopping the bot.");
                Reset();
                //BotManager.Stop();
                return;
            }

            var cache = CombatAreaCache.Current;

            ++cache.StuckCount;

            GlobalLog.Error($"[StuckDetection] Stuck incidents in current area: {cache.StuckCount}");

            GlobalLog.Info($"[StuckDetection] Triggered event ({cache.StuckCount})");
            Triggered?.Invoke(cache.StuckCount);
            Utility.BroadcastMessage(null, TriggeredMessage);
            Reset();

        }

        private static bool IsMonsterForScan(Monster m)
        {
            return !m.IsDead && m.Reaction == Reaction.Enemy && m.Distance <= MobScanRange;
        }

        /// <summary>
        /// For debugging purposes. Bot will stop instead of logging out.
        /// </summary>
        public static bool StopBotOnStuck { get; set; }
    }
}