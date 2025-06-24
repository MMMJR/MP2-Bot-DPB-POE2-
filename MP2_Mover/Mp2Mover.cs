using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using log4net;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using Logger = DreamPoeBot.Loki.Common.Logger;
using Message = DreamPoeBot.Loki.Bot.Message;
using UserControl = System.Windows.Controls.UserControl;

namespace MP2
{
    public class Mp2Mover : IPlayerMover
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private Mp2MoverGui _gui;
        private bool _useForceAdjustments;
        private PathfindingCommand _cmd;
        private static TaskManager _botTaskManager;
        private readonly Stopwatch _pathRefreshSw = new Stopwatch();
        private static readonly Stopwatch _DasherW = new Stopwatch();
        private Vector2i _cachedPosition = Vector2i.Zero;
        private static readonly List<Vector2i> BlacklistedLocations = new List<Vector2i>();
        private static readonly Dictionary<Vector2, DateTime> DangerPositionsTimed = new Dictionary<Vector2, DateTime>();

        /// <summary>Initializes this object.</summary>
        public void Initialize()
        {

        }
        /// <summary>Deinitializes this object. This is called when the object is being unloaded from the bot.</summary>
        public void Deinitialize()
        {

        }
        public JsonSettings Settings
        {
            get { return Mp2MoverSettings.Instance; }
        }

        /// <summary> The plugin's settings control. This will be added to the DreamPoeBot Settings tab.</summary>
        public UserControl Control
        {
            get { return (_gui ?? (_gui = new Mp2MoverGui())); }
        }
        /// <summary> The mover start callback. Do any initialization here. </summary>
        public void Start()
        {

        }
        /// <summary> The mover tick callback. Do any update logic here. </summary>
        public void Tick()
        {
            if (!LokiPoe.IsInGame)
                return;

            var cwa = LokiPoe.CurrentWorldArea;

            if (cwa.IsCombatArea && Mp2MoverSettings.Instance.ForceAdjustCombatAreas || Mp2MoverSettings.Instance.ForcedAdjustmentAreas.Any(e => e.Value.Equals(cwa.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _useForceAdjustments = true;
            }
            else
            {
                _useForceAdjustments = false;
            }

            var now = DateTime.UtcNow;
            foreach (var key in DangerPositionsTimed.Keys.ToList())
            {
                if ((now - DangerPositionsTimed[key]).TotalSeconds > 10)
                {
                    DangerPositionsTimed.Remove(key);
                    ExilePather.PolyPathfinder.RemoveObstacle(key.ToVector2i());
                    Log.Info($"[Mp2Mover] Removed expired danger position: {key}");
                }
            }

            ClearBlacklistedLocations();
        }
        /// <summary> The mover stop callback. Do any pre-dispose cleanup here. </summary>
        public void Stop()
        {
        }
        /// <summary>
        /// Implements the ability to handle a logic passed through the system.
        /// </summary>
        /// <param name="logic">The logic to be processed.</param>
        /// <returns>A LogicResult that describes the result..</returns>
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }
        /// <summary>
        /// Implements logic to handle a message passed through the system.
        /// </summary>
        /// <param name="message">The message to be processed.</param>
        /// <returns>A tuple of a MessageResult and object.</returns>
        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == "Mp2Mover_add_danger_positions")
            {
                if (message.TryGetInput(0, out List<Vector2> dangerPositions))
                {
                    foreach (var position in dangerPositions)
                    {
                        if (!DangerPositionsTimed.ContainsKey(position))
                        {
                            DangerPositionsTimed[position] = DateTime.UtcNow;
                            ExilePather.PolyPathfinder.AddObstacle(position.ToVector2i(), 10f);
                            Log.Warn($"[Mp2Mover] Added danger position: {position}");
                        }
                    }
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }
        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Name + ": " + Description;
        }
        /// <summary>
        /// Returns the player mover's current PathfindingCommand being used.
        /// </summary>
        public PathfindingCommand CurrentCommand => _cmd;

        #region Override of IMover

