using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace MP2.EXtensions.Tasks.Breach
{
	public class BreachCache
	{
		private static readonly ILog Log = Logger.GetLoggerInstanceForType();

		public static int ValidateDistance = 75;

		public bool IsValid { get; private set; }

		public int Id { get; }

		public Vector2i Position { get; }

		public Vector2i WalkablePosition { get; }

		public DreamPoeBot.Loki.Game.Objects.Breach NetworkObject => LokiPoe.ObjectManager.GetObjectById<DreamPoeBot.Loki.Game.Objects.Breach>(Id);

		public bool? Activate { get; set; }

		public BreachCache(DreamPoeBot.Loki.Game.Objects.Breach breach)
		{
			Id = breach.Id;
			Position = breach.Position;
			WalkablePosition = ExilePather.FastWalkablePositionFor(breach);
			IsValid = true;

			Log.InfoFormat("[BreachCache] {0} {1}", Id, WalkablePosition);
		}
		public void Update(DreamPoeBot.Loki.Game.Objects.Breach breach)
		{
			// Nothing to do here
		}

		public void Validate()
		{
			if (!IsValid)
				return;

			// If we're in spawning range of the breach, but it no longer exists or is not a Breach object, it's no longer valid.
			if (LokiPoe.MyPosition.Distance(Position) <= ValidateDistance)
			{
				var breach = NetworkObject;
				if (breach == null)
				{
					Log.InfoFormat("[BreachCache::Validate] The Breach [{0}] is no longer valid because it does not exist.", Id);
					IsValid = false;
				}
			}
		}
	}
}