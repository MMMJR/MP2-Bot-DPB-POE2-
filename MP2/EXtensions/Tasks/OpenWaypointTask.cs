using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions;
using MP2.EXtensions.Global;
using MP2.EXtensions.Positions;
using System.Threading.Tasks;

namespace MP2.EXtensions.Tasks
{
    public class OpenWaypointTask : ITask
    {
        private static readonly Interval ScanInterval = new Interval(500);

        private static bool _sceptreSpecial;
        private static bool _enabled;

        private static WalkablePosition CachedWaypointPos
        {
            get => CombatAreaCache.Current.Storage["WaypointPosition"] as WalkablePosition;
            set => CombatAreaCache.Current.Storage["WaypointPosition"] = value;
        }

        public async Task<bool> Run()
        {
            await CombatAreaCache.Current.Explorer.Execute();
            if (!_enabled || !World.CurrentArea.IsOverworldArea)
            {
                return true;
            }

            WalkablePosition wpPos = CachedWaypointPos;
            if (wpPos != null)
            {
                if (wpPos.IsFar)
                {
                    wpPos.Come();
                    return true;
                }
                if (!await PlayerAction.OpenWaypoint())
                {
                    ErrorManager.ReportError();
                    return true;
                }
                _enabled = false;
                await Coroutines.CloseBlockingWindows();
                return true;
            }

            return true;
        }

        public void Tick()
        {
            if (!_enabled && !_sceptreSpecial)
            {
                return;
            }

            if (!ScanInterval.Elapsed || !LokiPoe.IsInGame || !World.CurrentArea.IsOverworldArea)
            {
                return;
            }

            if (CachedWaypointPos != null)
            {
                return;
            }

            NetworkObject waypoint = LokiPoe.ObjectManager.Waypoint;
            if (waypoint != null)
            {
                CachedWaypointPos = waypoint.WalkablePosition();
            }

            if (_sceptreSpecial)
            {
                if (waypoint != null)
                {
                    GlobalLog.Warn("[OpenWaypointTask] Enabled (waypoint object detected)");
                    _enabled = true;
                    _sceptreSpecial = false;
                    return;
                }
            }
        }

        public MessageResult Message(DreamPoeBot.Loki.Bot.Message message)
        {
            if (message.Id == Events.Messages.CombatAreaChanged)
            {
                _enabled = false;
                _sceptreSpecial = false;

                DreamPoeBot.Loki.Game.GameData.DatWorldAreaWrapper area = World.CurrentArea;
                string areaId = area.Id;
                if (area.IsOverworldArea && area.HasWaypoint && !World.IsWaypointOpened(areaId))
                {
                    if (!BlockedByBoss(areaId))
                    {
                        GlobalLog.Warn("[OpenWaypointTask] Enabled.");
                        _enabled = true;
                    }
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        private static bool BlockedByBoss(string areaId)
        {
            /*return areaId == World.Act5.CathedralRooftop.Id ||
                   areaId == World.Act8.DoedreCesspool.Id;*/

            return false;
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

        public string Name => "OpenWaypointTask";
        public string Description => "Task that handles waypoint opening.";
        public string Author => "Alcor75";
        public string Version => "1.0";

        #endregion
    }
}
