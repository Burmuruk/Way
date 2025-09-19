using System;
using UnityEngine;

namespace Xolito.Utilities
{
    public readonly struct SpriteData : IEquatable<SpriteData>
    {
        public readonly ColorType color;
        public readonly BlockType type;
        public readonly Vector2Int amount;
        public readonly int idx;

        public SpriteData(ColorType color, BlockType type, Vector2Int amount, int idx)
        {
            this.color = color;
            this.type = type;
            this.amount = amount;
            this.idx = idx;
        }

        public bool Equals(SpriteData other) =>
            color == other.color &&
            type == other.type &&
            amount == other.amount &&
            idx == other.idx;

        public override bool Equals(object obj) => obj is SpriteData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(color, type, amount, idx);

        public static bool operator ==(SpriteData left, SpriteData right) => left.Equals(right);
        public static bool operator !=(SpriteData left, SpriteData right) => !left.Equals(right);
    }

}
