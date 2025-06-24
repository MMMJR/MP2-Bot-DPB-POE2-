using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.FilesInMemory;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using Microsoft.VisualBasic.Logging;

namespace MP2.EXtensions.Global
{

    public static class TrackMobLogic
    {
        private const int MaxKillAttempts = 20;
        private static readonly Interval LogInterval = new Interval(1000);

        public static CachedObject CurrentTarget;

        static TrackMobLogic()
        {
            Events.AreaChanged += (sender, args) => CurrentTarget = null;
        }

        public static async Task<bool> Execute(int range = -1)
        {
            var cachedMonsters = CombatAreaCache.Current.Monsters;
            GlobalLog.Debug("cachedMonster Count: " + cachedMonsters.Count);
            if (CurrentTarget == null)
            {
                CurrentTarget = range == -1
                    ? cachedMonsters.ClosestValid()
                    : cachedMonsters.ClosestValid(m => m.Position.Distance <= range);

                if (CurrentTarget == null)
                    return false;
            }
            GlobalLog.Debug("TrackMobLogic");
            if (Blacklist.Contains(CurrentTarget.Id))
            {
                GlobalLog.Debug("[TrackMobLogic] Current target is in global blacklist. Now abandoning it.");
                CurrentTarget.Ignored = true;
                CurrentTarget = null;
                return true;
            }

            var blockedByDoor = ClosedDoorBetween(LokiPoe.Me.Position, CurrentTarget.Position, 30, 30, false);

            if (blockedByDoor)
            {
                Blacklist.Add(CurrentTarget.Id, TimeSpan.FromMinutes(2), "Blocked By door.");
                return false;
            }

            var pos = CurrentTarget.Position;
            if (pos.IsFar || pos.IsFarByPath)
            {
                if (LogInterval.Elapsed)
                {
                    GlobalLog.Debug($"[TrackMobTask] Cached monster locations: {cachedMonsters.Valid().Count()}");
                    GlobalLog.Debug($"[TrackMobTask] Moving to {pos}");
                }
                if (!PlayerMoverManager.MoveTowards(pos))
                {
                    GlobalLog.Error($"[TrackMobTask] Fail to move to {pos}. Marking this monster as unwalkable.");
                    CurrentTarget.Unwalkable = true;
                    CurrentTarget = null;
                }
                return true;
            }

            var monsterObj = CurrentTarget.Object as Monster;

            // Untested fix to not wait on a captured beast. Will be changed once confirmed issue is solved.
            //if (monsterObj == null || monsterObj.IsDead || (Loki.Game.LokiPoe.InstanceInfo.Bestiary.IsActive && (monsterObj.HasBestiaryCapturedAura || monsterObj.HasBestiaryDisappearingAura)))

            if (monsterObj == null || monsterObj.IsDead)
            {
                cachedMonsters.Remove(CurrentTarget);
                CurrentTarget = null;
            }
            else
            {
                var attempts = ++CurrentTarget.InteractionAttempts;
                if (attempts > MaxKillAttempts)
                {
                    GlobalLog.Error("[TrackMobTask] All attempts to kill current monster have been spent. Now ignoring it.");
                    CurrentTarget.Ignored = true;
                    CurrentTarget = null;
                    return true;
                }
                GlobalLog.Debug($"[TrackMobTask] Alive monster is nearby, this is our {attempts}/{MaxKillAttempts} attempt to kill it.");
                await DreamPoeBot.Loki.Coroutine.Coroutine.Sleep(200);
            }
            return true;
        }

        public static bool ClosedDoorBetween(NetworkObject start, NetworkObject end, int distanceFromPoint = 10, int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }
        public static bool ClosedDoorBetween(Entity start, NetworkObject end, int distanceFromPoint = 10, int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }
        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(NetworkObject start, Vector2i end, int distanceFromPoint = 10, int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end, distanceFromPoint, stride, dontLeaveFrame);
        }
        public static bool ClosedDoorBetween(Entity start, Vector2i end, int distanceFromPoint = 10, int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start.Position, end, distanceFromPoint, stride, dontLeaveFrame);
        }
        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(Vector2i start, NetworkObject end, int distanceFromPoint = 10, int stride = 10, bool dontLeaveFrame = false)
        {
            return ClosedDoorBetween(start, end.Position, distanceFromPoint, stride, dontLeaveFrame);
        }

        /// <summary>
        /// Checks for a closed door between start and end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
        /// <param name="stride">The distance between points to check in the path.</param>
        /// <param name="dontLeaveFrame">Should the current frame not be left?</param>
        /// <returns>true if there's a closed door and false otherwise.</returns>
        public static bool ClosedDoorBetween(Vector2i start, Vector2i end, int distanceFromPoint = 10, int stride = 10, bool dontLeaveFrame = false)
        {
            // We need to store positions and not objects to avoid frame leaving issues.
            var doorPositions = LokiPoe.ObjectManager.AnyDoors.Where(d => !d.IsOpened).Select(d => d.Position).ToList();
            if (!doorPositions.Any())
                return false;

            var path = ExilePather.GetPointsOnSegment(start, end, dontLeaveFrame);

            for (var i = 0; i < path.Count; i += stride)
            {
                foreach (var doorPosition in doorPositions)
                {
                    if (doorPosition.Distance(path[i]) <= distanceFromPoint)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
