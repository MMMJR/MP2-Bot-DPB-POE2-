using MP2.EXtensions.Global;
using DreamPoeBot.Loki.Bot;
using MP2.EXtensions.Mapper;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    public class TrackMobTask : ITask
    {
        private const int RestrictedRange = 45;

        private static int _range = -1;

        public async Task<bool> Run()
        {
            if (!World.CurrentArea.IsMap)
                return false;

            if(!Mp2Settings.Instance.TrackMob) return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            GlobalLog.Debug("TrackMob");

            return await TrackMobLogic.Execute(_range);
        }

        internal static void RestrictRange()
        {
            GlobalLog.Info($"[TrackMobTask] Restricting monster tracking range to {RestrictedRange}");
            _range = RestrictedRange;
            TrackMobLogic.CurrentTarget = null;
        }

        internal static void RemoreRestrict()
        {
            GlobalLog.Info($"[TrackMobTask] Remove restrict");
            _range = -1;
            TrackMobLogic.CurrentTarget = null;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == MP2.Messages.NewMapEntered)
            {
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

        public string Name => "TrackMobTask";
        public string Description => "Task for tracking monsters.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}