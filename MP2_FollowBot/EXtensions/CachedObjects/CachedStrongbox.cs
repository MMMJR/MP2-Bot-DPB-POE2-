using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Positions;


namespace MP2.EXtensions
{
    public class CachedStrongbox : CachedObject
    {
        public Rarity Rarity { get; }

        public CachedStrongbox(int id, WalkablePosition position, Rarity rarity)
            : base(id, position)
        {
            Rarity = rarity;
        }

        public new Chest Object => GetObject() as Chest;
    }
}
