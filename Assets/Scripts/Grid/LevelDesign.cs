using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xolito.Utilities
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "XolitoSettings/Level Design", order = 0)]
    internal class LevelDesign : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] GridSprites images;
        [SerializeField] int rows = 0;
        [SerializeField] int columns = 0;


        BlockData[,] grid;

        public class ColliderData
        {
            public List<(int y, int x)> blocks;
            public bool isHorizontal = false;
        }
    }

    public class BlockData
    {
        public SpriteData sprite;
        public (int y, int x) position;
        public bool? isHorizontal = null;
        public Vector2 colliderSize = default;
        public Vector2 colliderPosition = default;
        public Vector2 spriteOffset = default;

        public BlockType Type 
        {
            get => sprite != null ? sprite.type : BlockType.None;
            set
            {
                if (sprite != null) 
                { 
                    sprite.type = value; 
                }
            }
        }

        public BlockData()
        {

        }

        public BlockData((int y, int x) position)
        {
            this.position = position;
            Type = BlockType.None;
        }

        public BlockData(SpriteData data)
        {
            sprite = data;
            Type = BlockType.None;
        }

        public override string ToString()
        {
            return $"BlockData: pos=({position.y},{position.x})- isHorizontal={isHorizontal}\n";
        }
    }
}
