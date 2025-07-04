﻿using System;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MP2.EXtensions.Positions;

namespace MP2.EXtensions
{
    public class CachedObject : IEquatable<CachedObject>
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

        public CachedObject(int id, WalkablePosition position)
        {
            Id = id;
            Position = position;
            IsMonster = false;
        }

        public CachedObject(int id, WalkablePosition position, bool isMonster)
        {
            Id = id;
            Position = position;
            IsMonster = isMonster;
        }

        public CachedObject(NetworkObject obj)
        {
            Id = obj.Id;
            Position = obj.WalkablePosition();
            IsMonster = false;
        }

        public bool Equals(CachedObject other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CachedObject);
        }

        public static bool operator ==(CachedObject left, CachedObject right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (((object)left == null) || ((object)right == null))
                return false;

            return left.Id == right.Id;
        }

        public static bool operator !=(CachedObject left, CachedObject right)
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
