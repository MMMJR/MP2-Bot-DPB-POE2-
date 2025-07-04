﻿using MP2.EXtensions;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    public class TravelToHideoutTask : ITask
    {
        public async Task<bool> Run()
        {
            if (!Mp2Settings.Instance.UseHideout)
                return false;

            var area = World.CurrentArea;
            if (area.IsHideoutArea || area.IsMap)
                return false;

            GlobalLog.Debug("TravelToHideout");

            //Zana daily room is handled by DeviceAreaTask
            if (area.Id.Contains("Daily3_1"))
                return false;

            GlobalLog.Debug("[TravelToHideoutTask] Now traveling to player's hideout.");

            if (area.IsTown || AnyWpNearby)
            {
                if (!await PlayerAction.GoToHideout())
                    ErrorManager.ReportError();
            }
            else
            {
                if (!await PlayerAction.TpToTown())
                    ErrorManager.ReportError();
            }
            return true;
        }

        private static bool AnyWpNearby => LokiPoe.ObjectManager.Objects
            .Any<Waypoint>(w => w.Distance <= 70 && w.PathDistance() <= 73);

        #region Unused interface methods

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public void Tick()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public string Name => "TravelToHideoutTask";
        public string Description => "Task for traveling to player's hideout.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}