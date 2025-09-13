using System.Collections.Generic;
using UnityEngine;

namespace Xolito.Utilities
{
    public class GridBlock
    {
        public const int MAX_LAYERS = 10;
        public List<SubBlock> blocks;
        private (int y, int x) position;

        public SubBlock this[int idx]
        {
            get
            {
                if (blocks == null || blocks.Count <= idx)
                    return null;

                return blocks[idx];
            }
            set
            {
                if (blocks != null && blocks.Count > idx)
                    blocks[idx] = value;
            }
        }
        public Vector3 Position { get; set; }

        public static bool operator true(GridBlock block) => block != null;
        public static bool operator false(GridBlock block) => block == null;
        public static bool operator !(GridBlock block)
        {
            if (block != null)
                return true;
            else
                return false;
        }

        public GridBlock(int row, int column, Vector3 position, Vector2 spriteSize)
        {
            Position = position;
            blocks = new();

            for (int i = 0; i < MAX_LAYERS; i++)
            {
                var block = new SubBlock(row, column, i);
                block.Block.transform.position = position;
                block.Renderer.size = spriteSize;

                blocks.Add(block);
            }
        }

        public GridBlock()
        {
            blocks = new(MAX_LAYERS);
        }

        public (int y, int x) GridPosition => position;

        public int? GetCollider(int layer) => blocks[layer].collider;

        public void SetCollider(int? idx, int layer) => blocks[layer].collider = idx;

        private BlockData Get_Data(int layer) => blocks[layer].data;

        public void SetParent(Transform parent)
        {
            foreach (var layer in blocks)
            {
                layer.Block.transform.parent = parent;
            }
        }

        public bool ContainsBlockType(BlockType type, out int layer)
        {
            layer = 0;

            foreach (var block in blocks)
            {
                if (block.data.Type == type)
                {
                    layer = block.Layer;
                    return true;
                }
            }

            return false;
        }

        public int? GetSpriteCloserTo(Vector3 position)
        {
            var (minDis, idx) = (float.MaxValue, -1);

            foreach (var block in blocks)
            {
                float curDis = Vector3.Distance(block.Block.transform.position, position);
                if (block.Sprite != null && curDis < minDis)
                {
                    (minDis, idx) = (curDis, block.Layer);
                }
            }

            return idx < 0 ? null : idx;
        }

        public void Clear(int layer) => blocks[layer].Clear();

        public void Clear() => blocks.ForEach(b => b.Clear());

        public override string ToString()
        {
            string str = $"GridBlock: position=({position.y},{position.x})-\n";
            str += " blocks=";
            string slicer = "";

            foreach (var block in blocks)
            {
                str += slicer;
                str += block;
                slicer = ",";
            }

            str += "\n";
            return str;
        }
    }
}
