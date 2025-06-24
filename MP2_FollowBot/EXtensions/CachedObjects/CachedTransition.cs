using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Positions;

namespace MP2.EXtensions
{
    public class CachedTransition : CachedObject
    {
        public TransitionType Type { get; }
        public DatWorldAreaWrapper Destination { get; }
        public bool Visited { get; set; }
        public bool LeadsBack { get; set; }
        public string Name { get; }

        public CachedTransition(int id, WalkablePosition position, TransitionType type, DatWorldAreaWrapper destination)
            : base(id, position)
        {
            Type = type;
            Destination = destination;
            Name = Object?.Name;
        }

        public new AreaTransition Object => GetObject() as AreaTransition;
    }
}
