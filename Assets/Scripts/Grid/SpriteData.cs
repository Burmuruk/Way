using System;
using UnityEngine;

namespace Xolito.Utilities
{
    public class SpriteData
    {
        public ColorType color;
        public BlockType type;
        public int idx;
        public Vector2Int amount;

        public SpriteData(ColorType color, BlockType type, Vector2Int amount, int index)
        {
            this.color = color;
            this.type = type;
            this.amount = amount;
            this.idx = index;
        }

        public override bool Equals(object obj)
        {
            return obj is SpriteData data &&
                   color == data.color &&
                   type == data.type &&
                   idx == data.idx &&
                   amount.Equals(data.amount);
        }

        public static bool operator == (SpriteData a, SpriteData b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator != (SpriteData a, SpriteData b) => !(a == b);

        public override int GetHashCode()
        => HashCode.Combine(color, type, idx, amount);

        public override string ToString()
        {
            return $"SpriteData: color={color}- type={type}- idx={idx}- amount=({amount.x},{amount.y})\n";
        }
    }
}