        /// <summary>
        /// Attempts to move towards a position. This function will perform pathfinding logic and take into consideration move distance
        /// to try and smoothly move towards a point.
        /// </summary>
        /// <param name="position">The position to move towards.</param>
        /// <param name="user">A user object passed.</param>
        /// <returns>true if the position was moved towards, and false if there was a pathfinding error.</returns>
        public bool MoveTowards(Vector2i position, params dynamic[] user)
        {
            var myPosition = LokiPoe.MyPosition;

            if (_cmd == null || // No command yet
                _cmd.Path == null ||
                _cmd.EndPoint != position || // Moving to a new position
                LokiPoe.CurrentWorldArea.IsTown || // In town, always generate new paths
                (_pathRefreshSw.IsRunning && _pathRefreshSw.ElapsedMilliseconds > Mp2MoverSettings.Instance.PathRefreshRateMs) || // New paths on interval
                _cmd.Path.Count <= 2 || // Not enough points
                _cmd.Path.All(p => myPosition.Distance(p) > 7))
            // Try and find a better path to follow since we're off course
            {
                _cmd = new PathfindingCommand(myPosition, position, 3, Mp2MoverSettings.Instance.AvoidWallHugging);
                if (!ExilePather.FindPath(ref _cmd))
                {
                    _pathRefreshSw.Restart();
                    Log.ErrorFormat("[Alcor75PlayerMoverSettings.MoveTowards] ExilePather.FindPath failed from {0} to {1}.",
                        myPosition, position);
                    return false;
                }
                _pathRefreshSw.Restart();
                // Signal 'FindPath_Result' tp PatherExplorer.
                Utility.BroadcastMessage(null, "FindPath_Result", _cmd);
            }
            var canUseMoveSkill = true;

            var cwa = LokiPoe.CurrentWorldArea;
            var specialMoveRange = Mp2MoverSettings.Instance.MoveRange;

            while (_cmd.Path.Count > 1)
            {
                if (BlacklistedLocations.Contains(_cmd.Path[0]) ||
                    ExilePather.PathDistance(_cmd.Path[0], myPosition) < specialMoveRange)
                {
                    _cmd.Path.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
            var point = _cmd.Path[0];

            point += new Vector2i(LokiPoe.Random.Next(-2, 3), LokiPoe.Random.Next(-2, 3));

            if (_useForceAdjustments)
            {
                var negX = 0;
                var posX = 0;

                var tmp1 = point;
                var tmp2 = point;

                for (var i = 0; i < 10; i++)
                {
                    tmp1.X--;
                    if (!ExilePather.IsWalkable(tmp1))
                    {
                        negX++;
                    }

                    tmp2.X++;
                    if (!ExilePather.IsWalkable(tmp2))
                    {
                        posX++;
                    }
                }

                if (negX > 5 && posX == 0)
                {
                    point.X += 10;
                    Log.WarnFormat("[Alcor75PlayerMover.MoveTowards] X-Adjustments being made!");
                    _cmd.Path[0] = point;
                }
                else if (posX > 5 && negX == 0)
                {
                    point.X -= 10;
                    Log.WarnFormat("[Alcor75PlayerMover.MoveTowards] X-Adjustments being made!");
                    _cmd.Path[0] = point;
                }

                var negY = 0;
                var posY = 0;

                tmp1 = point;
                tmp2 = point;

                for (var i = 0; i < 10; i++)
                {
                    tmp1.Y--;
                    if (!ExilePather.IsWalkable(tmp1))
                    {
                        negY++;
                    }

                    tmp2.Y++;
                    if (!ExilePather.IsWalkable(tmp2))
                    {
                        posY++;
                    }
                }

                if (negY > 5 && posY == 0)
                {
                    point.Y += 10;
                    Log.WarnFormat("[Alcor75PlayerMover.MoveTowards] Y-Adjustments being made!");
                    _cmd.Path[0] = point;
                }
                else if (posY > 5 && negY == 0)
                {
                    point.Y -= 10;
                    Log.WarnFormat("[Alcor75PlayerMover.MoveTowards] Y-Adjustments being made!");
                    _cmd.Path[0] = point;
                }
            }

            //used to check if there are objects on path
            var pathCheck = ExilePather.Raycast(myPosition, point, out var hitPoint);
            if (!pathCheck)
            {
                BlacklistedLocations.Add(point);
                BlacklistedLocations.Add(hitPoint);
            }

            // Cache actual position, in case we have a stuck logic that can later use it...
            _cachedPosition = myPosition;

            return BasicMove(myPosition, point);
        }

        #endregion

        private static void AdjustMouseDirection(Vector2i targetPosition)
        {
            MouseManager.SetMousePos("Mp2Mover", targetPosition, false);
        }

        private static bool BasicMove(Vector2i myPosition, Vector2i point)
        {
            // Signal 'Next_Selected_Walk_Position' tp PatherExplorer.
            Utility.BroadcastMessage(null, "Next_Selected_Walk_Position", point);

            if (!_DasherW.IsRunning) _DasherW.Start();

            var move = LokiPoe.InGameState.SkillBarHud.Slot1;
            if (move == null)
            {
                Log.ErrorFormat("[Alcor75PlayerMover.MoveTowards] Please assign the \"Movess\" skill to your skillbar, do not use mouse button, use q,w,e,r,t!");

                BotManager.Stop();

                return false;
            }

            if (Mp2MoverSettings.Instance.UseDash && _DasherW.ElapsedMilliseconds >= Mp2MoverSettings.Instance.UseDashInterval)
            {
                if (LokiPoe.Me.HasCurrentAction && LokiPoe.Me.CurrentAction.Skill != null)
                {
                    // If the key is pressed, but next moving point is less that SingleUseDistance setted, we need to reset keys and press the move skill once.
                    if (myPosition.Distance(point) <= Mp2MoverSettings.Instance.SingleUseDistance)
                    {
                        LokiPoe.ProcessHookManager.ClearAllKeyStates();
                        AdjustMouseDirection(point);
                        LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                    }
                    // Otherwise we just move the mouse to the next moving position, for a fast and smooth natural movement.
                    else
                    {
                        MouseManager.SetMousePos("Alcor75PlayerMoverSettings.MoveTowards", point, false);
                    }
                }
                else
                {

                    // If the key was not pressed, we clear all keys state (we newer know what keys other component might have pressed but for sure we now just want to move!)
                    // Then decide if to perform a single key press of to press the key and keep it pressed based on next position distance and configuration.
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    if (myPosition.Distance(point) <= Mp2MoverSettings.Instance.SingleUseDistance)
                    {
                        LokiPoe.ProcessHookManager.ClearAllKeyStates();
                        AdjustMouseDirection(point);
                        LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                    }
                    else
                    {
                        LokiPoe.ProcessHookManager.ClearAllKeyStates();
                        AdjustMouseDirection(point);
                        LokiPoe.Input.SimulateKeyEvent(Keys.Space, true, false, false);
                    }
                }
                _DasherW.Restart();
            }
            else
            {
                // In this example we check the current state of the key assigned to the move skill.
                if ((LokiPoe.ProcessHookManager.GetKeyState(move.BoundKeys.Last()) & 0x8000) != 0 &&
                    LokiPoe.Me.HasCurrentAction && LokiPoe.Me.CurrentAction.Skill != null)
                {
                    // If the key is pressed, but next moving point is less that SingleUseDistance setted, we need to reset keys and press the move skill once.
                    if (myPosition.Distance(point) <= Mp2MoverSettings.Instance.SingleUseDistance)
                    {
                        LokiPoe.ProcessHookManager.ClearAllKeyStates();
                        LokiPoe.InGameState.SkillBarHud.UseAt(move.Slots.Last(), false, point);
                        Log.WarnFormat("[SkillBarHud.UseAt] {0}", point);
                    }
                    // Otherwise we just move the mouse to the next moving position, for a fast and smooth natural movement.
                    else
                    {
                        MouseManager.SetMousePos("Alcor75PlayerMoverSettings.MoveTowards", point, false);
                    }
                }
                else
                {
                    // If the key was not pressed, we clear all keys state (we newer know what keys other component might have pressed but for sure we now just want to move!)
                    // Then decide if to perform a single key press of to press the key and keep it pressed based on next position distance and configuration.
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    if (myPosition.Distance(point) <= Mp2MoverSettings.Instance.SingleUseDistance)
                    {
                        LokiPoe.InGameState.SkillBarHud.UseAt(move.Slots.Last(), false, point);
                        Log.WarnFormat("[SkillBarHud.UseAt] {0}", point);
                    }
                    else
                    {
                        LokiPoe.InGameState.SkillBarHud.BeginUseAt(move.Slots.Last(), false, point);
                        Log.WarnFormat("[BeginUseAt] {0}", point);
                    }
                }
            }
            return true;
        }

        private void ClearBlacklistedLocations()
        {
            if (BlacklistedLocations.Count > 100)
            {
                BlacklistedLocations.Clear();
                Log.Info("[Mp2Mover] Cleared blacklisted locations to prevent overflow.");
            }
        }

        private static TaskManager GetCurrentBotTaskManager()
        {
            var bot = BotManager.Current;

            var msg = new Message("GetTaskManager");
            bot.Message(msg);
            var taskManager = msg.GetOutput<TaskManager>();

            return taskManager;
        }

        public string Author => "MMMJR and Alcor75";
        public string Description => "Movement.";
        public string Name => "Mp2Mover";
        public string Version => "2.2.0";
    }
}
