using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using log4net;
using MP2.Class;
using MP2.EXtensions.Global;
using MP2.EXtensions;
using System.Diagnostics;
using Message = DreamPoeBot.Loki.Bot.Message;
using UserControl = System.Windows.Controls.UserControl;
using MP2.EXtensions.Tasks;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Mapper;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using MP2.EXtensions.Tasks.Breach;
using DreamPoeBot.Loki;

namespace MP2
{
	public class MP2 : IBot//NOTE: you can also manually inherit from IStartStopEvents if you need to override Start/Stop event handlers.
	{
		private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        internal static bool IsOnRun;

        private Mp2Gui _gui;
        private Coroutine _coroutine;
        private readonly TaskManager _taskManager = new TaskManager();

        private OverlayWindow _overlay = new OverlayWindow(LokiPoe.ClientWindowHandle);
        private ChatParser _chatParser = new ChatParser();
        private Stopwatch _chatSw = Stopwatch.StartNew();
        private Stopwatch _Security = Stopwatch.StartNew();

        public static bool Debug = true;

        internal static bool IsInteracting;

        public UserControl Control => _gui ??= new Mp2Gui();
		public JsonSettings Settings => Mp2Settings.Instance;

        public TaskManager GetTaskManager()
        {
            return _taskManager;
        }

        private void BotManagerOnOnBotChanged(object sender, BotChangedEventArgs botChangedEventArgs)
        {
            if (botChangedEventArgs.New == this)
            {
                ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            }
        }

        public async void Initialize()
        {
            BotManager.OnBotChanged += BotManagerOnOnBotChanged;
            GameOverlay.TimerService.EnableHighPrecisionTimers();
            _overlay.Start();
        }

        public void Deinitialize()
        {
            BotManager.OnBotChanged -= BotManagerOnOnBotChanged;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await _taskManager.ProvideLogic(TaskGroup.Enabled, RunBehavior.UntilHandled, logic);
        }

        public void Start()
        {
            ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            Explorer.CurrentDelegate = user => CombatAreaCache.Current.Explorer.BasicExplorer;
            ComplexExplorer.ResetSettingsProviders();
            ComplexExplorer.AddSettingsProvider("MP2", MapBotExploration, ProviderPriority.High);

            // Cache all bound keys.
            LokiPoe.Input.Binding.Update();

            // Reset the default MsBetweenTicks on start.
            Log.Debug($"[Start] MsBetweenTicks: {BotManager.MsBetweenTicks}.");
            Log.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");

            // Since this bot will be performing client actions, we need to enable the process hook manager.
            LokiPoe.ProcessHookManager.Enable();

            OpenChestTask.chestAtt = 0;

            _coroutine = null;

            ExilePather.Reload();

            _taskManager.Reset();

            AddTasks();

            Events.Start();
            PluginManager.Start();
            PlayerMoverManager.Start();
            RoutineManager.Start();
            _taskManager.Start();
            SimulacrumTask.Reset();
            SimulacrumTask.LoadAreas();
            IsOnRun = false;
            IsInteracting = false;
            DeviceAreaTask._toMap = false;

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                Log.Debug($"[Start] The plugin {plugin.Name} is enabled.");
            }

            Log.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");
            Task.Run(async () =>
            {
                if (await VerifySuApiClient.VerifyUserKey())
                {
                    var DaysLeft = await VerifySuApiClient.VerifySu.GetKeyExpirationDate(Mp2Settings.Instance.UserKey);
                    if (DaysLeft == null) DaysLeft = "1";
                    Mp2Settings.Instance.DaysLeft = DaysLeft;
                }
            });
        }

