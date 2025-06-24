using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    public class CombatTask : ITask
    {
        private readonly int _leashRange;

        public CombatTask(int leashRange)
        {
            _leashRange = leashRange;
        }

        public async Task<bool> Run()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            if (LokiPoe.InGameState.CheckPointUi.IsOpened)
            {
                LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                await Coroutines.CloseBlockingWindows();
            }

            var routine = RoutineManager.Current;

            routine.Message(new Message("SetLeash", this, _leashRange));

            //var expeditionNpc = LokiPoe.ObjectManager.Objects.Find<>(s => s.Metadata == "");

            var res = await routine.Logic(new Logic("hook_combat", this));
            GlobalLog.Debug("ResCombat: " + (res == LogicResult.Provided));
            return res == LogicResult.Provided;
        }

        #region Unused interface methods

        public MessageResult Message(Message message)
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

        public string Name => "CombatTask (Leash " + _leashRange + ")";

        public string Description => "This task executes routine logic for combat.";

        public string Author => "NotYourFriend original from EXVault";

        public string Version => "1.0";

        #endregion
    }
}
