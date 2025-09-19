using UnityEngine;

namespace Xolito.Utilities
{
    public class SubBlock
    {
        public BlockData data;
        public string colliderId;

        public int Layer { get; private set; }
        public GameObject Block { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public Sprite Sprite
        {
            get
            {
                return Renderer.sprite;
            }
            set
            {
                Renderer.sprite = value;
            }
        }
        public bool? IsHorizontal { get => data.isHorizontal; set => data.isHorizontal = value; }
        public (int y, int x) Position => data.position;
        public bool HasCollider { get => ColliderId != null; }
        public string ColliderId {
            get => colliderId;
            set
            {
                if (value == "")
                {
                    colliderId = null;
                }
                else
                    colliderId = value;
            }
        }
        public static bool operator true(SubBlock block) => block != null;
        public static bool operator false(SubBlock block) => block == null;
        public static bool operator !(SubBlock block)
        {
            if (block != null)
                return true;
            else
                return false;
        }

        public SubBlock(int row, int column, int layer)
        {
            Block = new GameObject($"Block{row},{column}", typeof(SpriteRenderer));
            Renderer = Block.GetComponent<SpriteRenderer>();
            data = new BlockData((row, column));

            Renderer.sortingOrder = layer;
            Layer = layer;
            ColliderId = null;
        }

        public void Clear()
        {
            Sprite = null;
            data.sprite = default;
            data.Type = BlockType.None;
        }

        public override string ToString()
        {
            string str = $"SubBlock: pos=({Position.y},{Position.x})- layer={Layer}- data={data}- isHorizontal={IsHorizontal}- hasCollider={HasCollider}-";
            str += $"objPos={Block.transform.position.x},{Block.transform.position.y}-\n";

            return str;
        }
    }
}
