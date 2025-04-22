using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xolito.Utilities
{
    internal class ColliderData
    {
        public GameObject item;
        public List<Block> blocks;
        public BoxCollider2D collider;
        public bool? isHorizontal = null;
        public int headIdx = 0;
        private bool isTrigger = false;
        private Action<GameObject> _destructionCallback;

        public Block First => blocks == null || blocks.Count == 0 ? null : blocks[0];
        public Block Last => blocks == null || blocks.Count == 0 ? null : blocks[blocks.Count - 1];
        public Block Head => blocks == null || blocks.Count == 0 ? null : blocks[headIdx];
        public bool IsTrigger
        {
            get => isTrigger;
            set
            {
                if (collider)
                    collider.isTrigger = value;

                isTrigger = value;
            }
        }
        public string Tag
        {
            get => collider.tag;
            set => collider.tag = value;
        }
        public Vector2Int ElementsPerUnit { get; set; }

        public ColliderData(string name, Block block, bool? isHorizontal, Vector2Int elementsPerUnit, Action<GameObject> destructionCallback)
        {
            this.item = new GameObject(name, typeof(BoxCollider2D));
            collider = item.GetComponent<BoxCollider2D>();
            blocks = new List<Block>();
            this.isHorizontal = isHorizontal;
            ElementsPerUnit = elementsPerUnit;
            _destructionCallback = destructionCallback;

            AddFirst(block);
        }

        public void AddFirst(Block block) => blocks.Insert(0, block);
        public void AddLast(Block block) => blocks.Insert(blocks.Count, block);
        public void RemoveFirst()
        {
            blocks[0].Collider = null;
            blocks.RemoveAt(0);
        }
        public void RemoveLast()
        {
            if (headIdx == blocks.Count - 1)
                headIdx = blocks.Count - 2;

            blocks[blocks.Count - 1].Collider = null;
            blocks.RemoveAt(blocks.Count - 1);
        }

        public void Clear()
        {
            foreach (var block in blocks)
                block.Collider = null;

            blocks.Clear();
            _destructionCallback?.Invoke(item);
        }

        public Point[] GetPoints()
        {
            Point[] points = new Point[blocks.Count];

            int i = 0;
            foreach (var block in blocks)
                points[i++] = new Point(block.Position);

            return points;
        }
    }
}
