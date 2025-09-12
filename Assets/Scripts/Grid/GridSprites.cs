using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xolito.Utilities
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "XolitoSettings/Grid Sprites", order = 0)]
    public class GridSprites : ScriptableObject
    {
        [Header("Blocks")]
        [SerializeField] bool applyChanges = false;
        [SerializeField] List<SpritesType> blackBlocks = new List<SpritesType>();
        [SerializeField] List<SpritesType> whiteBlocks = new List<SpritesType>();

        Dictionary<ColorType, Dictionary<BlockType, SpritesType>> spritesData;

        public Sprite GetSprite(ColorType color, BlockType type, int index)
        {
            if (spritesData == null || applyChanges)
                Initialize();

            return spritesData[color][type][index];
        }

        public ActionType GetAction(ColorType color, BlockType type, int index)
        {
            if (spritesData == null || applyChanges)
                Initialize();

            return spritesData[color][type].action;
        }

        public Vector2Int GetSpriteAmount(ColorType color, BlockType type)
        {
            if (spritesData == null || applyChanges)
                Initialize();

            return spritesData[color][type].amount;
        }

        public bool ContainsBlockType(ColorType color, BlockType type) 
        {
            if (spritesData == null || applyChanges)
                Initialize();

            return spritesData[color][type] != null;
        }

        public int Get_SpritesCount(ColorType color, BlockType type)
        {
            int result = -1;
            try
            {
                result = spritesData[color][type].sprites.Count;
            }
            catch (NullReferenceException)
            {
            }

            return result;
        }

        public (int x, int y) Get_SpriteOffset(ColorType color, BlockType type, int index)
        {
            try {
                if (spritesData == null || applyChanges)
                    Initialize();

                if (color == ColorType.None || type == BlockType.None)
                    return (1, 1);

                return (spritesData[color][type].amount.x, spritesData[color][type].amount.y);
            } catch (Exception) {

                throw;
            }
        }

        private void Initialize()
        {
            spritesData = new Dictionary<ColorType, Dictionary<BlockType, SpritesType>>();
            var blockLists = new List<SpritesType>[]
            {
                blackBlocks,
                whiteBlocks
            };
            Array blockTypes = Enum.GetValues(typeof(BlockType));
            Array colors = Enum.GetValues(typeof(ColorType));

            spritesData.Add(ColorType.None, null);

            for (int i = 0; i < blockLists.Length; i++)
            {
                var sprites = new Dictionary<BlockType, SpritesType>()
                {
                    { BlockType.None, null }
                };

                for (int j = 1; j < blockTypes.Length; j++)
                {
                    SpritesType newList = null;

                    foreach (var data in blockLists[i])
                    {
                        if (data.type == (BlockType)blockTypes.GetValue(j))
                        {
                            newList = data;
                        }
                    }

                    sprites.Add((BlockType)blockTypes.GetValue(j), newList);
                }

                spritesData.Add((ColorType)colors.GetValue(i + 1), sprites);
            }
        }
    }
}