        public void Stop()
        {
            _taskManager.Stop();
            PluginManager.Stop();
            PlayerMoverManager.Stop();
            RoutineManager.Stop();

            // When the bot is stopped, we want to remove the process hook manager.
            LokiPoe.ProcessHookManager.Disable();

            MP2.IsOnRun = false;

            // Cleanup the coroutine.
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }
        }

        public void Tick()
        {
            if (_coroutine == null)
            {
                _coroutine = new Coroutine(() => MainCoroutine());
            }

            ExilePather.Reload();

            Events.Tick();
            CombatAreaCache.Tick();
            _taskManager.Tick();
            
            PluginManager.Tick();
            RoutineManager.Tick();
            PlayerMoverManager.Tick();
            StuckDetection.Tick();


            if (_chatSw.ElapsedMilliseconds > 250)
            {
                _chatParser.Update();
                _chatSw.Restart();
            }
            // Check to see if the coroutine is finished. If it is, stop the bot.
            if (_coroutine.IsFinished)
            {
                Log.Debug($"The bot coroutine has finished in a state of {_coroutine.Status}");
                BotManager.Stop();
                return;
            }

            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }

        private async Task MainCoroutine()
        {
            while (true)
            {
                if (LokiPoe.IsInLoginScreen)
                {
                    // Offload auto login logic to a plugin.
                    var logic = new Logic("hook_login_screen", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                    }
                }
                else if (LokiPoe.IsInCharacterSelectionScreen)
                {
                    // Offload character selection logic to a plugin.
                    var logic = new Logic("hook_character_selection", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                    }
                }
                else if (LokiPoe.IsInGame)
                {
                    // To make things consistent, we once again allow user coorutine logic to preempt the bot base coroutine logic.
                    // This was supported to a degree in 2.6, and in general with our bot bases. Technically, this probably should
                    // be at the top of the while loop, but since the bot bases offload two sets of logic to plugins this way, this
                    // hook is being placed here.
                    var hooked = false;
                    var logic = new Logic("hook_ingame", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                        {
                            hooked = true;
                            break;
                        }
                    }

                    if (!hooked)
                    {
                        // Wait for game pause
                        if (LokiPoe.InstanceInfo.IsGamePaused)
                        {
                            Log.Debug("Waiting for game pause");
                        }
                        // Resurrect character if it is dead
                        else if (LokiPoe.Me.IsDead)
                        {
                            await ResurrectionLogic.Execute();
                        }
                        // What the bot does now is up to the registered tasks.
                        else
                        {
                            await _taskManager.Run(TaskGroup.Enabled, RunBehavior.UntilHandled);
                        }
                    }
                }
                else
                {
                    // Most likely in a loading screen, which will cause us to block on the executor, 
                    // but just in case we hit something else that would cause us to execute...
                    await Coroutine.Sleep(1000);
                    continue;
                }

                // End of the tick.
                await Coroutine.Yield();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void AddTasks()
        {
            _taskManager.Add(new ClearCursorTask());
            _taskManager.Add(new AssignMoveSkillTask());
            _taskManager.Add(new LeaveAreaTask());
            _taskManager.Add(new HandleBlockingChestsTask());
            _taskManager.Add(new HandleBlockingObjectTask());
            _taskManager.Add(new FlaskTask(0));
            _taskManager.Add(new CombatTask(45));
            _taskManager.Add(new PostCombatHookTask());
            _taskManager.Add(new FlaskTask(1));
            _taskManager.Add(new HandleBreachesTask());
            _taskManager.Add(new KillBossTask());
            _taskManager.Add(new SpecialObjectTask());
            _taskManager.Add(new ProximityTriggerTask());
            _taskManager.Add(new LootItemTask());
            _taskManager.Add(new DeviceAreaTask());
            _taskManager.Add(new OpenChestTask());
            _taskManager.Add(new CombatTask(-1));
            _taskManager.Add(new IdTask());
            _taskManager.Add(new StashTask());
            _taskManager.Add(new SimulacrumTask());
            _taskManager.Add(new SortInventoryTask());
            _taskManager.Add(new TravelToHideoutTask());
            _taskManager.Add(new TakeMapTask());
            _taskManager.Add(new DeviceAreaTask());
            _taskManager.Add(new OpenMapTask());
            _taskManager.Add(new TrackMobTask());
            _taskManager.Add(new MapExplorationTask());
            _taskManager.Add(new FinishMapTask());
            _taskManager.Add(new FallbackTask());
        }

        private static ExplorationSettings MapBotExploration()
        {
            if (!World.CurrentArea.IsMap)
                return new ExplorationSettings();

            OnNewMapEnter();


            return new ExplorationSettings(tileSeenRadius: TileSeenRadius);
        }

        private static void OnNewMapEnter()
        {
            var areaName = World.CurrentArea.Name;
            Log.Info($"[Mp2Bot] New map has been entered: {areaName}.");

            bool triggerRestrict = false;

            if (areaName == "Sinking Spire") triggerRestrict = true;
            if (areaName == "Sump") triggerRestrict = true;
            if (areaName == "Blooming Field") triggerRestrict = true;
            if (areaName == "Vaal City") triggerRestrict = true;
            if (areaName == "Hidden Grotto") triggerRestrict = true;
            if (areaName == "Augury") triggerRestrict = true;
            if (areaName == "Vaal Foundry") triggerRestrict = true;
            if (areaName == "Vaal Factory") triggerRestrict = true;
            if (areaName.Contains("Hideout", StringComparison.OrdinalIgnoreCase)) triggerRestrict = true;
            if (areaName == "Vaal Temple") triggerRestrict = true;
            if (areaName.Contains("Citadel", StringComparison.OrdinalIgnoreCase)) triggerRestrict = true;

            if (triggerRestrict)
                TrackMobTask.RestrictRange();
            else
                TrackMobTask.RemoreRestrict();

            if (areaName == "Crypt" || triggerRestrict)
            {
                Mp2Settings.Instance.ChestOpenRange = 0;
                Mp2Settings.Instance.StrongboxOpenRange = 0;
                Mp2Settings.Instance.ShrineOpenRange = 0;
            }
            else
            {
                Mp2Settings.Instance.ChestOpenRange = 30;
                Mp2Settings.Instance.StrongboxOpenRange = 30;
                Mp2Settings.Instance.ShrineOpenRange = 30;
            }
            if (Mp2Settings.Instance.SimulacrumBot)
                SimulacrumTask.WaveCount = 1;

            IsOnRun = true;
            MapData.ResetCurrent();
            OpenChestTask.chestAtt = 0;
            Utility.BroadcastMessage(null, Messages.NewMapEntered, areaName);
        }

        private static int TileSeenRadius
        {
            get
            {
                if (TileSeenDict.TryGetValue(World.CurrentArea.Name, out int radius))
                    return radius;

                return ExplorationSettings.DefaultTileSeenRadius;
            }
        }

        private static readonly Dictionary<string, int> TileSeenDict = new Dictionary<string, int>
        {

        };

        public static class Messages
        {
            public const string NewMapEntered = "MB_new_map_entered_event";
            public const string MapFinished = "MB_map_finished_event";
            public const string GetIsOnRun = "MB_get_is_on_run";
            public const string SetIsOnRun = "MB_set_is_on_run";
            public const string GetMapSettings = "MB_get_map_settings";
        }

        public MessageResult Message(Message message)
        {
            var handled = false;
            var id = message.Id;

            if (id == BotStructure.GetTaskManagerMessage)
            {
                message.AddOutput(this, _taskManager);
                handled = true;
            }
            else if (id == Messages.GetIsOnRun)
            {
                message.AddOutput(this, IsOnRun);
                handled = true;
            }
            else if (id == Messages.SetIsOnRun)
            {
                var value = message.GetInput<bool>();
                IsOnRun = value;
                handled = true;
            }
            else if (message.Id == Events.Messages.AreaChanged)
            {
                if (World.CurrentArea.IsHideoutArea || World.CurrentArea.IsTown)
                    MP2.IsOnRun = false;

                handled = true;
            }
            else if (id == Messages.GetMapSettings)
            {
                message.AddOutput(this, JObject.FromObject(Mp2Settings.Instance.MapDict));
                handled = true;
            }

            Events.FireEventsFromMessage(message);

            var res = _taskManager.SendMessage(TaskGroup.Enabled, message);
            if (res == MessageResult.Processed)
                handled = true;

            return handled ? MessageResult.Processed : MessageResult.Unprocessed;
        }

        /// <summary>
		/// author of this project
		/// </summary>
		public string Author => "MMMJR";

        /// <summary>
        /// Description of this project
        /// </summary>
        public string Description => "";

        /// <summary>
        /// Name of this project
        /// </summary>
        public string Name => "Mp2";

        /// <summary>
        /// Version of this project
        /// </summary>
        public string Version => "0.0.0.1";
    }
}