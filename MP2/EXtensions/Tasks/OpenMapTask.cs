using MP2.EXtensions;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Mapper;
using DreamPoeBot.Common;
using Message = DreamPoeBot.Loki.Bot.Message;
using DreamPoeBot.Loki.Elements;
using DreamPoeBot.Loki.Coroutine;
using System.Diagnostics;
using DreamPoeBot.Framework;
using MP2.Class;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace MP2.EXtensions.Tasks
{
    public class OpenMapTask : ITask
    {
        internal static bool Enabled;

        private static int noMapsCount = 0;

        public static List<Vector2i> cachedCitadels = new List<Vector2i>();
        public static AtlasPanel2.AtlasNode CurrentTower = null;

        public async Task<bool> Run()
        {
            if (!Enabled && !DeviceAreaTask._toMap)
                return false;

            var area = World.CurrentArea;

            GlobalLog.Debug("OpenMap");

            if (area.IsHideoutArea)
                goto inProperArea;

            if (area.IsMapRoom)
            {
                if (await DeviceAreaTask.HandleStairs(true))
                    return true;

                goto inProperArea;
            }

            if (area.Name == "The Ziggurat Refuge")
            {
                goto inProperArea;
            }

            return false;

            inProperArea:
            Item map;
            if (Mp2Settings.Instance.SimulacrumBot)
            {
                map = Inventories.InventoryItems.Find(i => i.Name == "Simulacrum");
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] There is no map in inventory.");
                    Enabled = false;
                    return true;
                }
            }
            else if (Mp2Settings.Instance.BreachRunner && Mp2Settings.Instance.RunBreachStone)
            {
                map = Inventories.InventoryItems.Find(i => i.Metadata == "Metadata/Items/MapFragments/CurrencyBreachFragment");
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] There is no map in inventory.");
                    Enabled = false;
                    return true;
                }
            }
            else
            {
                map = Inventories.InventoryItems.Find(i => i.IsMap());
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] There is no map in inventory.");
                    Enabled = false;
                    return true;
                }
            }
            
            var mapPos = map.LocationTopLeft;
            GlobalLog.Info("Aqui22");

            if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 4, 5000))
            {
                GlobalLog.Info("Aqui25");
                await Coroutines.CloseBlockingWindows();
                ErrorManager.ReportError();
                return true;
            }
            await Wait.Sleep(3000);
            GlobalLog.Info("Aqui23");
            if (!LokiPoe.InGameState.AtlasUi.IsOpened) return true;
            
            List<AtlasPanel2.AtlasNode> selectedNodes = new List<AtlasPanel2.AtlasNode>();
            AtlasPanel2.AtlasNode selectedNode = null;
            if (Mp2Settings.Instance.CitadelFinder)
            {
                AtlasPanel2.AtlasNode CurNode = GetCurrentAtlasNode();
                if (CurNode == null)
                {
                    GlobalLog.Error("Current Atlas node is null reference");
                    Mp2Settings.Instance.CitadelFinder = false;
                    Mp2Settings.Instance.AtlasExplorationEnabled = true;
                    await Coroutines.CloseBlockingWindows();
                    return true;
                }
                selectedNodes = CitadelPathFinder.FindPathToClosestCitadel(CurNode);
                if (selectedNodes == null)
                {
                    GlobalLog.Error("Selected Atlas nodes is null reference");
                    Mp2Settings.Instance.CitadelFinder = false;
                    Mp2Settings.Instance.AtlasExplorationEnabled = true;
                    await Coroutines.CloseBlockingWindows();
                    return true;
                }
                foreach (var n in selectedNodes)
                {
                    GlobalLog.Info("Path Area Name:" + n.Area.Name);
                    GlobalLog.Info("Path X:" + n.Coordinate.X);
                    GlobalLog.Info("Path Y:" + n.Coordinate.Y);
                    GlobalLog.Info("Path Complete:" + n.IsCompleted);
                    if (n == null) continue;
                    if (n.IsCompleted) continue;
                    selectedNode = n;
                    break;
                }
                if (selectedNode == null)
                {
                    GlobalLog.Error("Selected Atlas node is null reference");
                    return true;
                }
                if (selectedNode.Area.Name.Contains("Citadel"))
                {
                    cachedCitadels.Add(selectedNode.Coordinate);
                    return true;
                }
                GlobalLog.Info(selectedNode.Area.Name);
                GlobalLog.Info(selectedNode.Coordinate.X);
                GlobalLog.Info(selectedNode.Coordinate.Y);
                GlobalLog.Info(selectedNode.IsCompleted);
            }
            else if (Mp2Settings.Instance.BreachRunner)
            {
                if(Mp2Settings.Instance.RunBreachStone)
                {
                    GlobalLog.Info("Aqui24");
                    AtlasUi.MoveToNode(new Vector2i(1, 0));
                    await Wait.Sleep(1000);
                    LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult OpenNodeResult = LokiPoe.InGameState.AtlasUi.OpenNodeInventory(LokiPoe.InGameState.AtlasUi.TheRealmgate);
                    await Wait.Sleep(350);
                    if (!await Wait.For(() => LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.IsOpened, "Node Opened", 100, 5000)) return true;

                    if (OpenNodeResult != LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult.None)
                    {
                        return true;
                    }

                    if (!await ClearDevice())
                    {
                        ErrorManager.ReportError();
                        return true;
                    }

                    if (!await PlayerAction.TryTo(() => PlaceIntoDevice(mapPos), "Place map into device", 3))
                    {
                        ErrorManager.ReportError();
                        return true;
                    }
                    await Wait.Sleep(350);
                    goto OpenSimulacrum;
                }
                else if (Mp2Settings.Instance.SetupBreachTower && CurrentTower == null)
                {
                    var towers = TowerRunner.FindTowers();
                    if (towers != null)
                    {
                        foreach (var tower in towers)
                        {
                            bool TResult = LokiPoe.InGameState.AtlasUi.MoveToNode(tower, 7000);
                            if (TResult)
                            {
                                Item tablet;
                                tablet = Inventories.InventoryItems.Find(i => i.Metadata == "Metadata/Items/TowerAugment/RitualAugment");
                                if (tablet == null)
                                {
                                    Mp2Settings.Instance.SetupBreachTower = false;
                                    GlobalLog.Error("[OpenMapTask] There is no tablet in inventory.");
                                    continue;
                                }
                                await Wait.Sleep(2000);
                                LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult OpenNodeResult = LokiPoe.InGameState.AtlasUi.OpenNodeInventory(tower);
                                await Wait.Sleep(350);
                                if (!await Wait.For(() => LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.IsOpened, "Node Opened", 100, 5000)) return true;

                                if (OpenNodeResult != LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult.None)
                                {
                                    return true;
                                }

                                if (!await ClearDevice())
                                {
                                    ErrorManager.ReportError();
                                    return true;
                                }

                                if (!await PlayerAction.TryTo(() => PlaceIntoDevice(tablet.LocationTopLeft), "Place tablet into device", 3))
                                {
                                    Mp2Settings.Instance.SetupBreachTower = false;
                                    ErrorManager.ReportError();
                                    return true;
                                }
                                await Wait.Sleep(350);
                                if (!await PlayerAction.TryTo(ActivateDevice, "Activate Tablet", 6000))
                                {
                                    Mp2Settings.Instance.SetupBreachTower = false;
                                    ErrorManager.ReportError();
                                    return true;
                                }
                                await Wait.SleepSafe(1800);
                            }
                        }
                        CurrentTower = towers[0];
                    }
                }
                if (CurrentTower != null)
                {
                    selectedNodes = AtlasUi.UsableAtlasNodes.Where(s => !s.IsCompleted && s.Coordinate.Distance(CurrentTower.Coordinate) < 12 && !s.IsTower && s.Influence.Count > 0 && s.Content.Find(c => c.Name.Contains("Breach", StringComparison.OrdinalIgnoreCase)) != null).ToList();

                    if (selectedNodes.Count == 0)
                    {
                        CurrentTower = null;
                        Mp2Settings.Instance.SetupBreachTower = true;
                        GlobalLog.Error("No maps to do in towers.");
                        return true;
                    }
                }
                else
                {
                    selectedNodes = AtlasUi.UsableAtlasNodes.Where(s => !s.IsCompleted && !s.IsTower && s.Content.Find(c => c.Name.Contains("Breach", StringComparison.OrdinalIgnoreCase)) != null).ToList();

                    if (selectedNodes.Count == 0)
                    {
                        Mp2Settings.Instance.SetupBreachTower = false;
                        Mp2Settings.Instance.BreachRunner = false;
                        Mp2Settings.Instance.AtlasExplorationEnabled = true;
                        GlobalLog.Error("No maps to do with breachs.");
                        return true;
                    }
                }
                int rNodeIndex = LokiPoe.Random.Next(0, (selectedNodes.Count - 1));
                selectedNode = selectedNodes[rNodeIndex];
            }
            else if (Mp2Settings.Instance.SimulacrumBot)
            {
                /*selectedNode = LokiPoe.InGameState.AtlasUi.AtlasNodes.Find(s => s.Area.Name == "The Ziggurat Refuge");
                if (selectedNode == null)
                {
                    selectedNode = LokiPoe.InGameState.AtlasUi.AtlasNodes.Find(s => s.Coordinate.X == 0);
                }
                if(selectedNode != null)
                {
                    LokiPoe.InGameState.AtlasUi.MoveToNode(selectedNode, 7000);
                }*/
                AtlasUi.MoveToNode(new Vector2i(1, 0));
                await Wait.Sleep(1000);
                LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult OpenNodeResult = LokiPoe.InGameState.AtlasUi.OpenNodeInventory(LokiPoe.InGameState.AtlasUi.TheRealmgate);
                await Wait.Sleep(350);
                if (!await Wait.For(() => LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.IsOpened, "Node Opened", 100, 5000)) return true;

                if (OpenNodeResult != LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult.None)
                {
                    return true;
                }

                if (!await ClearDevice())
                {
                    ErrorManager.ReportError();
                    return true;
                }

                if (!await PlayerAction.TryTo(() => PlaceIntoDevice(mapPos), "Place map into device", 3))
                {
                    ErrorManager.ReportError();
                    return true;
                }
                await Wait.Sleep(350);
                goto OpenSimulacrum;

            }
            else if (Mp2Settings.Instance.AtlasExplorationEnabled)
            {
                if (LokiPoe.InGameState.AtlasUi.UsableAtlasNodes.Count == 0) return true;

                foreach (AtlasPanel2.AtlasNode n in LokiPoe.InGameState.AtlasUi.UsableAtlasNodes)
                {
                    if (n.IsCompleted) continue;
                    if (Mp2Settings.Instance.OpenOnlyThatMap)
                    {
                        if (n.Area.Name == "Bluff" && n.IsTower)
                            selectedNodes.Add(n);
                    }
                    else
                    {
                        //if (n.Area.Name == "Sinking Spire") continue;
                        //if (n.Area.Name == "Sump") continue;
                        //blooming field
                        //if (n.Area.Name == "Vaal City") continue;
                        //if (n.Area.Name == "Hidden Grotto") continue;
                        //if (n.Area.Name == "Vaal Foundry") continue;
                        if (n.Area.Name == "Vaal Factory") continue;
                        if (n.Area.Name.Contains("Hideout", StringComparison.OrdinalIgnoreCase)) continue;
                        //if (n.Area.Name == "Vaal Temple") continue;
                        if (n.Area.Name.Contains("Citadel", StringComparison.OrdinalIgnoreCase)) continue;
                        selectedNodes.Add(n);
                    }
                }
                if (selectedNodes.Count == 0)
                {
                    GlobalLog.Error("No maps to do.");

                    await Coroutines.CloseBlockingWindows();

                    if (noMapsCount > 5)
                        BotManager.Stop();

                    noMapsCount++;

                    return true;
                }
                noMapsCount = 0;
                int rNodeIndex = LokiPoe.Random.Next(0, (selectedNodes.Count - 1));
                selectedNode = selectedNodes[rNodeIndex];
            }
            else
            {
                GlobalLog.Error("Atlas Settings Error! Pleask go to Atlas explore settings.");
                BotManager.Stop();
                return true;
            }

            if (selectedNode == null) return true;

            bool MResult = LokiPoe.InGameState.AtlasUi.MoveToNode(selectedNode, 9000);
            if (MResult)
            {
                await Wait.Sleep(2000);
                

                while (!LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.IsOpened)
                {
                    LokiPoe.InGameState.AtlasUi.OpenNodeInventoryResult OpenNodeResult = LokiPoe.InGameState.AtlasUi.OpenNodeInventory(selectedNode);
                    //await Wait.Sleep(550);
                    await Wait.For(() => LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.IsOpened, "Node Opened", 100, 3000);
                }

                if (!await ClearDevice())
                {
                    ErrorManager.ReportError();
                    return true;
                }

                if (!await PlayerAction.TryTo(() => PlaceIntoDevice(mapPos), "Place map into device", 3))
                {
                    ErrorManager.ReportError();
                    return true;
                }
                await Wait.Sleep(350);
            }
            else
            {
                await Coroutines.CloseBlockingWindows();
                return true;
            }

            OpenSimulacrum:

            if (!await PlayerAction.TryTo(ActivateDevice, "Activate Map Device", 6000))
            {
                ErrorManager.ReportError();
                return true;
            }
            DeviceAreaTask._toMap = true;
            await Wait.SleepSafe(2900);
            var portal = LokiPoe.ObjectManager.Objects.Closest<Portal>();
            if (portal == null)
            {
                GlobalLog.Error("[OpenMapTask] Unknown error. Fail to find any portal near map device.");
                ErrorManager.ReportError();
                return true;
            }

            var isTargetable = portal.IsTargetable;


            /*if (isTargetable)
            {
                if (!await Wait.For(() => !portal.Fresh().IsTargetable, "old map portals despawning", 200, 300))
                {
                    ErrorManager.ReportError();
                    return true;
                }
            }
            if (!await Wait.For(() =>
                {
                    var p = portal.Fresh();
                    return p.IsTargetable;
                },
                "new map portals spawning", 500, 6000))
            {
                ErrorManager.ReportError();
                return true;
            }*/

            //await Wait.SleepSafe(500);
            if(portal.IsTargetable)
                if (!await TakeMapPortal(portal))
                    ErrorManager.ReportError();

            return true;
        }

        private static async Task<bool> OpenDevice()
        {
            if (MapDevice.IsOpen) return true;

            var device = LokiPoe.ObjectManager.MapDevice;
            if (device == null)
            {
                if (World.CurrentArea.IsHideoutArea)
                {
                    GlobalLog.Error("[OpenMapTask] Fail to find Map Device in hideout.");
                }
                else
                {
                    GlobalLog.Error("[OpenMapTask] Unknown error. Fail to find Map Device in Refuge.");
                }
                GlobalLog.Error("[OpenMapTask] Now stopping the bot because it cannot continue.");
                //BotManager.Stop();
                return false;
            }
            GlobalLog.Debug("[OpenMapTask] Now going to open Map Device.");
            await Coroutines.CloseBlockingWindows();
            device.WalkablePosition().TryCome();
            await Coroutines.FinishCurrentMoveAction();

            if (await PlayerAction.Interact(device, () => LokiPoe.InGameState.AtlasUi.IsOpened, "Map Device opening", 6000))
            {
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully opened.");
                return true;
            }
            return false;
        }

        public static AtlasPanel2.AtlasNode GetCurrentAtlasNode()
        {
            var curNode = AtlasUi.AtlasNodes.Find(n => n.Coordinate == AtlasUi.CurrenctAtlasNodeCoordinates);
            if(curNode == null) return AtlasUi.CurrentAtlasNode;
            //return AtlasUi.CurrentAtlasNode;
            return curNode;
        }

        private static async Task<bool> ClearDevice()
        {
            var itemPositions = MapDevice.InventoryControl.Inventory.Items.Select(i => i.LocationTopLeft).ToList();
            if (itemPositions.Count == 0)
                return true;

            GlobalLog.Error("[OpenMapTask] Map Device is not empty. Now going to clean it.");

            foreach (var itemPos in itemPositions)
            {
                if (!await PlayerAction.TryTo(() => FastMoveFromDevice(itemPos), null, 2))
                    return false;
            }
            GlobalLog.Debug("[OpenMapTask] Map Device has been successfully cleaned.");
            return true;
        }

        private static async Task<bool> PlaceIntoDevice(Vector2i itemPos)
        {
            var oldCount = MapDevice.InventoryControl.Inventory.Items.Count;

            if (!await Inventories.FastMoveFromInventory(itemPos))
                return false;

            if (!await Wait.For(() => MapDevice.InventoryControl.Inventory.Items.Count == oldCount + 1, "item amount change in Map Device"))
                return false;

            return true;
        }

        private static async Task<bool> ActivateDevice()
        {
            GlobalLog.Debug("[OpenMapTask] Now going to activate the Map Device.");

            await Wait.SleepSafe(500); // Additional delay to ensure Activate button is targetable

            if(Mp2Settings.Instance.SimulacrumBot)
            {
                var map = MapDevice.InventoryControl.Inventory.Items.Find(i => i.Name == "Simulacrum");
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] Unexpected error. There is no map in the Map Device.");
                    return false;
                }
            }
            else if(Mp2Settings.Instance.RunBreachStone && Mp2Settings.Instance.BreachRunner)
            {
                var map = MapDevice.InventoryControl.Inventory.Items.Find(i => i.Metadata == "Metadata/Items/MapFragments/CurrencyBreachFragment");
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] Unexpected error. There is no map in the Map Device.");
                    return false;
                }
            }
            else if (Mp2Settings.Instance.RunBreachStone && Mp2Settings.Instance.SetupBreachTower)
            {
                var map = MapDevice.InventoryControl.Inventory.Items.Find(i => i.Metadata == "Metadata/Items/TowerAugment/RitualAugment");
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] Unexpected error. There is no map in the Map Device.");
                    return false;
                }
            }
            else
            {
                var map = MapDevice.InventoryControl.Inventory.Items.Find(i => i.Class == ItemClasses.Map);
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] Unexpected error. There is no map in the Map Device.");
                    return false;
                }
            }    
            

            LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.TraverseResult activated;

            activated = LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.Traverse();

            await Wait.LatencySleep();
            await Wait.SleepSafe(1000);

            if (activated != LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.TraverseResult.None)
            {
                GlobalLog.Error($"[OpenMapTask] Fail to activate the Map Device. Error: \"{activated}\".");
                return false;
            }
            else
            {
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully activated.");
                DeviceAreaTask._toMap = true;
                return true;
            }
        }

        private static async Task<bool> FastMoveFromDevice(Vector2i itemPos)
        {
            var item = MapDevice.InventoryControl.Inventory.FindItemByPos(itemPos);
            if (item == null)
            {
                GlobalLog.Error($"[FastMoveFromDevice] Fail to find item at {itemPos} in Map Device.");
                return false;
            }

            var itemName = item.FullName;

            GlobalLog.Debug($"[FastMoveFromDevice] Fast moving \"{itemName}\" at {itemPos} from Map Device.");

            var moved = MapDevice.InventoryControl.FastMove(item.LocalId);
            if (moved != FastMoveResult.None)
            {
                GlobalLog.Error($"[FastMoveFromDevice] Fast move error: \"{moved}\".");
                return false;
            }
            if (await Wait.For(() => MapDevice.InventoryControl.Inventory.FindItemByPos(itemPos) == null, "fast move"))
            {
                GlobalLog.Debug($"[FastMoveFromDevice] \"{itemName}\" at {itemPos} has been successfully fast moved from Map Device.");
                return true;
            }
            GlobalLog.Error($"[FastMoveFromDevice] Fast move timeout for \"{itemName}\" at {itemPos} in Map Device.");
            return false;
        }

        private static async Task<bool> TakeMapPortal(Portal portal, int attempts = 3)
        {
            for (int i = 1; i <= attempts; ++i)
            {
                if (!LokiPoe.IsInGame || World.CurrentArea.IsMap)
                    return true;

                GlobalLog.Debug($"[OpenMapTask] Take portal to map attempt: {i}/{attempts}");

                if (await PlayerAction.TakePortal(portal))
                    return true;

                await Wait.SleepSafe(1000);
            }
            return false;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == MP2.Messages.NewMapEntered)
            {
                Enabled = false;
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        private static class MapDevice
        {
            public static bool IsOpen => LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.IsOpened;

            public static InventoryControlWrapper InventoryControl => LokiPoe.InGameState.AtlasUi.AtlasMapDeviceUi.InventoryControl;
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

        public string Name => "OpenMapTask";
        public string Description => "Task for opening maps via Map Device.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}