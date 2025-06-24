using MP2.EXtensions;
using DreamPoeBot.Loki.Bot;
using MP2.EXtensions.Mapper;
using Message = DreamPoeBot.Loki.Bot.Message;
using MP2.EXtensions.Global;

namespace MP2.EXtensions.Tasks
{
    public class FinishMapTask : ITask
    {
        private static int _pulse;

        public async Task<bool> Run()
        {
            if (!World.CurrentArea.IsMap)
                return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            await Coroutines.FinishCurrentAction();

            var maxPulses = MaxPulses;

            if (Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
                maxPulses = 25;

            if (_pulse < maxPulses)
            {
                ++_pulse;
                GlobalLog.Info($"[FinishMapTask] Final pulse {_pulse}/{maxPulses}");
                await Wait.SleepSafe(500);
                return true;
            }

            if (!MapExplorationTask.CompletionPointReached && !MapExplorationTask.MapCompleted)
            {
                MapExplorationTask.CompletionPointReached = true;
                SpecialObjectTask.OnMapCompletePointReached();
                _pulse = 0;
                return true;
            }

            GlobalLog.Warn("[FinishMapTask] Now leaving current map.");

            if (!await PlayerAction.TpToTown())
            {
                ErrorManager.ReportError();
                if (World.CurrentArea.IsTown || World.CurrentArea.IsHideoutArea)
                {
                    MP2.IsOnRun = false;
                    Statistics.Instance.OnMapFinish();
                    GlobalLog.Info("[MapBot] MapFinished event.");
                    Utility.BroadcastMessage(this, MP2.Messages.MapFinished);
                    DeviceAreaTask._toMap = false;
                    MP2.IsOnRun = false;
                }
                return true;
            }

            MP2.IsOnRun = false;
            Statistics.Instance.OnMapFinish();
            GlobalLog.Info("[MapBot] MapFinished event.");
            Utility.BroadcastMessage(this, MP2.Messages.MapFinished);
            DeviceAreaTask._toMap = false;
            MP2.IsOnRun = false;
            return true;
        }

        private static int MaxPulses
        {
            get
            {
                if (!KillBossTask.BossKilled && !MapData.Current.IgnoredBossroom)
                {
                    var areaName = World.CurrentArea.Name;
                }
                return 8;
            }
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == MP2.Messages.NewMapEntered)
            {
                _pulse = 0;
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        #region Unused interface methods

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

        public string Name => "FinishMapTask";
        public string Description => "Task for leaving current map.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}