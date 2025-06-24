using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Positions;
using AreaTransition = DreamPoeBot.Loki.Game.Objects.AreaTransition;
using Chest = DreamPoeBot.Loki.Game.Objects.Chest;

namespace MP2.EXtensions.Global
{
    public static class Travel
    {
        private static readonly HashSet<AreaInfo> NewInstanceRequests = new HashSet<AreaInfo>();
        public static async Task To(AreaInfo area)
        {
            if (Handlers.TryGetValue(area, out Func<Task> handler))
            {
                await handler();
            }
            else
            {
                GlobalLog.Error($"[Travel] Unsupported area: {area}.");
                ErrorManager.ReportCriticalError();
            }
        }
        public static void RequestNewInstance(AreaInfo area)
        {
            if (NewInstanceRequests.Add(area))
            {
                GlobalLog.Debug($"[Travel] New instance requested for {area}");
            }
        }

        private static readonly Dictionary<AreaInfo, Func<Task>> Handlers = new Dictionary<AreaInfo, Func<Task>>
        {
            //[World.Act1.LioneyeWatch] = LioneyeWatch
        };

        private static bool AnyWaypointNearby
        {
            get
            {
                var area = World.CurrentArea;
                //return area.IsTown || area.IsHideoutArea || area.IsMapRoom || LokiPoe.ObjectManager.Objects.Exists(o => o is Waypoint && o.Distance <= 70 && o.PathDistance() <= 73);
                return area.IsTown || area.IsHideoutArea ||
                       LokiPoe.ObjectManager.Objects.Exists(o => o is Waypoint && o.Distance <= 70 && o.PathDistance() <= 73);
            }
        }

        private static async Task WpAreaHandler(AreaInfo area, TgtPosition tgtPos, AreaInfo prevArea, Func<Task> prevAreaHandler, Action postEnter = null)
        {
            if (area.IsCurrentArea)
            {
                OuterLogicError(area);
                return;
            }
            if (prevArea.IsCurrentArea)
            {
                await MoveAndEnter(area, tgtPos, postEnter);
                return;
            }
            if (area.IsWaypointOpened)
            {
                if (AnyWaypointNearby)
                {
                    await TakeWaypoint(area, postEnter);
                }
                else
                {
                    await TpToTown();
                }
                return;
            }
            await prevAreaHandler();
        }

        private static async Task NoWpAreaHandler(AreaInfo area, TgtPosition tgtPos, AreaInfo prevArea, Func<Task> prevAreaHandler, Action postEnter = null)
        {
            if (area.IsCurrentArea)
            {
                OuterLogicError(area);
                return;
            }
            if (prevArea.IsCurrentArea)
            {
                await MoveAndEnter(area, tgtPos, postEnter);
                return;
            }
            await prevAreaHandler();
        }

        private static async Task ThroughMultilevelAreaHander(AreaInfo area, TgtPosition nextLevelTgt, AreaInfo prevArea, Func<Task> prevAreaHandler, Action postEnter = null)
        {
            if (area.IsCurrentArea)
            {
                OuterLogicError(area);
                return;
            }
            if (area.IsWaypointOpened)
            {
                if (AnyWaypointNearby)
                {
                    await TakeWaypoint(area, postEnter);
                }
                else
                {
                    await TpToTown();
                }
                return;
            }
            if (prevArea.IsCurrentArea)
            {
                await MoveAndEnterMultilevel(area, nextLevelTgt, postEnter);
                return;
            }

            await prevAreaHandler();
        }

        private static async Task TownConnectedAreaHandler(AreaInfo area, WalkablePosition transitionPos, AreaInfo town, Func<Task> townHandler, Action postEnter = null)
        {
            if (area.IsCurrentArea)
            {
                OuterLogicError(area);
                return;
            }
            if (area.IsWaypointOpened)
            {
                if (AnyWaypointNearby)
                {
                    await TakeWaypoint(area, postEnter);
                }
                else
                {
                    await TpToTown();
                }
                return;
            }
            if (town.IsCurrentArea)
            {
                await transitionPos.ComeAtOnce();
                await TakeTransition(area, postEnter);
                return;
            }
            await townHandler();
        }

        private static async Task StrictlyWpAreaHandler(AreaInfo area, string hint, Action postEnter = null)
        {
            if (!area.IsWaypointOpened)
            {
                GlobalLog.Error($"[Travel] {area.Name} waypoint is not available. {hint}.");
                ErrorManager.ReportCriticalError();
                return;
            }
            if (AnyWaypointNearby)
            {
                await TakeWaypoint(area, postEnter);
            }
            else
            {
                await TpToTown();
            }
        }

