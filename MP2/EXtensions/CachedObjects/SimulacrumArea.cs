using System;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Positions;

namespace MP2.EXtensions
{
    public class SimulacrumArea
    {
        public WalkablePosition Position { get; set; }
        public bool Unwalkable { get; set; }
        public int InteractionAttempts { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Vector2i movePosition { get; set; }


        public SimulacrumArea(int _X, int _Y)
        {
            X = _X;
            Y = _Y;
            movePosition = new Vector2i(X, Y);
            Position = new WalkablePosition("", movePosition);
        }

        public async Task CheckAndMoveToCenter()
        {
            await Position.ComeAtOnce();
            //await Coroutines.FinishCurrentMoveAction();
        }

        public bool Equals(SimulacrumArea other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SimulacrumArea);
        }

        public static bool operator ==(SimulacrumArea left, SimulacrumArea right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (((object)left == null) || ((object)right == null))
                return false;

            return left.movePosition == right.movePosition;
        }

        public static bool operator !=(SimulacrumArea left, SimulacrumArea right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Position.ToString();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
