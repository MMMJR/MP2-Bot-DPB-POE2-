using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using MP2.Helpers;
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
    public class Mp2SkillMover : IPlayerMover
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private Mp2SkillMoverGui _instance;

        #region Implementation of IAuthored
        /// <summary> The name of the plugin. </summary>
        public string Name => "Mp2SkillMover";

        /// <summary> The description of the plugin. </summary>
        public string Description => "A plugin that try to use move skills to speed up exploration.";

        /// <summary>The author of the plugin.</summary>
        public string Author => "Bossland GmbH mod by Alcor75";

        /// <summary>The version of the plugin.</summary>
        public string Version => "0.4.2.1";

        #endregion

        //private Coroutine _coroutine;

        private PathfindingCommand _cmd;
        private readonly Stopwatch _sw = new Stopwatch();
        private static Vector2i _lastPoint = Vector2i.Zero;
        private static readonly Stopwatch _dashStopwatch = new Stopwatch();
        private static int _dashStuckCount;
        private static Vector2i _dashCashedPosition = new Vector2i(0, 0);

        ///// <summary>
        ///// These are areas that always have issues with stock pathfinding, so adjustments will be made.
        ///// </summary>
        //private readonly string[] _forcedAdjustmentAreas = new[]
        //{
        //    "The City of Sarn",
        //    "The Slums",
        //};

        private static readonly List<Vector2i> BlacklistedLocations = new List<Vector2i>();
        private bool _casted;
        private int _moveSkillSlot = -1;
        private int _maxStucks = 1;
        private int _stucks;
        private Vector2i _cachePosition = Vector2i.Zero;
        private readonly Stopwatch _skillBlacklistStopwatch = Stopwatch.StartNew();
        private bool _recievedFleeBroadcast = false;

        private static TaskManager GetCurrentBotTaskManager()
        {
            var bot = BotManager.Current;

            var msg = new Message("GetTaskManager");
            bot.Message(msg);
            var taskManager = msg.GetOutput<TaskManager>();

            return taskManager;
        }

        #region Implementation of IBase
        /// <summary>Initializes this object.</summary>
        public void Initialize()
        {
        }
        /// <summary>Deinitializes this object. This is called when the object is being unloaded from the bot.</summary>
        public void Deinitialize()
        {
        }
        #endregion

        #region Implementation of ITickEvents / IStartStopEvents
        /// <summary> The mover start callback. Do any initialization here. </summary>
        public void Start()
        {

        }
        /// <summary> The mover tick callback. Do any update logic here. </summary>
        public void Tick()
        {
            if (!LokiPoe.IsInGame)
                return;
        }
        /// <summary> The mover stop callback. Do any pre-dispose cleanup here. </summary>
        public void Stop()
        {
        }

        #endregion

        #region Implementation of IConfigurable

        public JsonSettings Settings => Mp2SkillMoverSettings.Instance;

        /// <summary> The plugin's settings control. This will be added to the Exilebuddy Settings tab.</summary>
        public UserControl Control => (_instance ?? (_instance = new Mp2SkillMoverGui()));

        #endregion

        #region Implementation of ILogicHandler

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

        #region Implementation of IMessageHandler

        /// <summary>
        /// Implements logic to handle a message passed through the system.
        /// </summary>
        /// <param name="message">The message to be processed.</param>
        /// <returns>A tuple of a MessageResult and object.</returns>
        public MessageResult Message(Message message)
        {
            var id = message.Id;
            if (id == "SkillPlayerMover_player_flee")
            {
                List<Vector2> dangerPositions;
                message.TryGetInput<List<Vector2>>(0, out dangerPositions);
                //Log.WarnFormat($"[{Name} flee broadcast recieved]");
                _recievedFleeBroadcast = true;
                if (dangerPositions != null && dangerPositions.Count > 0)
                {
                    foreach (var position in dangerPositions)
                    {
                        var pos = position.ToVector2i();
                        ExilePather.PolyPathfinder.AddObstacle(pos, 30f);
                    }
                }
            }
            if (id == "dangerdodger_danger_positions")
            {

            }
            return MessageResult.Unprocessed;
        }

        #endregion

        #region Implementation of IEnableable

        /// <summary> The plugin is being enabled.</summary>
        public void Enable()
        {
        }

        /// <summary> The plugin is being disabled.</summary>
        public void Disable()
        {
        }

        #endregion

        #region Override of Object

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Name + ": " + Description;
        }

        #endregion

        #region Override of IMover

        /// <summary>
        /// Returns the player mover's current PathfindingCommand being used.
        /// </summary>
        public PathfindingCommand CurrentCommand => _cmd;

        /// <summary>
        /// Attempts to move towards a position. This function will perform pathfinding logic and take into consideration move distance
        /// to try and smoothly move towards a point.
        /// </summary>
        /// <param name="position">The position to move towards.</param>
        /// <param name="user">A user object passed.</param>
        /// <returns>true if the position was moved towards, and false if there was a pathfinding error.</returns>
        public bool MoveTowards(Vector2i position, params dynamic[] user)
        {
            if (position == Vector2i.Zero)
            {
                Log.ErrorFormat("[SkillPlayerMover.MoveTowards] Recieved 0,0 as position return false.");
                return false;
            }
            var myPosition = LokiPoe.MyPosition;

            if (_casted ||
                _cmd == null || // No command yet
                _cmd.Path == null ||
                _cmd.EndPoint != position || // Moving to a new position
                LokiPoe.CurrentWorldArea.IsTown || // In town, always generate new paths
                (_sw.IsRunning && _sw.ElapsedMilliseconds > Mp2SkillMoverSettings.Instance.PathRefreshRateMs) || // New paths on interval
                _cmd.Path.Count <= 2 || // Not enough points
                _cmd.Path.All(p => myPosition.Distance(p) > 7))
            // Try and find a better path to follow since we're off course
            {
                _cmd = new PathfindingCommand(myPosition, position, 3, Mp2SkillMoverSettings.Instance.AvoidWallHugging);
                if (!ExilePather.FindPath(ref _cmd))
                {
                    _sw.Restart();
                    Log.ErrorFormat("[SkillPlayerMover.MoveTowards] ExilePather.FindPath failed from {0} to {1}.",
                        myPosition, position);
                    return false;
                }
                _sw.Restart();
            }

            var cwa = LokiPoe.CurrentWorldArea;
            var specialMoveRange = Mp2SkillMoverSettings.Instance.MoveRange;
            if (cwa.IsTown)
                specialMoveRange = 12;
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
            // Le sigh...

            //used to check if there are objects on path
            var pathCheck = ExilePather.Raycast(myPosition, point, out var hitPoint);
            if (!pathCheck)
            {
                BlacklistedLocations.Add(point);
                BlacklistedLocations.Add(hitPoint);
            }

            // If we think we used a movement skill but haven't actually moved, we're stuck.
            if (_casted && _cachePosition == myPosition)
            {
                _stucks++;
            }

            var canUseMoveSkill = true;

            // If we've tried too many times to hit the given point on path with no success, blacklist it and move on.
            if (_stucks >= _maxStucks)
            {
                BlacklistedLocations.Add(point);

                _skillBlacklistStopwatch.Restart();
                //_stucks = 0;
                canUseMoveSkill = false;
            }

            if (_cachePosition == myPosition)
            {
                // If we're blacklisting it, shouldn't we be NOT moving to it?
                // This appears to be some sort of attempt to prevent us from moving back to the original spot - 
                // "if we haven't moved from the starting point, add the destination as a blacklisted spot"
                // I think this is supposed to be part of the stuck handling logic. Moving it there.
                //PlayerMoverPlugin.BlacklistedLocations.Add(pointOnPath);
            }
            // If we've actually moved, clear the blacklist and move on.
            else
            {
                _stucks = 0;
                BlacklistedLocations.Clear();
            }

            _cachePosition = myPosition;

            _moveSkillSlot = SkillsHelpers.SkillReady(point);
            if (_moveSkillSlot == -1 || cwa.IsTown || cwa.IsHideoutArea || cwa.IsMapRoom || cwa.Id == "Labyrinth_Airlock")
            {
                canUseMoveSkill = false;
            }
            // Try to use move skill.
            if (canUseMoveSkill && myPosition.Distance(point) > Mp2SkillMoverSettings.Instance.SingleUseDistance &&
                (!_skillBlacklistStopwatch.IsRunning || _skillBlacklistStopwatch.ElapsedMilliseconds > 150 || _recievedFleeBroadcast)
            )
            {
                if (_recievedFleeBroadcast)
                {
                    _recievedFleeBroadcast = false;
                    CastSkillToMove(_moveSkillSlot, point, point, myPosition);
                    _casted = true;
                    return true;
                }

                var skillMovePoint = FindSkillPosition(_moveSkillSlot, position, _cmd, BlacklistedLocations);
                if (skillMovePoint != Vector2i.Zero)
                {
                    _casted = CastSkillToMove(_moveSkillSlot, skillMovePoint, point, myPosition);
                    if (_recievedFleeBroadcast) _recievedFleeBroadcast = false;
                    return true;
                }
                if (_cachePosition != LokiPoe.Me.Position)
                {
                    _skillBlacklistStopwatch.Reset();
                }
            }
            _casted = false;
            if (_recievedFleeBroadcast) _recievedFleeBroadcast = false;

            return BasicMove(myPosition, point);
        }

        #endregion

        private Vector2i FindSkillPosition(int moveSkillSlot, Vector2i originalDestination, PathfindingCommand cmd, List<Vector2i> blacklistedLocations)
        {
            var myPosition = new Vector2i(LokiPoe.Me.Position.X, LokiPoe.Me.Position.Y);
            var skillName = LokiPoe.InGameState.SkillBarHud.Slot(moveSkillSlot).Name;

            var maxdist = MoveHelper.GetSkillMaxRange(skillName);
            var mindist = MoveHelper.GetSkillMinRange(skillName);

            var desttest = cmd.Path.Where(
                        pathVector => !blacklistedLocations.Contains(pathVector) &&
                                      PathDistanceCheck(pathVector, myPosition, maxdist, mindist, originalDestination)


                )
                    .OrderBy(x => ExilePather.PathDistance(x, myPosition))
                    .ToList();

            for (var i = desttest.Count - 1; i >= 0; --i)
            {
                if (!ExilePather.Raycast(myPosition, desttest[i], out Vector2i res))
                {
                    continue;
                }

                if (MiscHelpers.ClosedDoorBetween(myPosition, desttest[i], 10, 10, true))
                {
                    continue;
                }
                var dest = desttest[i] += new Vector2i(LokiPoe.Random.Next(-2, 2), LokiPoe.Random.Next(-2, 2));
                return dest;
            }

            return Vector2i.Zero;
        }

        private bool PathDistanceCheck(Vector2i pathVector, Vector2i myPosition, int maxdist, int mindist, Vector2i originalDestination)
        {
            var myposdist = pathVector.Distance(myPosition);
            //var myposdist = ExilePather.PathDistance(myPosition, pathVector);
            if (myposdist > maxdist) return false;
            if (myposdist < mindist) return false;
            //if (myposdist > ExilePather.PathDistance(myPosition, originalDestination)) return false;

            return true;
        }

        private static bool CastSkillToMove(int moveSkillSlot, Vector2i movingSpellPosition, Vector2i originalPosition, Vector2i myPosition)
        {
            //if (movingSpellPosition == Vector2i.Zero) return false;

            var skillName = LokiPoe.InGameState.SkillBarHud.Slot(moveSkillSlot).Name;

            if (skillName == "Phase Run")// Withering Step, InternalName: Slither, InternalId: slither
            {
                if (LokiPoe.Me.Auras.All(x => x.Name != "Phase Run"))
                {
                    //LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    LokiPoe.InGameState.SkillBarHud.Use(moveSkillSlot, false, false);
                }
                return BasicMove(myPosition, originalPosition);
            }
            if (skillName == "Withering Step")// Withering Step, InternalName: Slither, InternalId: slither
            {
                if (LokiPoe.Me.Auras.All(x => x.Name != "Withering Step"))
                {
                    //LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    LokiPoe.InGameState.SkillBarHud.Use(moveSkillSlot, false, false);
                }
                return BasicMove(myPosition, originalPosition);
            }

            if (skillName == "Dash")
            {
                if (myPosition == _dashCashedPosition)
                {
                    _dashStuckCount += 1;
                }

                if (_dashStuckCount > 2)
                {
                    _dashStopwatch.Restart();
                    _dashStuckCount = 0;
                    return BasicMove(myPosition, originalPosition);
                }
                if (_dashStopwatch.IsRunning && _dashStopwatch.ElapsedMilliseconds <= 1000) return BasicMove(myPosition, originalPosition);
                LokiPoe.InGameState.SkillBarHud.Use(moveSkillSlot, false, false);
                _dashStopwatch.Restart();
                _dashCashedPosition = myPosition;
                return false;
            }

            //SimpleMove(myPosition, movingSpellPosition);
            switch (skillName)
            {
                case "Shield Charge":
                    return SkillsHelpers.CastAttackMovementSkill(moveSkillSlot, movingSpellPosition, "Shield Charge");
                default:
                    return SkillsHelpers.UseSkillAwareMovementSkill(moveSkillSlot, movingSpellPosition, originalPosition, myPosition);
            }
        }

        private static bool BasicMove(Vector2i myPosition, Vector2i point)
        {
            var move = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill;
            if (move == null)
            {
                Log.ErrorFormat("[SkillPlayerMover.MoveTowards] Please assign the \"Move\" skill to your skillbar, do not use mouse button, use q,w,e,r,t!");

                var plugin = PluginManager.Plugins.FirstOrDefault(p => p.Name == "LajtTools");
                if (plugin != null)
                {
                    Log.DebugFormat($"[SkillPlayerMover] LajtTools detected, trying to update skills");
                    var moveSkillSlot = (int)plugin.Settings.GetProperty("TutorialMoveSkillSlot");
                    var moveSkill = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Move" && !s.IsOnSkillBar);
                    if (moveSkill != null)
                    {
                        var setSlotResult = LokiPoe.InGameState.SkillBarHud.SetSlot(moveSkillSlot, moveSkill);
                        Log.DebugFormat($"setResult: {setSlotResult}");
                    }
                }

                return false;
            }

            var currentAction = LokiPoe.Me?.CurrentAction?.Skill;
            if ((LokiPoe.ProcessHookManager.GetKeyState(move.BoundKeys.Last()) & 0x8000) != 0 &&
                currentAction != null && currentAction.InternalId.Equals("Move"))
            {
                if (myPosition.Distance(point) <= Mp2SkillMoverSettings.Instance.SingleUseDistance)
                {
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    LokiPoe.InGameState.SkillBarHud.UseAt(move.Slots.Last(), false, point);
                    if (Mp2SkillMoverSettings.Instance.DebugInputApi)
                    {
                        Log.WarnFormat("[SkillBarHud.UseAt] {0}", point);
                    }
                    _lastPoint = point;
                }
                else
                {
                    MouseManager.SetMousePos("Mp2SkillMoverSettings.MoveTowards", point, false);
                }
            }
            else
            {
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                if (myPosition.Distance(point) <= Mp2SkillMoverSettings.Instance.SingleUseDistance)
                {
                    LokiPoe.InGameState.SkillBarHud.UseAt(move.Slots.Last(), false, point);
                    if (Mp2SkillMoverSettings.Instance.DebugInputApi)
                    {
                        Log.WarnFormat("[SkillBarHud.UseAt] {0}", point);
                    }
                }
                else
                {
                    LokiPoe.InGameState.SkillBarHud.BeginUseAt(move.Slots.Last(), false, point);
                    if (Mp2SkillMoverSettings.Instance.DebugInputApi)
                    {
                        Log.WarnFormat("[BeginUseAt] {0}", point);
                    }
                }
            }
            return true;
        }
    }
}
