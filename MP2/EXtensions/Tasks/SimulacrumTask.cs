using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions;
using log4net;
using Message = DreamPoeBot.Loki.Bot.Message;
using MP2.EXtensions.Global;
using System.Xml.Linq;
using System.Windows.Media.Animation;
using System.Diagnostics;
using Newtonsoft.Json;
using MP2.EXtensions.Mapper;


namespace MP2.EXtensions.Tasks
{
    public enum SimulacrumBotState
    {
        Initiator = 0,
        Wave = 1,
        Loot = 2,
        Finish = 3,

    }
    class SimulacrumTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();

        public static SimulacrumBotState BotState;
        public static int WaveCount;
        public static bool Enabled;
        public static readonly string InitiatorMetadata = "Metadata/Terrain/Gallows/Leagues/Delirium/Act1Town/Objects/DeliriumnatorAct1";
        public static int _cooldown;
        public static List<SimulacrumArea> _areas = new List<SimulacrumArea>();

        public static readonly Stopwatch loootDelay = Stopwatch.StartNew();
        public static readonly Stopwatch FinishDelay = Stopwatch.StartNew();
        public static readonly Stopwatch sDelay = Stopwatch.StartNew();

        public string Name { get { return "Simulacrum Bot"; } }
        public string Description { get { return "."; } }
        public string Author { get { return "MMMJR"; } }
        public string Version { get { return "0.0.0.1"; } }

        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
            Enabled = false;
            WaveCount = 0;
            BotState = SimulacrumBotState.Initiator;
            _cooldown = 0;
        }

        public static void Reset()
        {
            Enabled = false;
            WaveCount = 0;
            BotState = SimulacrumBotState.Initiator;
            _cooldown = 0;
        }
        public void Stop()
        {
            loootDelay.Stop();
        }
        public void Tick()
        {
            if (!Mp2Settings.Instance.SimulacrumBot) return;
            if (!LokiPoe.IsInGame) return;
            if (!World.CurrentArea.IsMap) return;

            if (BotState == SimulacrumBotState.Finish) return;

            var portals = LokiPoe.ObjectManager.GetObjectsByType<Portal>();

            if (portals == null) return;
            GlobalLog.Debug("Portals Cached Simulacrum");
            foreach(var portal in portals)
            {
                ExilePather.PolyPathfinder.AddObstacle(portal.Position, 10);
                ExilePather.PolyPathfinder.UpdateObstacles();
                ExilePather.SignalObstacleUpdate();
            }
        }

        public static void LoadAreas()
        {
            if (_areas == null)
            {
                _areas = new List<SimulacrumArea>();
            }
            //layoult 1
            _areas.Add(new SimulacrumArea(670, 580));// 618, 559 603, 546
            _areas.Add(new SimulacrumArea(882, 631));// 641, 301
            _areas.Add(new SimulacrumArea(889, 866)); //862, 285
            _areas.Add(new SimulacrumArea(928, 1040)); // 1075, 240
            _areas.Add(new SimulacrumArea(786, 1007)); // 986, 380
            _areas.Add(new SimulacrumArea(599, 683));
            //layoult 2
            _areas.Add(new SimulacrumArea(769, 631)); //765, 616 754 611
            _areas.Add(new SimulacrumArea(768, 523));
            _areas.Add(new SimulacrumArea(513, 923)); // 513, 923
            _areas.Add(new SimulacrumArea(307, 937));
            _areas.Add(new SimulacrumArea(386, 799));
            _areas.Add(new SimulacrumArea(497, 681)); //
            _areas.Add(new SimulacrumArea(946, 658));
            //layoult 3
            _areas.Add(new SimulacrumArea(515, 795));
            _areas.Add(new SimulacrumArea(301, 758)); //746, 275
            _areas.Add(new SimulacrumArea(290, 525)); // 352, 240
            _areas.Add(new SimulacrumArea(243, 341)); // 757, 5680
            _areas.Add(new SimulacrumArea(280, 404));
            _areas.Add(new SimulacrumArea(459, 542));
            _areas.Add(new SimulacrumArea(622, 783));
            //layoult 4
            _areas.Add(new SimulacrumArea(603, 546));//603, 546
            _areas.Add(new SimulacrumArea(641, 301));
            _areas.Add(new SimulacrumArea(862, 285));
            _areas.Add(new SimulacrumArea(1075, 240));
            _areas.Add(new SimulacrumArea(888, 501));
            _areas.Add(new SimulacrumArea(746, 275));
            _areas.Add(new SimulacrumArea(526, 290));
            _areas.Add(new SimulacrumArea(352, 240));
            _areas.Add(new SimulacrumArea(386, 380));
            _areas.Add(new SimulacrumArea(506, 493));
            _areas.Add(new SimulacrumArea(757, 568)); //754, 557 769, 631 762 607
            _areas.Add(new SimulacrumArea(962, 532)); //979, 539 734, 906
            _areas.Add(new SimulacrumArea(734, 906));
            _areas.Add(new SimulacrumArea(598, 626));
            _areas.Add(new SimulacrumArea(859, 935));//859, 9351044, 951
            _areas.Add(new SimulacrumArea(1044, 951));
            _areas.Add(new SimulacrumArea(983, 817)); 
            _areas.Add(new SimulacrumArea(862, 682));
            _areas.Add(new SimulacrumArea(437, 517)); 
            _areas.Add(new SimulacrumArea(961, 512));
            _areas.Add(new SimulacrumArea(729, 940));

            _areas.Add(new SimulacrumArea(936, 647)); // 943, 304
            _areas.Add(new SimulacrumArea(931, 864));
            _areas.Add(new SimulacrumArea(939, 1086));
            _areas.Add(new SimulacrumArea(808, 985));
            _areas.Add(new SimulacrumArea(682, 869)); // 666, 438
            _areas.Add(new SimulacrumArea(666, 438)); 
            _areas.Add(new SimulacrumArea(534, 779));
            

            _areas.Add(new SimulacrumArea(926, 737)); // 943, 304
            _areas.Add(new SimulacrumArea(943, 304));
            _areas.Add(new SimulacrumArea(793, 403));
            _areas.Add(new SimulacrumArea(686, 497));
            _areas.Add(new SimulacrumArea(625, 780)); // 666, 438
            _areas.Add(new SimulacrumArea(667, 959)); 
            _areas.Add(new SimulacrumArea(547, 785));
            _areas.Add(new SimulacrumArea(546, 596));
            _areas.Add(new SimulacrumArea(238, 649));

            _areas.Add(new SimulacrumArea(246, 864)); // 666, 438
            _areas.Add(new SimulacrumArea(230, 1092));
            _areas.Add(new SimulacrumArea(370, 984));
            _areas.Add(new SimulacrumArea(494, 888));
            _areas.Add(new SimulacrumArea(504, 428));

            _areas.Add(new SimulacrumArea(533, 780)); // 666, 438
            _areas.Add(new SimulacrumArea(241, 736));
            _areas.Add(new SimulacrumArea(250, 518));
            _areas.Add(new SimulacrumArea(234, 291));
            _areas.Add(new SimulacrumArea(359, 403));
            _areas.Add(new SimulacrumArea(491, 499));
            _areas.Add(new SimulacrumArea(510, 951)); // 920, 642
            _areas.Add(new SimulacrumArea(632, 602));
            _areas.Add(new SimulacrumArea(920, 642));
            _areas.Add(new SimulacrumArea(546, 780)); // 807, 976
            _areas.Add(new SimulacrumArea(807, 976));
            _areas.Add(new SimulacrumArea(393, 983)); // 680, 857
            _areas.Add(new SimulacrumArea(680, 857));
            _areas.Add(new SimulacrumArea(666, 428));

            _areas.Add(new SimulacrumArea(822, 968));
            _areas.Add(new SimulacrumArea(643, 945)); // 807, 976
            _areas.Add(new SimulacrumArea(428, 658));
            _areas.Add(new SimulacrumArea(660, 947)); // 680, 857683, 868
            _areas.Add(new SimulacrumArea(534, 776));
            _areas.Add(new SimulacrumArea(359, 974));
            _areas.Add(new SimulacrumArea(677, 859));
            _areas.Add(new SimulacrumArea(387, 964));
            _areas.Add(new SimulacrumArea(514, 460));//959, 380
            _areas.Add(new SimulacrumArea(995, 357));// 402, 992
            _areas.Add(new SimulacrumArea(959, 380));
            _areas.Add(new SimulacrumArea(402, 992));//997, 354 930, 661 673, 886978, 385 993, 384
            _areas.Add(new SimulacrumArea(930, 661));
            _areas.Add(new SimulacrumArea(673, 886));
            _areas.Add(new SimulacrumArea(978, 385));
            _areas.Add(new SimulacrumArea(993, 384)); //964, 404
            _areas.Add(new SimulacrumArea(964, 404)); 
            _areas.Add(new SimulacrumArea(962, 346));
            //677, 859

        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || !World.CurrentArea.IsCombatArea)
                return false;

            if (!Mp2Settings.Instance.SimulacrumBot) return false;

            GlobalLog.Debug("BotState: " + BotState);
            if (LokiPoe.InstanceInfo.IsMapCompleted)
                BotState = SimulacrumBotState.Finish;

            if (BotState == SimulacrumBotState.Initiator)
            {
                if(!DeviceAreaTask._toMap) DeviceAreaTask._toMap = true;
                if (await HandleInitiator())
                {
                    if (_areas.Count > 0)
                    {
                        var area = _areas.ClosestSimulacrumPosition<SimulacrumArea>();
                        GlobalLog.Info("Areas");
                        if (area == null) return true;
                        GlobalLog.Info("AreaS X: " + area.Position.AsVector.X + " Y: " + area.Position.AsVector.Y + " Distance: " + area.Position.Distance);
                        await area.CheckAndMoveToCenter();
                    }
                    BotState = SimulacrumBotState.Wave;
                    loootDelay.Restart();
                    FinishDelay.Restart();
                    sDelay.Restart();
                }
                else return true;
            }
            if(BotState == SimulacrumBotState.Wave)
            {
                var obj = LokiPoe.ObjectManager.Objects.Find(s => s.Metadata == InitiatorMetadata && s.IsTargetable);
                if (obj != null && WaveCount < 15)
                {
                    BotState = SimulacrumBotState.Loot;
                    loootDelay.Restart();
                    return true;
                }
                GlobalLog.Info("Wave Count: " + WaveCount + " ElapsedMillis: " + FinishDelay.ElapsedMilliseconds);
                if(WaveCount >= 14)
                {
                    if (FinishDelay.ElapsedMilliseconds > 46000)
                    {
                        loootDelay.Restart();
                        FinishDelay.Restart();
                        BotState = SimulacrumBotState.Loot;
                        return true;
                    }
                }
                await HandleCombatTask();
                return true;
            }
            if(BotState == SimulacrumBotState.Loot)
            {
                if(loootDelay.ElapsedMilliseconds > 5800)
                {
                    if(WaveCount == 15)
                    {
                        BotState = SimulacrumBotState.Finish;
                    }
                    else
                    {
                        BotState = SimulacrumBotState.Initiator;
                    }
                }
                return true;
            }
            if(BotState == SimulacrumBotState.Finish)
            {
                GlobalLog.Warn("Simulacrum Complete");
                DeviceAreaTask._toMap = false;
                if (!await PlayerAction.TpToTown())
                {
                    ErrorManager.ReportError();
                    if (World.CurrentArea.IsTown || World.CurrentArea.IsHideoutArea)
                    {
                        MP2.IsOnRun = false;
                        Statistics.Instance.OnMapFinish();
                        Utility.BroadcastMessage(this, MP2.Messages.MapFinished);
                        DeviceAreaTask._toMap = false;
                        Reset();
                    }
                    return true;
                }

                MP2.IsOnRun = false;
                Statistics.Instance.OnMapFinish();
                Utility.BroadcastMessage(this, MP2.Messages.MapFinished);
                DeviceAreaTask._toMap = false;
                Reset();
                return true;
            }
            return false;
        }

        private static int NumberOfMobsNear(NetworkObject target, float distance, bool dead = false)
        {
            var mpos = target.Position;

            var mobPositions =
                LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(d => d.IsAliveHostile && d.Id != target.Id).Select(m => m.Position).ToList();
            if (!mobPositions.Any())
                return 0;

            var curCount = 0;

            foreach (var mobPosition in mobPositions)
            {
                if (mobPosition.Distance(mpos) < distance)
                {
                    curCount++;
                }
            }

            return curCount;
        }

        public static async Task<bool> HandleCombatTask()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            if (LokiPoe.InGameState.CheckPointUi.IsOpened)
            {
                LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                await Coroutines.CloseBlockingWindows();
            }

            var routine = RoutineManager.Current;

            routine.Message(new Message("SetLeash", null, 100));

            var res = await routine.Logic(new Logic("hook_combat", null));
            GlobalLog.Debug("ResCombat: " + (res == LogicResult.Provided));
            return res == LogicResult.Provided;
        }

        public static async Task<bool> HandleInitiator()
        {
            if (LokiPoe.InGameState.StashUi.IsOpened)
                await Coroutines.CloseBlockingWindows();

            var obj2 = LokiPoe.ObjectManager.GetObjectByType<AflictionInitiator>();
            if (obj2 != null)
            {
                WaveCount = obj2.CurrentWave;
                GlobalLog.Warn("WaveCount: " + WaveCount);
                if (WaveCount == 16)
                {
                    BotState = SimulacrumBotState.Finish;
                    return false;
                }
            }

            var obj = LokiPoe.ObjectManager.Objects.Find(s => s.Metadata == InitiatorMetadata && s.IsTargetable);
            if (obj == null) return true;

            var pos = obj.WalkablePosition(18, 10);
            if(pos == null) return false;

            if (pos.IsFar)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Error($"[SimulacrumBot] Fail to move to {pos}.");
                    await Wait.Sleep(100);
                    return false;
                }
                await Coroutines.FinishCurrentMoveAction();
                return false;
            }

            var obj3 = LokiPoe.ObjectManager.GetObjectByType<AflictionInitiator>();
            if (obj3 == null)
            {
                WaveCount++;
                if(WaveCount > 15) WaveCount = 15;
            }
            else
            {
                WaveCount = obj2.CurrentWave;
            }

            if (await PlayerAction.Interact(obj, () => !obj.Fresh().IsTargetable, $"\"{obj.Name}\" interaction", 300))
            {
            }
            
            return true;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == MP2.Messages.NewMapEntered)
            {
                GlobalLog.Info("[MapExplorationTask] Reset.");
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }
    }
}