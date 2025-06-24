using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using MP2.EXtensions;
using log4net;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    class PreCombatFollowTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        private int FollowFailCounter = 0;

        public string Name { get { return "PreCombatFollowTask"; } }
        public string Description { get { return "This task will keep the bot under a specific distance from the leader, in combat situation."; } }
        public string Author { get { return "NotYourFriend, origial code from Unknown"; } }
        public string Version { get { return "0.0.0.1"; } }


        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInHideout)
            {
                return false;
            }

            KeyManager.ClearAllKeyStates();
            return false;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
    }
}
