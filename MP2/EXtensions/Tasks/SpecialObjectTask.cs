using MP2.EXtensions.Global;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using Message = DreamPoeBot.Loki.Bot.Message;
using DreamPoeBot.Loki.Game.NativeWrappers;
using Microsoft.VisualBasic.Logging;
using DreamPoeBot.Loki.Components;
using System.Drawing;
using System.Xml.Linq;
using static DreamPoeBot.Loki.Elements.GrandHeistContractElement;
using static MP2.EXtensions.Tasks.KillBossTask;
using DreamPoeBot.Loki.FilesInMemory;
using MP2.EXtensions.Positions;

namespace MP2.EXtensions.Tasks
{
    public class SpecialObjectTask : ITask
    {
        private const int MaxInteractionAttempts = 30;

        private static readonly Interval TickInterval = new Interval(200);

        private static List<CachedObject> Objects = new List<CachedObject>();
        private static List<CachedPosition> MiniMapPositions = new List<CachedPosition>();

        private static bool _enabled;
        private static CachedObject _current;
        private static CachedPosition _currentP;
        private static Func<Task> _postInteraction;

        public async Task<bool> Run()
        {
            if (MapExplorationTask.MapCompleted || !World.CurrentArea.IsMap)
                return false;

            if (Mp2Settings.Instance.SimulacrumBot) return false;

            if (Objects.Count == 0)
            {
                return false;
            }

            GlobalLog.Debug("SpecialObjectTask");

            if (MapExplorationTask.CompletionPointReached)
            {
                if (MiniMapPositions.Count == 0) return false;
                foreach (CachedPosition sObj in MiniMapPositions)
                {
                    if (sObj == null) continue;
                    if(sObj.Ignored) continue;
                    _currentP = sObj;
                    var pos = sObj.Position;
                    if(pos == null) continue;
                    if (sObj.Position.IsFar)
                    {
                        if (!pos.TryCome())
                        {
                            GlobalLog.Error($"[SpecialObjectTask] Fail to move to {pos}. Marking this special object as unwalkable.");
                            _currentP.Unwalkable = true;
                            _currentP = null;
                        }
                        return true;
                    }
                    else if (sObj.Position.IsNear)
                    {
                        await Wait.Sleep(2200);
                        var obj = _currentP.Object;
                        if (obj == null)
                        {
                            sObj.Ignored = true;
                            continue;
                        }
                        if (_currentP.IsMonster)
                        {
                            if (obj.IsTargetable)
                            {
                                var name = _currentP.Position.Name;
                                var attempts = ++_currentP.InteractionAttempts;

                                if (attempts > MaxInteractionAttempts)
                                {
                                    GlobalLog.Error($"[SpecialObjectTask1] All attempts to interact with \"{name}\" have been spent. Now ignoring it.");

                                    sObj.Ignored = true;
                                    _currentP = null;
                                    return true;
                                }

                                var routine = RoutineManager.Current;

                                routine.Message(new Message("SetLeash", this, 60));

                                var res = await routine.Logic(new Logic("hook_combat", this));
                                GlobalLog.Debug("SpecialMonsterCombat: " + (res == LogicResult.Provided));
                                await Wait.SleepSafe(500);
                                return true;
                            }
                            else
                            {
                                var attempts = ++_currentP.InteractionAttempts;
                                if (attempts > MaxInteractionAttempts)
                                {
                                    GlobalLog.Error($"[SpecialObjectTask2] All attempts to interact have been spent. Now ignoring it.");

                                    sObj.Ignored = true;
                                    _currentP = null;
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            if (obj.IsTargetable)
                            {
                                var name = _currentP.Position.Name;

                                var attempts = ++_currentP.InteractionAttempts;

                                if (attempts > MaxInteractionAttempts)
                                {
                                    GlobalLog.Error($"[SpecialObjectTask4] All attempts to interact with \"{name}\" have been spent. Now ignoring it.");

                                    sObj.Ignored = true;
                                    _currentP = null;
                                    return true;
                                }
                                if (obj.Metadata == "Metadata/Terrain/Maps/Augury/Objects/DoorLever" || obj.Name == "Switch")
                                {
                                    if (await PlayerAction.Interact(obj))
                                    {
                                        await Wait.Sleep(8000);
                                        GlobalLog.Debug("Waiting...");
                                        if (await Wait.For(() => !obj.Fresh().Components.StateMachineComponent.StageStates.FirstOrDefault(x => x.Name == "activate")?.IsActive == true, $"[InteractWithObject] {_current.Position} interaction", 100, 9000))
                                        {
                                            GlobalLog.Debug("Ignored");
                                            sObj.Ignored = true;
                                        }
                                        else
                                        {
                                            GlobalLog.Debug("Returning True");
                                            return true;
                                        }
                                    }

                                }
                                else
                                {
                                    if (await PlayerAction.Interact(obj, () => !obj.Fresh().IsTargetable, $"\"{name}\" interaction", 6000))
                                    {
                                        await Wait.Sleep(1000);
                                        if (_postInteraction != null)
                                            await _postInteraction();
                                    }
                                    else
                                    {
                                        await Wait.SleepSafe(500);
                                    }
                                    return true;
                                }
                            }
                            else
                            {
                                var attempts = ++sObj.InteractionAttempts;
                                if (attempts > MaxInteractionAttempts)
                                {
                                    GlobalLog.Error($"[SpecialObjectTask5] All attempts to interact have been spent. Now ignoring it.");

                                    sObj.Ignored = true;
                                    _currentP = null;
                                    return true;
                                }
                            }
                        }
                        return true;
                    }
                }
            }

            if (Objects.Count == 0) return false;

            foreach (CachedObject sObj in Objects)
            {
                if (sObj == null) continue;
                if (sObj.Ignored) continue;

                /*if(sObj.Ig)
                {
                    var monster = LokiPoe.ObjectManager.GetObjectsByType<DreamPoeBot.Loki.Game.Objects.Monster>().Where(s => s.Id == sObj.Id).First();
                    if(monster != null)
                    {
                        if(!monster.IsValid || monster.IsDead)
                        {
                            continue;
                        }
                    }
                }*/

                _current = sObj;
                GlobalLog.Debug("Aqui33 :");

                var pos = _current.Position;
                if (pos == null) continue;

                /*if (_current.Position.Distance > 45)
                {
                    GlobalLog.Debug("Aqui35 :");
                    await Coroutines.FinishCurrentMoveAction();
                    if (!pos.TryCome())
                    {
                        GlobalLog.Error($"[SpecialObjectTask] Fail to move to {pos}. Marking this special object as unwalkable.");
                        _current.Unwalkable = true;
                        _current = null;
                    }
                    return true;
                }*/
                if (_current.Position.Distance > 25 && _current.Position.Distance <= 45)
                {
                    GlobalLog.Debug("Aqui35 :");
                    while(_current.Position.Distance > 25)
                    {
                        await Coroutines.FinishCurrentMoveAction();
                        if (!pos.TryCome())
                        {
                            GlobalLog.Error($"[SpecialObjectTask] Fail to move to {pos}. Marking this special object as unwalkable.");
                            _current.Unwalkable = true;
                            sObj.Ignored = true;
                            return true;
                        }
                    }
                }

                if (_current.Position.Distance <= 25)
                {
                    GlobalLog.Debug("Aqui37 :");
                    _current = sObj;
                    var obj = _current.Object;
                    if (obj == null)
                    {
                        _current.Ignored = true;
                        continue;
                    }
                    GlobalLog.Debug("Aqui33 Name:" + _current.Object?.Name);
                    if (_current.IsMonster)
                    {
                        if (obj.IsTargetable)
                        {
                            var name = _current.Position.Name;
                            var attempts = ++_current.InteractionAttempts;
                            if (attempts > MaxInteractionAttempts)
                            {
                                GlobalLog.Error($"[SpecialObjectTask1] All attempts to interact with \"{name}\" have been spent. Now ignoring it.");
                                _current.Ignored = true;
                                return false;
                            }

                            var routine = RoutineManager.Current;

                            routine.Message(new Message("SetLeash", this, 60));

                            var res = await routine.Logic(new Logic("hook_combat", this));
                            GlobalLog.Debug("SpecialMonsterCombat: " + (res == LogicResult.Provided));
                            return true;
                        }
                        else
                        {
                            var attempts = ++_current.InteractionAttempts;
                            if (attempts > MaxInteractionAttempts)
                            {
                                GlobalLog.Error($"[SpecialObjectTask2] All attempts to interact have been spent. Now ignoring it.");
                                _current.Ignored = true;
                                //Objects.RemoveAll(m => m.Object?.Name == sObj.Object?.Name);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (obj.IsTargetable)
                        {
                            var name = _current.Position.Name;

                            var attempts = ++_current.InteractionAttempts;

                            if (attempts > MaxInteractionAttempts)
                            {
                                GlobalLog.Error($"[SpecialObjectTask4] All attempts to interact with \"{name}\" have been spent. Now ignoring it.");
                                _current.Ignored = true;
                                _current = null;
                                return true;
                            }
                            if (obj.Metadata == "Metadata/Terrain/Maps/Augury/Objects/DoorLever" || obj.Name == "Switch")
                            {
                                if (await PlayerAction.Interact(obj))
                                {
                                    await Wait.Sleep(8000);
                                    GlobalLog.Debug("Waiting...");
                                    if (await Wait.For(() => !obj.Fresh().Components.StateMachineComponent.StageStates.FirstOrDefault(x => x.Name == "activate")?.IsActive == true, $"[InteractWithObject] {_current.Position} interaction", 100, 9000))
                                    {
                                        GlobalLog.Debug("Ignored");
                                        _current.Ignored = true;
                                    }
                                    else
                                    {
                                        GlobalLog.Debug("Returning True");
                                        return true;
                                    }
                                }

                            }
                            else
                            {
                                if (await PlayerAction.Interact(obj, () => !obj.Fresh().IsTargetable, $"\"{name}\" interaction", 6000))
                                {
                                    await Wait.Sleep(1000);
                                    if (_postInteraction != null)
                                        await _postInteraction();
                                }
                                else
                                {
                                    await Wait.SleepSafe(500);
                                }
                                if (await PlayerAction.Interact(obj, () => !obj.Fresh().IsTargetable, $"\"{name}\" interaction", 6000))
                                {
                                    await Wait.Sleep(1000);
                                    if (_postInteraction != null)
                                        await _postInteraction();
                                }
                                else
                                {
                                    await Wait.SleepSafe(500);
                                }
                                return true;
                            }
                        }
                        else
                        {
                            var attempts = ++sObj.InteractionAttempts;
                            if (attempts > MaxInteractionAttempts)
                            {
                                GlobalLog.Error($"[SpecialObjectTask5] All attempts to interact have been spent. Now ignoring it.");

                                sObj.Ignored = true;
                                _current.Ignored = true;
                                _current = null;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static void OnMapCompletePointReached()
        {
            foreach(CachedObject s in Objects)
            {

            }
            Blacklist.Clear();
            Objects.Clear();
            MiniMapPositions.Clear();
            HandleTick();
        }

        public static void HandleTick()
        {
            if (MapExplorationTask.MapCompleted)
                return;

            if (!TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            if (LokiPoe.InstanceInfo.MinimapIcons == null) return;

            _enabled = true;

            if (MapExplorationTask.CompletionPointReached)
            {
                foreach (MinimapIconWrapper icon in LokiPoe.InstanceInfo.MinimapIcons)
                {
                    if (icon == null) continue;
                    
                    if (icon.MinimapIcon.Name.Contains("Checkpoint", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Shrine", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Expedition", StringComparison.OrdinalIgnoreCase)) continue; ;
                    if (icon.MinimapIcon.Name.Contains("Entrance", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Checkpoint", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Ritual", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Item", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Affliction", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Loot", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name == "Portal" || icon.MinimapIcon.Name == "LootFilterMediumWhiteCircle") continue;
                    if (icon.MinimapIcon.Name.Contains("Breach", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("NPC")) continue;
                    GlobalLog.Debug("Trying to cache " + icon.MinimapIcon.Name);
                    if (icon.MinimapIcon.Name.Contains("Beacon", StringComparison.OrdinalIgnoreCase) && Mp2Settings.Instance.OpenOnlyThatMap)
                    {
                        continue;
                    }
                    var cached = MiniMapPositions?.Find(s => s.Id == icon.ObjectId);
                    if (cached == null)
                    {
                        if (icon.MinimapIcon.Name.Contains("Rare") || icon.MinimapIcon.Name.Contains("Unique"))
                        {
                            var pos = new WalkablePosition("", icon.LastSeenPosition);
                            if (pos != null)
                            {
                                if (!KillBossTask.CachedBosses.Contains(new KillBossTask.CachedBoss(icon.ObjectId, pos, false)))
                                    KillBossTask.CachedBosses.Add(new KillBossTask.CachedBoss(icon.ObjectId, pos, false));
                            }
                            MiniMapPositions?.Add(new CachedPosition(icon.ObjectId, icon.LastSeenPosition, true));
                            
                        }
                        else
                        {
                            MiniMapPositions?.Add(new CachedPosition(icon.ObjectId, icon.LastSeenPosition, false));
                        }
                    }
                }
            }
 
            foreach (MinimapIconWrapper icon in LokiPoe.InstanceInfo.MinimapIcons)
            {
                if (icon == null) continue;
                if (icon.NetworkObject == null) continue;
                if (icon.MinimapIcon.Name == "Portal" || icon.MinimapIcon.Name == "LootFilterMediumWhiteCircle") continue;
                if (icon.MinimapIcon.Name.Contains("Shrine")) continue;
                if (icon.MinimapIcon.Name.Contains("Expedition")) continue; ;
                if (icon.MinimapIcon.Name.Contains("Entrance")) continue;
                if (icon.MinimapIcon.Name.Contains("Checkpoint")) continue;
                if (icon.MinimapIcon.Name.Contains("Boss")) continue;
                if (icon.MinimapIcon.Name.Contains("Ritual")) continue;
                if (icon.MinimapIcon.Name.Contains("Item")) continue;
                if (icon.MinimapIcon.Name.Contains("Loot")) continue;
                if (icon.MinimapIcon.Name.Contains("NPC")) continue;
                if (icon.MinimapIcon.Name.Contains("Affliction", StringComparison.OrdinalIgnoreCase)) continue;
                if (icon.MinimapIcon.Name.Contains("Breach", StringComparison.OrdinalIgnoreCase)) continue;
                if (icon.MinimapIcon.Name.Contains("Beacon") && Mp2Settings.Instance.OpenOnlyThatMap)
                {
                    continue;
                }

                var cached = Objects?.Find(s => s.Id == icon.ObjectId);
                GlobalLog.Debug("Trying to cache " + icon.MinimapIcon.Name);
                if (cached == null)
                {
                    GlobalLog.Debug("Caching " + icon.MinimapIcon.Name);
                    var pos = icon.NetworkObject.WalkablePosition(5, 20);

                    if (icon.MinimapIcon.Name.Contains("Rare") || icon.MinimapIcon.Name.Contains("Unique"))
                    {
                        GlobalLog.Debug("Aqui28 :" + icon.MinimapIcon.Name);
                        if (icon.NetworkObject.IsTargetable)
                        {
                            if (MapExplorationTask.CompletionPointReached && !MapExplorationTask.MapCompleted)
                                if (!KillBossTask.CachedBosses.Contains(new KillBossTask.CachedBoss(icon.NetworkObject.Id, pos, false)))
                                    KillBossTask.CachedBosses.Add(new KillBossTask.CachedBoss(icon.NetworkObject.Id, pos, false));

                            GlobalLog.Debug("Cached :" + icon.MinimapIcon.Name);
                            Objects?.Add(new CachedObject(icon.NetworkObject.Id, pos, true));
                        }
                    }
                    else
                    {
                        if (icon.NetworkObject.IsTargetable)
                        {
                            GlobalLog.Debug("Cached :" + icon.MinimapIcon.Name);
                            Objects?.Add(new CachedObject(icon.NetworkObject.Id, pos, false));
                        }
                    }
                }
                else
                {
                    if (!icon.MinimapIcon.Name.Contains("Lever") && !icon.MinimapIcon.Name.Contains("Switch")) continue;
                    if (cached.Object == null) continue;
                    if (cached.Ignored && cached.Object.IsTargetable)
                        cached.Ignored = false;
                }
            }

            foreach (var obj in LokiPoe.ObjectManager.Objects)
            {
                if (!SpecialObjectMetadata.Contains(obj.Metadata))
                    continue;

                var id = obj.Id;
                var cached = Objects?.Find(s => s.Id == id);

                if (obj.IsTargetable)
                {
                    if (cached == null)
                    {
                        var pos = obj.WalkablePosition(5, 20);
                        Objects?.Add(new CachedObject(obj.Id, pos));
                        GlobalLog.Debug($"[SpecialObjectTask] Registering {pos}");
                    }
                }
                else
                {
                    if (cached != null)
                    {
                        if (cached == _current) _current = null;
                        Objects?.Remove(cached);
                    }
                }
            }
        }

        public void Tick()
        {
            if (MapExplorationTask.MapCompleted)
                return;

            if (!TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            if (MapExplorationTask.CompletionPointReached)
            {
                foreach (MinimapIconWrapper icon in LokiPoe.InstanceInfo.MinimapIcons)
                {
                    if (icon == null) continue;
                    if (icon.MinimapIcon.Name.Contains("Checkpoint", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Shrine", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Expedition", StringComparison.OrdinalIgnoreCase)) continue; ;
                    if (icon.MinimapIcon.Name.Contains("Entrance", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Checkpoint", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Ritual", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Affliction", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Item", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("Loot", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name.Contains("NPC")) continue;
                    if (icon.MinimapIcon.Name.Contains("Breach", StringComparison.OrdinalIgnoreCase)) continue;
                    if (icon.MinimapIcon.Name == "Portal" || icon.MinimapIcon.Name == "LootFilterMediumWhiteCircle") continue;

                    if (icon.MinimapIcon.Name.Contains("Beacon", StringComparison.OrdinalIgnoreCase) && Mp2Settings.Instance.OpenOnlyThatMap)
                    {
                        continue;
                    }
                    var cached = MiniMapPositions?.Find(s => s.Id == icon.ObjectId);
                    if (cached == null)
                    {
                        if (icon.MinimapIcon.Name.Contains("Rare") || icon.MinimapIcon.Name.Contains("Unique"))
                        {
                            var pos = new WalkablePosition("", icon.LastSeenPosition);
                            if (pos != null)
                            {
                                if (!KillBossTask.CachedBosses.Contains(new KillBossTask.CachedBoss(icon.ObjectId, pos, false)))
                                    KillBossTask.CachedBosses.Add(new KillBossTask.CachedBoss(icon.ObjectId, pos, false));
                            }
                            MiniMapPositions?.Add(new CachedPosition(icon.ObjectId, icon.LastSeenPosition, true));
                        }
                        else
                        {
                            MiniMapPositions?.Add(new CachedPosition(icon.ObjectId, icon.LastSeenPosition, false));
                        }
                    }
                }
            }

            _enabled = true;
            if (LokiPoe.InstanceInfo.MinimapIcons == null) return;
            foreach (MinimapIconWrapper icon in LokiPoe.InstanceInfo.MinimapIcons)
            {
                if (icon == null) continue;
                if (icon.NetworkObject == null) continue;
                if (icon.MinimapIcon.Name == "Portal" || icon.MinimapIcon.Name == "LootFilterMediumWhiteCircle") continue;
                if (icon.MinimapIcon.Name.Contains("Shrine")) continue;
                if (icon.MinimapIcon.Name.Contains("Expedition")) continue;;
                if (icon.MinimapIcon.Name.Contains("Entrance")) continue;
                if (icon.MinimapIcon.Name.Contains("Checkpoint")) continue;
                if (icon.MinimapIcon.Name.Contains("Boss")) continue;
                if (icon.MinimapIcon.Name.Contains("Ritual")) continue;
                if (icon.MinimapIcon.Name.Contains("Item")) continue;
                if (icon.MinimapIcon.Name.Contains("NPC")) continue;
                if (icon.MinimapIcon.Name.Contains("Affliction")) continue;
                if (icon.MinimapIcon.Name.Contains("Loot")) continue;
                if (icon.MinimapIcon.Name.Contains("Breach", StringComparison.OrdinalIgnoreCase)) continue;
                if (icon.MinimapIcon.Name.Contains("Beacon") && Mp2Settings.Instance.OpenOnlyThatMap)
                {
                    continue;
                }
                
                var cached = Objects?.Find(s => s.Id == icon.ObjectId);
                if(cached == null)
                {
                    var pos = icon.NetworkObject.WalkablePosition(5, 20);

                    if (icon.MinimapIcon.Name.Contains("Rare") || icon.MinimapIcon.Name.Contains("Unique"))
                    {
                        GlobalLog.Debug("Aqui28 :" + icon.MinimapIcon.Name);
                        if(icon.NetworkObject.IsTargetable) 
                        {
                            if(MapExplorationTask.CompletionPointReached && !MapExplorationTask.MapCompleted)
                                if (!KillBossTask.CachedBosses.Contains(new KillBossTask.CachedBoss(icon.ObjectId, pos, false)))
                                    KillBossTask.CachedBosses.Add(new KillBossTask.CachedBoss(icon.ObjectId, pos, false));

                            GlobalLog.Debug("Cached :" + icon.MinimapIcon.Name);
                            Objects?.Add(new CachedObject(icon.ObjectId, pos, true));
                        }
                    }
                    else
                    {
                        if (icon.NetworkObject.IsTargetable)
                        {
                            GlobalLog.Debug("Cached :" + icon.MinimapIcon.Name);
                            Objects?.Add(new CachedObject(icon.ObjectId, pos, false));
                        }
                    }
                }
                else
                {
                    if (!icon.MinimapIcon.Name.Contains("Lever") && !icon.MinimapIcon.Name.Contains("Switch")) continue;
                    if (cached.Object == null) continue;
                    if (cached.Ignored && cached.Object.IsTargetable)
                        cached.Ignored = false;
                }
            }

            foreach (var obj in LokiPoe.ObjectManager.Objects)
            {
                if (!SpecialObjectMetadata.Contains(obj.Metadata))
                    continue;

                var id = obj.Id;
                var cached = Objects?.Find(s => s.Id == id);

                if (obj.IsTargetable)
                {
                    if (cached == null)
                    {
                        var pos = obj.WalkablePosition(5, 20);
                        Objects?.Add(new CachedObject(obj.Id, pos));
                        GlobalLog.Debug($"[SpecialObjectTask] Registering {pos}");
                    }
                }
                else
                {
                    if (cached != null)
                    {
                        if (cached == _current) _current = null;
                        Objects?.Remove(cached);
                    }
                }
            }
        }

        private static void Reset(string areaName)
        {
            _enabled = false;
            _current = null;
            _postInteraction = null;
            Objects.Clear();
        }

        private static readonly HashSet<string> SpecialObjectMetadata = new HashSet<string>
        {
            // Zana quest objects
            "Metadata/Effects/Environment/artifacts/Gaius/ObjectiveTablet",
            "Metadata/Effects/Environment/artifacts/Gaius/TimeTablet",

            // Desert - Storm-Weathered Chest
            "Metadata/Terrain/EndGame/MapDesert/Objects/MummyEventChest",

            // Waste Pool - Valve
            "Metadata/Monsters/Doedre/DoedreSewer/DoedreCauldronValve",

            // Whakawairua Tuahu - Ancient Seal
            "Metadata/Terrain/EndGame/MapShipGraveyardCagan/Objects/IncaReleaseBall",

            // Olmec's Sanctum - Silver Monkey body parts
            "Metadata/Terrain/EndGame/MapIncaUniqueLegends/Objects/LegendsGlyph1",
            "Metadata/Terrain/EndGame/MapIncaUniqueLegends/Objects/LegendsGlyph2",
            "Metadata/Terrain/EndGame/MapIncaUniqueLegends/Objects/LegendsGlyph3",
            "Metadata/Terrain/EndGame/MapIncaUniqueLegends/Objects/LegendsGlyph4",
            "Metadata/Terrain/EndGame/MapIncaUniqueLegends/Objects/LegendsGlyphMain",

            // Mao Kun - Fairgraves
            "Metadata/Terrain/EndGame/MapTreasureIsland/Objects/FairgravesTreasureIsland",

            //POE2
            "Metadata/MiscellaneousObjects/Endgame/TowerCompletion",
            "Metadata/Terrain/Maps/Crypt/Objects/CryptSecretDoorSwitch"
        };

        internal static void ToggleRhoaNests(bool enable)
        {
            if (enable)
            {
                SpecialObjectMetadata.Add("Metadata/Terrain/EndGame/MapSaltFlats/Objects/AngeredBird");
                SpecialObjectMetadata.Add("Metadata/Terrain/EndGame/MapSwampFetid/Objects/AngeredBird");
            }
            else
            {
                SpecialObjectMetadata.Remove("Metadata/Terrain/EndGame/MapSaltFlats/Objects/AngeredBird");
                SpecialObjectMetadata.Remove("Metadata/Terrain/EndGame/MapSwampFetid/Objects/AngeredBird");
            }
        }

        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == MP2.Messages.NewMapEntered)
            {
                GlobalLog.Info("[SpecialObjectTask] Reset.");

                Reset(message.GetInput<string>());

                if (_enabled)
                    GlobalLog.Info("[SpecialObjectTask] Enabled.");

                return MessageResult.Processed;
            }
            if (id == ComplexExplorer.LocalTransitionEnteredMessage)
            {
                GlobalLog.Info("[SpecialObjectTask] Resetting unwalkable flags.");
                foreach (var speacialObj in Objects)
                {
                    speacialObj.Unwalkable = false;
                }
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

        public string Name => "SpecialObjectTask";
        public string Description => "Task that handles objects specific to certain maps.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}
