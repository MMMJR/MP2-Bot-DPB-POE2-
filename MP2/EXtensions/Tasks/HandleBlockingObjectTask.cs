using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace MP2.EXtensions.Tasks
{
    public class HandleBlockingObjectTask : ITask
    {
        private const int MaxAttempts = 10;

        private static int _lastId;
        private static int _attempts;

        public async Task<bool> Run()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            Func<NetworkObject> getObj;
            if (AreaSpecificObjects.TryGetValue(World.CurrentArea.Id, out getObj))
            {
                var obj = getObj();
                if (obj != null)
                {
                    var name = obj.Name;
                    if (AttemptLimitReached(obj.Id, name))
                    {
                        await LeaveArea();
                        return true;
                    }
                    if (await PlayerAction.Interact(obj))
                    {
                        await Wait.LatencySleep();
                        await Wait.For(() => getObj() == null, "object interaction", 200, 2000);
                    }
                    else
                    {
                        await Wait.SleepSafe(500);
                    }
                    return true;
                }
            }

            var door = LokiPoe.ObjectManager.Objects.Closest<TriggerableBlockage>(IsClosedDoor);

            if (door == null)
                return false;

            if (AttemptLimitReached(door.Id, door.Name))
            {
                await LeaveArea();
                return true;
            }
            if (await PlayerAction.Interact(door))
            {
                await Wait.LatencySleep();
                await Wait.For(() => !door.IsTargetable || door.IsOpened, "door opening", 50, 300);
                return true;
            }
            await Wait.SleepSafe(300);
            return true;
        }

        private static async Task LeaveArea()
        {
            GlobalLog.Error("[HandleBlockingObjectTask] Fail to remove a blocking object. Now requesting a new instance.");

            PlayerAction.AbandonCurrentArea();

            if (!await PlayerAction.TpToTown())
                ErrorManager.ReportError();
        }

        private static bool AttemptLimitReached(int id, string name)
        {
            if (_lastId == id)
            {
                ++_attempts;
                if (_attempts > MaxAttempts)
                {
                    return true;
                }
                if (_attempts >= 2)
                {
                    GlobalLog.Error($"[HandleBlockingObjectTask] {_attempts}/{MaxAttempts} attempt to interact with \"{name}\" (id: {id})");
                }
            }
            else
            {
                _lastId = id;
                _attempts = 0;
            }
            return false;
        }

        private static bool IsClosedDoor(TriggerableBlockage d)
        {
            return d.IsTargetable && !d.IsOpened && d.Distance <= 15 &&
                   (d.Name == "Door" || d.Metadata == "Metadata/MiscellaneousObjects/Smashable" || d.Metadata.Contains("LabyrinthSmashableDoor"));
        }

        private static NetworkObject PitGate()
        {
            return LokiPoe.ObjectManager.Objects
                .Find(o => o.IsTargetable && o.Distance <= 15 && o.Metadata.Contains("PitGateTransition"));
        }

        private static NetworkObject BellyGate()
        {
            return LokiPoe.ObjectManager.Objects
                .Find(o => o.IsTargetable && o.Distance <= 15 && o.Metadata.Contains("BellyArenaTransition"));
        }

        private static NetworkObject VoltaicWorkshop()
        {
            return LokiPoe.ObjectManager.Objects
                .Find(o => o is AreaTransition && o.IsTargetable && o.Distance <= 15 && o.Name == "Voltaic Workshop");
        }

        private static bool PlayerHasQuestItem(string metadata)
        {
            return Inventories.InventoryItems.Exists(i => i.Class == ItemClasses.QuestItem && i.Metadata == metadata);
        }

        private static readonly Dictionary<string, Func<NetworkObject>> AreaSpecificObjects = new Dictionary<string, Func<NetworkObject>>
        {
            ["MapWorldsPit"] = PitGate,
            ["MapWorldsMalformation"] = BellyGate,
            ["MapWorldsCore"] = BellyGate,
            ["MapWorldsTribunal"] = BellyGate,
            ["MapWorldsSepulchre"] = BellyGate,
            ["MapWorldsOvergrownShrine"] = BellyGate,
            ["MapWorldsFactory"] = VoltaicWorkshop,

            // Keep legacy variants for a while
            ["MapAtlasPit"] = PitGate,
            ["MapAtlasMalformation"] = BellyGate,
            ["MapAtlasCore"] = BellyGate,
            ["MapAtlasOvergrownShrine"] = BellyGate,
            ["MapAtlasFactory"] = VoltaicWorkshop,
        };


        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                _lastId = 0;
                _attempts = 0;
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

        public void Tick()
        {
        }

        public void Stop()
        {
        }

        public string Name => "HandleBlockingObjectTask";
        public string Description => "Task that handles various blocking objects.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}