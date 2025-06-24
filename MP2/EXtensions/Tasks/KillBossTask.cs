using MP2.EXtensions;
using MP2.EXtensions.Global;
using MP2.EXtensions.Positions;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Mapper;
using Message = DreamPoeBot.Loki.Bot.Message;
using System.Windows.Automation;

namespace MP2.EXtensions.Tasks
{
    public class KillBossTask : ITask
    {
        private const int MaxKillAttempts = 50;

        private static readonly Interval LogInterval = new Interval(1000);
        private static readonly Interval TickInterval = new Interval(200);

        public static bool BossKilled { get; private set; }

        public static List<CachedBoss> CachedBosses = new List<CachedBoss>();

        public static CachedBoss _currentTarget;
        private static int _bossesKilled;

        private static bool _multiPhaseBoss;
        private static bool _teleportingBoss;
        private static string _priorityBossName;
        private static int _bossRange;
        private static int MoveAttempts;

        private static Func<Monster, bool> _isMapBoss = DefaultBossSelector;

        public async Task<bool> Run()
        {
            if (MapExplorationTask.MapCompleted)
                return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            var area = World.CurrentArea;

            if (!area.IsMap)
                return false;

            GlobalLog.Debug("KillBossTask");

            /*foreach(CachedBoss _boss in CachedBosses)
            {
                _currentTarget = _boss;
                if (_currentTarget == null) continue;

                var pos = _currentTarget.Position;
                if (pos == null) continue;
                if (_currentTarget.Position.Distance > 30 && _currentTarget.Position.Distance <= 45t)
                {
                    GlobalLog.Debug("Aqui35 :");
                    if (!pos.TryCome())
                    {
                        GlobalLog.Error($"[SpecialObjectTask] Fail to move to {pos}. Marking this special object as unwalkable.");
                        _currentTarget.Unwalkable = true;
                        _currentTarget = null;
                    }
                    await Coroutines.FinishCurrentMoveAction();
                    return true;
                }
            }    */

            if (_currentTarget == null)
            {
                if ((_currentTarget = CachedBosses.ClosestValid(b => !b.IsDead && !b.Ignored)) == null)
                    return false;
            }

            if (Blacklist.Contains(_currentTarget.Id))
            {
                GlobalLog.Warn("[KillBossTask] Boss is in global blacklist.");
                return false;
            }

            if (_priorityBossName != null && _currentTarget.Position.Name != _priorityBossName)
            {
                var priorityBoss = CachedBosses.ClosestValid(b => !b.IsDead && !b.Ignored && b.Position.Name == _priorityBossName);
                if (priorityBoss != null)
                {
                    GlobalLog.Debug($"[KillBossTask] Switching current target to \"{priorityBoss}\".");
                    _currentTarget = priorityBoss;
                    return true;
                }
            }

            if (_currentTarget.IsDead)
            {
                var newBoss = CachedBosses.Valid().FirstOrDefault(b => !b.IsDead);
                if (newBoss != null) _currentTarget = newBoss;
                else return false;
            }

            var pos = _currentTarget.Position;

            

            if (pos.Distance <= 50 && pos.PathDistance <= 55)
            {
                var bossObj = _currentTarget.Object as Monster;
                if (bossObj == null)
                {
                    if (_teleportingBoss)
                    {
                        CachedBosses.Remove(_currentTarget);
                        _currentTarget = null;
                        return true;
                    }
                    GlobalLog.Debug("[KillBossTask] We are close to last know position of map boss, but boss object does not exist anymore.");
                    GlobalLog.Debug("[KillBossTask] Most likely this boss does not spawn a corpse or was shattered/exploded.");
                    _currentTarget.IsDead = true;
                    _currentTarget = null;
                    RegisterDeath();
                    return true;
                }
            }

            if (pos.Distance > _bossRange)
            {
                if (LogInterval.Elapsed)
                {
                    GlobalLog.Debug($"[KillBossTask] Going to {pos}");
                }
                if (!pos.TryCome())
                {
                    GlobalLog.Error(MapData.Current.Type == MapType.Regular
                        ? $"[KillBossTask] Unexpected error. Fail to move to map boss ({pos.Name}) in a regular map."
                        : $"[KillBossTask] Fail to move to the map boss \"{pos.Name}\". Will try again after area transition.");
                    _currentTarget.Unwalkable = true;
                    _currentTarget = null;
                    return true;
                }
                /*var attemptsMove = ++_currentTarget.InteractionAttempts;
                if (attemptsMove > 70)
                {
                    Blacklist.Add(_currentTarget.Object.Id, TimeSpan.FromSeconds(30), "Blocked By door.");
                    return true;
                }*/

                if (_currentTarget == null) return true;
                if (_currentTarget.Object == null) return true;
                var blockedByDoor = TrackMobLogic.ClosedDoorBetween(LokiPoe.Me, _currentTarget.Object, 30, 30, false);
                if (blockedByDoor)
                {
                    Blacklist.Add(_currentTarget.Object.Id, TimeSpan.FromMinutes(1), "Blocked By door.");
                }
                return true;
            }

            var attempts = ++_currentTarget.InteractionAttempts;

            // Helps to trigger Gorge and Underground River bosses
            if (attempts == MaxKillAttempts / 4)
            {
                GlobalLog.Debug("[KillBossTask] Trying to move around to trigger a boss.");
                var distantPos = WorldPosition.FindPathablePositionAtDistance(40, 70, 5);
                if (distantPos != null)
                {
                    await Move.AtOnce(distantPos, "distant position", 10);
                }
            }
            if (attempts > MaxKillAttempts)
            {
                _currentTarget.Ignored = true;
                _currentTarget = null;
                RegisterDeath();
                return true;
            }
            await Coroutines.FinishCurrentAction();
            GlobalLog.Debug($"[KillBossTask] Waiting for map boss to become active ({attempts}/{MaxKillAttempts})");
            await Wait.StuckDetectionSleep(200);
            return true;
        }

