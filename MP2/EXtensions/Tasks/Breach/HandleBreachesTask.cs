using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using log4net;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using MP2.EXtensions.Tasks.Breach;
using Message = DreamPoeBot.Loki.Bot.Message;
using System.Xml.Linq;

namespace MP2.EXtensions.Tasks.Breach
{
    public class HandleBreachesTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private bool _skip;

        // This needs to be static so we have persistent data to use. Alternatively, we could host it elsewhere and pass it to the task, but
        // for this plugin, we'll do the former.
        internal static readonly AreaDataManager<BreachData> BreachDataManager =
            new AreaDataManager<BreachData>(hash => new BreachData(hash)) { DebugLogging = true };

        private BreachCache _current;
        private int _moveErrors;

        /// <summary>The name of this task.</summary>
        public string Name => "HandleBreachesTask";

        /// <summary>A description of what this task does.</summary>
        public string Description => "This task will handle interacting with Breaches.";

        /// <summary>The author of this task.</summary>
        public string Author => "Bossland GmbH";

        /// <summary>The version of this task.</summary>
        public string Version => "0.0.1.1";

        bool ShouldActivate(BreachCache breach)
        {
            // Cache and reuse this for performance reasons.
            if (breach.Activate != null)
            {
                return breach.Activate.Value;
            }

            Log.DebugFormat("[HandleBreachesTask] The Breach [{0}] will be activated.", breach.Id);

            breach.Activate = true;

            return false;
        }

        /// <summary>
        /// Coroutine logic to execute.
        /// </summary>
        /// <returns>true if logic was executed to handle this type and false otherwise.</returns>
        public async Task<bool> Run()
        {
            // NOTE: This task's Run function is triggered from "hook_post_combat" Logic, as it's added via a secondary TaskManager!

            // If this task needs to be disabled due to errors, support doing so.
            if (_skip)
            {
                return false;
            }

            // Don't do anything in these cases.
            if (LokiPoe.Me.IsDead || LokiPoe.Me.IsInHideout || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInMapRoom)
                return false;

            GlobalLog.Debug("HandleBreachTask");

            var myPos = LokiPoe.MyPosition;

            var active = BreachDataManager.Active;
            if (active == null)
                return false;

            // Make sure the breach is still valid and not blacklisted if it's set.
            // We don't re-eval current against settings, because of the performance overhead.
            if (_current != null)
            {
                if (!_current.IsValid || Blacklist.Contains(_current.Id))
                {
                    _current = null;
                }
            }

            // Find the next best breach.
            if (_current == null)
            {
                _current =
                    active.Breaches.Where(m => m.IsValid && !Blacklist.Contains(m.Id) && ShouldActivate(m))
                        .OrderBy(m => m.Position.Distance(myPos))
                        .FirstOrDefault();
                _moveErrors = 0;
            }

            // Nothing to do if there's no breach.
            if (_current == null)
            {
                return false;
            }

            // If we can't move to the breach, blacklist it.
            if (_moveErrors > 5)
            {
                Blacklist.Add(_current.Id, TimeSpan.FromHours(1),
                    string.Format("[HandleBreachesTask::Logic] Unable to move to the Breach."));
                _current = null;
                return true;
            }

            // If we are too far away to interact, move towards the object.
            if (myPos.Distance(_current.WalkablePosition) > 50)
            {
                // Make sure nothing is in the way.
                await Coroutines.CloseBlockingWindows();

                // Try to move towards the location.
                if (!PlayerMoverManager.MoveTowards(_current.WalkablePosition))
                {
                    Log.ErrorFormat("[HandleBreachesTask::Logic] PlayerMoverManager.MoveTowards({0}) failed for Breach [{1}].",
                        _current.WalkablePosition, _current.Id);
                    _moveErrors++;
                    return true;
                }

                _moveErrors = 0;

                return true;
            }

            // Make sure we're not doing anything before we interact.
            await Coroutines.FinishCurrentAction();

            // Now process the object, but make sure it exists.
            var breach = _current.NetworkObject;
            if (breach == null)
            {
                _current.Activate = false;
                Log.ErrorFormat("[HandleBreachesTask::Logic] The NetworkObject does not exist for the Breach [{0}] yet.", _current.Id);
                _current = null;
                return true;
            }

            // Try to move towards the location.
            if (!PlayerMoverManager.MoveTowards(_current.WalkablePosition))
            {
                Log.ErrorFormat("[HandleBreachesTask::Logic] PlayerMoverManager.MoveTowards({0}) failed for Breach [{1}].",
                    _current.WalkablePosition, _current.Id);
                _moveErrors++;
                return true;
            }
            if(Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
            {
                if (await PlayerAction.Interact(breach, () => !breach.Fresh().IsTargetable, $"\"{breach.Name}\" interaction", 6000))
                {
                    await Wait.Sleep(1000);
                }
                else
                {
                    await Wait.SleepSafe(500);
                }
                if (await PlayerAction.Interact(breach, () => !breach.Fresh().IsTargetable, $"\"{breach.Name}\" interaction", 6000))
                {
                    await Wait.Sleep(1000);
                }
                else
                {
                    await Wait.SleepSafe(500);
                }
            }    
            return true;
        }

        /// <summary>The bot Start event.</summary>
        public void Start()
        {
            _skip = false;
            _current = null; // Force clear, in case settings changed.

            BreachDataManager.Start(); // Will check IsInGame as needed
        }

        /// <summary>The bot Tick event.</summary>
        public void Tick()
        {
            BreachDataManager.Tick(); // Will check IsInGame as needed
        }

        /// <summary>The bot Stop event.</summary>
        public void Stop()
        {
            BreachDataManager.Stop(); // Will check IsInGame as needed
        }

        #region Implementation of IMessageHandler

        /// <summary>
        /// Implements logic to handle a message passed through the system.
        /// </summary>
        /// <param name="message">The message to be processed.</param>
        /// <returns>A tuple of a MessageResult and object.</returns>
        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        #endregion

        #region Implementation of ILogicProvider

        /// <summary>
        /// Implements the ability to handle a logic passed through the system.
        /// </summary>
        /// <param name="logic">The logic to be processed.</param>
        /// <returns>A LogicResult that describes the result..</returns>
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        #endregion
    }
}