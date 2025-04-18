using System;
using UnityEngine;

namespace Xolito.Utilities
{
    internal class Block
    {
        public GameObject block;
        public GameObject backGround;
        public SpriteRenderer renderer;
        public SpriteRenderer bRenderer;
        public BlockData data;
        public int? collider = null;

        public Sprite Sprite { get => renderer.sprite; set => renderer.sprite = value; }
        public Sprite BSprite
        {
            get => bRenderer?.sprite;
            set
            {
                if (backGround is null)
                {
                    backGround = new GameObject(block.name + " Back", typeof(SpriteRenderer));
                    backGround.transform.position = block.transform.position;
                    bRenderer = backGround.GetComponent<SpriteRenderer>();
                }

                if (value is null)
                    backGround = null;
                else
                    bRenderer.sprite = value;
            }
        }
        public bool? IsHorizontal { get => data.isHorizontal; set => data.isHorizontal = value; }
        public int? Collider
        {
            get => collider;
            set
            {
                collider = value;
                data.colliderPosition = new Vector2
                {
                    x = Int32.Parse(block.name.Substring(5, 1)),
                    y = Int32.Parse(block.name.Substring(5, 1)),
                };
            }
        }
        public (int y, int x) Position { get => data.position; }

        public static bool operator true(Block block) => block != null;
        public static bool operator false(Block block) => block == null;
        public static bool operator !(Block block)
        {
            if (block != null)
                return true;
            else
                return false;
        }

        public Block(int row, int column)
        {
            block = new GameObject($"Block{row},{column}", typeof(SpriteRenderer));
            renderer = block.GetComponent<SpriteRenderer>();
            data = new BlockData((row, column));
        }
    }
}
