using System.Diagnostics;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using MP2.EXtensions.Global;

namespace MP2.EXtensions.Tasks
{
    public class FallbackTask : ITask
    {
        private readonly Stopwatch _notificationSwStopwatch = Stopwatch.StartNew();
        public async Task<bool> Run()
        {
            if (_notificationSwStopwatch.ElapsedMilliseconds < 2000) return true;
            if (Mp2Settings.Instance.SimulacrumBot) return true;
            _notificationSwStopwatch.Restart();
            GlobalLog.Warn("[FallbackTask] The Fallback task is executing. The bot is IDLE.");
            await Wait.Sleep(200);
            return true;
        }

        #region Unused interface methods

        public MessageResult Message(DreamPoeBot.Loki.Bot.Message message)
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

        public string Name => "FallbackTask";
        public string Description => "This task is the last task executed. It should not execute.";
        public string Author => "NotYourFriend original from EXVault";
        public string Version => "1.0";

        #endregion
    }
}
