using System;
using System.Collections.Generic;
using UnityEngine;
using Xolito.Core;

namespace Xolito.Utilities
{
    internal class Block
    {
        public GameObject block;
        public SpriteRenderer renderer;
        public BlockData data;
        public const int MAX_LAYERS = 9;
        private int? _collider;
        private List<Block> layers;

        public Block this[int idx]
        {
            get
            {
                if (idx == 0) return this;

                if (layers != null && layers.Count > 0)
                    return layers[idx - 1];

                return this;
            }
        }
        public Sprite Sprite 
        {
            get
            {
                if (renderer.sprite != null) return renderer.sprite;

                foreach (var layer in layers)
                {
                    if (layer.Sprite != null)
                        return layer.Sprite;
                }

                return null;
            }
            set => renderer.sprite = value;
        }
        public bool? IsHorizontal { get => data.isHorizontal; set => data.isHorizontal = value; }
        public int? Collider
        {
            get
            {
                return HasAnyCollider(out _);
            }
            set
            {
                var col = HasAnyCollider(out int curLayer);
                var newPos = new Vector2
                {
                    x = Int32.Parse(block.name.Substring(5, 1)),
                    y = Int32.Parse(block.name.Substring(5, 1)),
                };

                if (curLayer == 0)
                {
                    _collider = value;
                    data.colliderPosition = newPos;
                }
                else
                {
                    layers[curLayer].Collider = value;
                    layers[curLayer].data.colliderPosition = newPos;
                }
            }
        }
        public (int y, int x) Position => data.position;

        public static bool operator true(Block block) => block != null;
        public static bool operator false(Block block) => block == null;
        public static bool operator !(Block block)
        {
            if (block != null)
                return true;
            else
                return false;
        }

        public Block(int row, int column, bool createLayers = true)
        {
            block = new GameObject($"Block{row},{column}", typeof(SpriteRenderer));
            renderer = block.GetComponent<SpriteRenderer>();
            data = new BlockData((row, column));

            if (createLayers)
            {
                layers = new List<Block>(9);

                for (int i = 0; i < layers.Count; i++)
                {
                    layers[i] = new Block(row, column, false);
                }
            }
        }

        private BlockData Get_Data()
        {
            int layerIdx = -1;

            if (renderer.sprite == null)
            {
                for (global::System.Int32 i = 0; i < layers.Count; i++)
                {
                    if (layers[i].Sprite != null)
                    {
                        layerIdx = i;
                        break;
                    }
                }
            }    
            else
            {
                layerIdx = 0;
            }

            BlockData curData = layerIdx switch
            {
                <= 0 => null,
                == 0 => data,
                _ => layers[layerIdx],
            };

            return curData;
        }

        public int? HasAnyCollider(out int layer)
        {
            layer = 0;

            if (Collider.HasValue) return Collider;

            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].Collider.HasValue)
                {
                    layer = i;
                    return layers[i].Collider;
                }
            }

            return null;
        }
    }
}