        public void Tick()
        {
            if (BossKilled || MapExplorationTask.MapCompleted)
                return;

            if (!TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            foreach (var obj in LokiPoe.ObjectManager.Objects)
            {
                var mob = obj as Monster;

                if (mob == null)
                    continue;

                if(!Mp2Settings.Instance.BreachRunner)
                    if(mob.IsHidden) continue;

                if (!mob.IsMapBoss) continue;

                var id = mob.Id;
                var cached = CachedBosses.Find(b => b.Id == id);

                if (!mob.IsDead)
                {
                    var pos = mob.WalkablePosition(5, 20);
                    if (cached != null)
                    {
                        cached.Position = pos;
                    }
                    else
                    {
                        CachedBosses.Add(new CachedBoss(id, pos, false));
                        GlobalLog.Warn($"[KillBossTask] Registering {pos}");
                    }
                }
                else
                {
                    if (cached == null)
                    {
                        GlobalLog.Warn($"[KillBossTask] Registering dead map boss \"{mob.Name}\".");
                        CachedBosses.Add(new CachedBoss(id, mob.WalkablePosition(), true));
                        RegisterDeath();
                    }
                    else if (!cached.IsDead)
                    {
                        GlobalLog.Warn($"[KillBossTask] Registering death of \"{mob.Name}\".");
                        cached.IsDead = true;
                        if (!_multiPhaseBoss) _currentTarget = null;
                        RegisterDeath();
                    }
                }
            }
        }

        private static void RegisterDeath()
        {
            ++_bossesKilled;
        }

        private static int BossAmountForMap
        {
            get
            {
                int bosses;
                if (!BossesPerMap.TryGetValue(World.CurrentArea.Name, out bosses))
                {
                    bosses = 1;
                }
                LokiPoe.LocalData.MapMods.TryGetValue(StatTypeGGG.MapSpawnTwoBosses, out int twoBossesFlag);
                if (twoBossesFlag == 1)
                {
                    return bosses * 2;
                }
                return Math.Max(bosses, CachedBosses.Count);
            }
        }

        private static readonly Dictionary<string, int> BossesPerMap = new Dictionary<string, int>
        {

        };

        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == MP2.Messages.NewMapEntered)
            {
                GlobalLog.Info("[KillBossTask] Reset.");
                var areaName = message.GetInput<string>();
                _bossesKilled = 0;
                _currentTarget = null;
                BossKilled = false;
                _multiPhaseBoss = false;
                _teleportingBoss = false;
                CachedBosses.Clear();
                SetBossSelector(areaName);
                SetPriorityBossName(areaName);
                SetBossRange(areaName);

                return MessageResult.Processed;
            }
            if (id == ComplexExplorer.LocalTransitionEnteredMessage)
            {
                GlobalLog.Info("[KillBossTask] Resetting unwalkable flags.");
                foreach (var cachedBoss in CachedBosses)
                {
                    cachedBoss.Unwalkable = false;
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        private static void SetPriorityBossName(string areaName)
        {
            _priorityBossName = null;
        }

        private static void SetBossRange(string areaName)
        {
            _bossRange = 30;
            GlobalLog.Info($"[KillBossTask] Boss range: {_bossRange}");
        }

        private static void SetBossSelector(string areaName)
        {
            _isMapBoss = DefaultBossSelector;
        }

        private static bool DefaultBossSelector(Monster m)
        {
            return m.IsMapBoss;
        }

        public class CachedBoss : CachedObject
        {
            public bool IsDead { get; set; }

            public CachedBoss(int id, WalkablePosition position, bool isDead) : base(id, position)
            {
                IsDead = isDead;
            }
        }

        #region Unused interface methods

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public string Name => "KillBossTask";
        public string Description => "Task for killing map boss.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}