using System;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Positions;

namespace MP2.EXtensions
{
    public class CachedPosition : IEquatable<CachedPosition>
    {
        private bool _ignored;

        public int Id { get; }
        public WalkablePosition Position { get; set; }
        public bool Unwalkable { get; set; }
        public int InteractionAttempts { get; set; }
        public NetworkObject Object => GetObject();

        public bool IsMonster { get; set; }

        public bool Ignored
        {
            get => _ignored;
            set
            {
                if (value == false)
                    InteractionAttempts = 0;

                _ignored = value;
            }
        }

        public CachedPosition(int id, Vector2i position, bool isMonster)
        {
            Id = id;
            Position = new WalkablePosition("", position);
            Ignored = false;
            IsMonster = isMonster;
        }

        public bool Equals(CachedPosition other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CachedPosition);
        }

        public static bool operator ==(CachedPosition left, CachedPosition right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (((object)left == null) || ((object)right == null))
                return false;

            return left.Id == right.Id;
        }

        public static bool operator !=(CachedPosition left, CachedPosition right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Position.ToString();
        }

        protected NetworkObject GetObject()
        {
            return LokiPoe.ObjectManager.Objects.Find(o => o.Id == Id);
        }
    }
}
