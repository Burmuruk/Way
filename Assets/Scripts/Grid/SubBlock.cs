using UnityEngine;

namespace Xolito.Utilities
{
    internal class SubBlock
    {
        public GameObject block;
        public SpriteRenderer renderer;
        public ColorType color;
        public Vector2Int amount;
        public (int y, int x) position;
        public bool? isHorizontal = null;
        public Vector2 colliderSize = default;
        public Vector2 colliderPosition = default;
        private int? _collider;

        //public (int y, int x) Position { get => data.position; }
    }
}