        private static async Task MoveAndEnter(AreaInfo area, TgtPosition tgtPos, Action postEnter)
        {
            var pos = GetCachedTransitionPos(area);
            if (pos != null)
            {
                if (pos.IsFar)
                {
                    pos.Come();
                }
                else
                {
                    await TakeTransition(area, tgtPos, postEnter);
                }
            }
            else
            {
                if (tgtPos.IsFar)
                {
                    tgtPos.Come();
                }
                else
                {
                    await TakeTransition(area, tgtPos, postEnter);
                }
            }
        }

        private static async Task MoveAndEnterMultilevel(AreaInfo area, TgtPosition tgtPos, Action postEnter)
        {
            if (tgtPos.IsFar)
            {
                tgtPos.Come();
                return;
            }

            var transition = await GetTransitionObject(tgtPos, null);
            if (transition == null)
                return;

            bool isDestination = transition.LeadsTo(area);
            bool newInstance = isDestination && NewInstanceRequests.Contains(area);

            if (!await PlayerAction.TakeTransition(transition, newInstance))
            {
                ErrorManager.ReportError();
                return;
            }
            if (isDestination)
            {
                if (newInstance) NewInstanceRequests.Remove(area);
                postEnter?.Invoke();
            }
            else
            {
                tgtPos.ResetCurrentPosition();
            }
        }

        private static async Task TakeWaypoint(AreaInfo area, Action postEnter)
        {
            bool newInstance = NewInstanceRequests.Contains(area);
            if (!await PlayerAction.TakeWaypoint(area, newInstance))
            {
                ErrorManager.ReportError();
                return;
            }
            if (newInstance) NewInstanceRequests.Remove(area);
            postEnter?.Invoke();
        }

        private static async Task TakeTransition(AreaInfo area, Action postEnter)
        {
            var transition = LokiPoe.ObjectManager.Objects.FirstOrDefault<AreaTransition>(a => a.LeadsTo(area));
            if (transition == null)
            {
                GlobalLog.Error($"[Travel] There is no transition that leads to {area}");
                ErrorManager.ReportError();
                return;
            }

            bool newInstance = NewInstanceRequests.Contains(area);
            if (!await PlayerAction.TakeTransition(transition, newInstance))
            {
                ErrorManager.ReportError();
                return;
            }
            if (newInstance) NewInstanceRequests.Remove(area);
            postEnter?.Invoke();
        }

        private static async Task TakeTransition(AreaInfo area, TgtPosition tgtPos, Action postEnter)
        {
            var transition = await GetTransitionObject(tgtPos, area);
            if (transition == null)
                return;

            bool newInstance = NewInstanceRequests.Contains(area);
            if (!await PlayerAction.TakeTransition(transition, newInstance))
            {
                ErrorManager.ReportError();
                return;
            }
            if (newInstance) NewInstanceRequests.Remove(area);
            postEnter?.Invoke();
        }

        private static async Task<AreaTransition> GetTransitionObject(TgtPosition tgtPos, AreaInfo area)
        {
            var transition = LokiPoe.ObjectManager.Objects.Closest<AreaTransition>();
            if (transition == null)
            {
                GlobalLog.Warn("[Travel] There is no area transition near tgt position.");
                tgtPos.ProceedToNext();
                return null;
            }
            if (transition.TransitionType == DreamPoeBot.Loki.Game.GameData.TransitionTypes.NormalToCorrupted)
            {
                GlobalLog.Warn("[Travel] Corrupted area entrance has the same tgt as our destination.");
                tgtPos.ProceedToNext();
                return null;
            }
            if (!transition.IsTargetable)
            {
                if (area == null)
                {
                    GlobalLog.Debug("[Travel] Waiting for transition activation.");
                }
                else
                {
                    GlobalLog.Debug($"[Travel] Waiting for \"{area.Name}\" transition activation.");
                }
                return null;
            }
            if (area != null)
            {
                var dest = transition.Destination;
                if (area != dest)
                {
                    GlobalLog.Warn($"[Travel] Transition leads to \"{dest.Name}\". Expected: \"{area.Name}\".");
                    tgtPos.ProceedToNext();
                    return null;
                }
            }
            return transition;
        }

        private static async Task TpToTown()
        {
            if (!await PlayerAction.TpToTown())
                ErrorManager.ReportError();
        }

        private static WalkablePosition GetCachedTransitionPos(AreaInfo area)
        {
            return CombatAreaCache.Current.AreaTransitions.Find(t => t.Destination == area)?.Position;
        }

        private static void OuterLogicError(AreaInfo area)
        {
            GlobalLog.Error($"[Travel] Outer logic error. Travel to {area} has been called, but we are already here.");
            ErrorManager.ReportError();
        }

    }
}
