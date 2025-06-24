using System;
using MP2.EXtensions.Global;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    public class ProximityTriggerTask : ITask
    {
        private static readonly Interval TickInterval = new Interval(200);

        private static string _triggerMetadata = "Metadata/Terrain/Gallows/Leagues/Delirium/Objects/DeliriumInitiator";
        private static CachedObject _trigger = null;
        private static Func<Task> _waitFunc;

        public async Task<bool> Run()
        {
            if (_triggerMetadata == null || MapExplorationTask.MapCompleted)
                return false;

            if (_trigger == null || _trigger.Ignored || _trigger.Unwalkable)
                return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            if (!World.CurrentArea.IsMap)
                return false;

            GlobalLog.Debug("ProximityTriggerTask");

            var pos = _trigger.Position;
            if (pos.Distance > 10 || pos.PathDistance > 12)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Error($"[ProximityTriggerTask] Fail to move to {pos}. Marking this trigger object as unwalkable.");
                    _trigger.Unwalkable = true;
                }
                return true;
            }

            await Coroutines.FinishCurrentAction();

            if (_waitFunc != null)
                await _waitFunc();

            _trigger.Ignored = true;
            return true;
        }

        public void Tick()
        {
            if (_triggerMetadata == null || _trigger != null || MapExplorationTask.MapCompleted)
                return;

            if (!TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            foreach (var obj in LokiPoe.ObjectManager.Objects)
            {
                if (obj.Metadata == _triggerMetadata || obj.Metadata == "Metadata/MiscellaneousObjects/Breach/EndGameBreach")
                {
                    var pos = obj.WalkablePosition();
                    GlobalLog.Warn($"[ProximityTriggerTask] Registering {pos}");
                    _trigger = new CachedObject(obj.Id, pos);
                    return;
                }
            }
        }

        private static void Reset(string areaName)
        {
            _trigger = null;
            _waitFunc = null;
        }

        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == MP2.Messages.NewMapEntered)
            {
                GlobalLog.Info("[ProximityTriggerTask] Reset.");

                Reset(message.GetInput<string>());

                if (_triggerMetadata != null)
                    GlobalLog.Info("[ProximityTriggerTask] Enabled.");

                return MessageResult.Processed;
            }
            if (id == ComplexExplorer.LocalTransitionEnteredMessage)
            {
                if (_trigger != null)
                {
                    GlobalLog.Info("[ProximityTriggerTask] Resetting unwalkable flag.");
                    _trigger.Unwalkable = false;
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
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

        public string Name => "ProximityTriggerTask";
        public string Description => "Task that comes to certain objects to trigger an event.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}