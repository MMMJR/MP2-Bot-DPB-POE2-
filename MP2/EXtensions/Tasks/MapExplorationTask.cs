using MP2.EXtensions;
using MP2.EXtensions.Global;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using MP2.EXtensions.Mapper;
using Message = DreamPoeBot.Loki.Bot.Message;
using DreamPoeBot.Loki.Elements;

namespace MP2.EXtensions.Tasks
{
    public class MapExplorationTask : ITask
    {
        private static readonly Interval TickInterval = new Interval(100);
        private static readonly Interval TickInterval2 = new Interval(1800);

        private static bool _mapCompletionPointReached;
        private static bool _mapCompleted;
        private static bool _bossInTheEnd;

        public static bool MapCompleted
        {
            get => _mapCompleted;
            private set
            {
                _mapCompleted = value;
                if (value) TrackMobTask.RestrictRange();
            }
        }

        public static bool CompletionPointReached
        {
            get => _mapCompletionPointReached;
            set
            {
                _mapCompletionPointReached = value;
            }
        }

        public async Task<bool> Run()
        {
            if (MapCompleted || !World.CurrentArea.IsMap)
                return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            if (Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
            {
                if(!TickInterval2.Elapsed)
                    return true;
            }


            GlobalLog.Debug("Explorer: MapComplete: " + MapCompleted + " IsMap: " + World.CurrentArea.IsMap);

            

            return await CombatAreaCache.Current.Explorer.Execute();
        }

        public void Tick()
        {
            if (MapCompleted || !TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            var mapData = MapData.Current;
            if(mapData == null) return;
            var type = mapData.Type;

            if (!_mapCompletionPointReached)
            {
                if (CombatAreaCache.Current.Explorer.BasicExplorer.PercentComplete >= Mp2Settings.Instance.ExplorationPercent)
                {
                    GlobalLog.Warn($"[MapExplorationTask] Exploration limit has been reached ({Mp2Settings.Instance.ExplorationPercent}%)");
                    if (Mp2Settings.Instance.StrictExplorationPercent)
                    {
                        GlobalLog.Debug("[MapExplorationTask] Strict exploration percent is true. Map is complete.");
                        MapCompleted = true;
                        return;
                    }
                    if (type == MapType.Bossroom)
                    {
                        TrackMobTask.RestrictRange();
                        CombatAreaCache.Current.Explorer.Settings.FastTransition = true;
                    }
                    _mapCompletionPointReached = true;
                    SpecialObjectTask.OnMapCompletePointReached();
                }
            }

            if (!MapCompleted)
            {
                if (LokiPoe.InstanceInfo.IsMapCompleted)
                {
                    MapCompleted = true;
                    DeviceAreaTask._toMap = false;
                }
            }
        }

        private static void Reset(string areaName)
        {
            MapCompleted = false;
            _mapCompletionPointReached = false;
            _bossInTheEnd = false;
        }

        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == MP2.Messages.NewMapEntered)
            {
                GlobalLog.Info("[MapExplorationTask] Reset.");
                Reset(message.GetInput<string>());
                return MessageResult.Processed;
            }
            if (id == ComplexExplorer.LocalTransitionEnteredMessage)
            {
                return MessageResult.Processed;
            }
            if (id == Events.Messages.PlayerResurrected)
            {
                MapCompleted = true;
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

        public string Name => "MapExplorationTask";
        public string Description => "Task that handles map exploration.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}