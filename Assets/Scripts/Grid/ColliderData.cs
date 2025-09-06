using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xolito.Utilities
{
    public class ColliderData
    {
        public GameObject item;
        public List<SubBlock> blocks;
        public BoxCollider2D collider;
        public bool? isHorizontal = null;
        public int headIdx = 0;
        private bool isTrigger = false;
        private Action<GameObject> _destructionCallback;

        public SubBlock First => blocks == null || blocks.Count == 0 ? null : blocks[0];
        public SubBlock Last => blocks == null || blocks.Count == 0 ? null : blocks[blocks.Count - 1];
        public SubBlock Head => blocks == null || blocks.Count == 0 ? null : blocks[headIdx];
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

        public ColliderData(string name, SubBlock block, bool? isHorizontal, Vector2Int elementsPerUnit, Action<GameObject> destructionCallback)
        {
            this.item = new GameObject(name, typeof(BoxCollider2D));
            collider = item.GetComponent<BoxCollider2D>();
            blocks = new List<SubBlock>();
            this.isHorizontal = isHorizontal;
            ElementsPerUnit = elementsPerUnit;
            _destructionCallback = destructionCallback;

            AddFirst(block);
        }

        public void AddFirst(SubBlock block) => blocks.Insert(0, block);
        public void AddLast(SubBlock block) => blocks.Insert(blocks.Count, block);
        public void RemoveFirst()
        {
            blocks[0].collider = null;
            blocks.RemoveAt(0);
        }
        public void RemoveLast()
        {
            if (headIdx == blocks.Count - 1)
                headIdx = blocks.Count - 2;

            blocks[blocks.Count - 1].collider = null;
            blocks.RemoveAt(blocks.Count - 1);
        }

        public void Clear()
        {
            foreach (var block in blocks)
                block.collider = null;

            blocks.Clear();
            _destructionCallback?.Invoke(item);
        }

        public Point[] GetPoints()
        {
            Point[] points = new Point[blocks.Count];

            int i = 0;
            foreach (var block in blocks)
                points[i++] = new Point(block.Position, block.Layer);

            return points;
        }

        public override string ToString()
        {
            string str = $"ColliderData: isHorizontal={isHorizontal}- isTrigger={isTrigger}- headIdx={headIdx}-";
            var pos = item.transform.position;
            str += $"position={pos.x},{pos.y}-";
            str += " blocks=";
            string slicer = "";

            foreach (var block in blocks)
            {
                str += slicer;
                str += block;
                slicer = ",";
            }

            str += $"-size={collider.size}\n";
            return str;
        }
    }
}
