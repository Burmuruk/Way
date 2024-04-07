using UnityEngine;

namespace Xolito.Utilities
{
    public class SpriteData
    {
        public ColorType color;
        public BlockType type;
        public Vector2Int amount;

        public SpriteData(ColorType color, BlockType type, Vector2Int amount)
        {
            this.color = color;
            this.type = type;
            this.amount = amount;
        }
    }
}
