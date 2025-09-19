using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Xolito.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xolito.Utilities
{
    internal class GridSaver
    {
        public void SaveToTxt(GridBlock[,] grid, Dictionary<string, ColliderData> colliders, GridSprites sprites, string relativePathInAssets)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (colliders == null) throw new ArgumentNullException(nameof(colliders));
            if (string.IsNullOrWhiteSpace(relativePathInAssets)) throw new ArgumentException("Ruta inválida", nameof(relativePathInAssets));

            var dto = new SaveFileDTO
            {
                rows = grid.GetLength(0),
                cols = grid.GetLength(1),
                colliders = colliders.Select(c => new ColliderDataDTO().From(c.Value, c.Key)).ToList(),
                spritesGuid = GetGuidForAsset(sprites)
            };

            dto.cells = new List<CellDTO>(dto.rows * dto.cols);
            for (int y = 0; y < dto.rows; y++)
            {
                for (int x = 0; x < dto.cols; x++)
                {
                    var cell = new CellDTO { y = y, x = x, blocks = new List<SubBlockDTO>() };
                    var gb = grid[y, x];
                    if (gb != null)
                    {
                        foreach (var sb in gb.GetBlocksSafe())
                        {
                            cell.blocks.Add(SubBlockDTO.From(sb));
                        }
                    }
                    dto.cells.Add(cell);
                }
            }

            string json = JsonUtility.ToJson(dto, prettyPrint: true);

            string fullPath = ToFullPath(relativePathInAssets);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, json);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public (GridBlock[,] grid, Dictionary<string, ColliderData> colliders, GridSprites sprites) LoadFromTxt(string relativePathInAssets, Action<GameObject> destructionCallback)
        {
            return LoadFromTxt(relativePathInAssets, destructionCallback, CreateParents());
        }

        private Dictionary<BlockType, GameObject> CreateParents()
        {
            var baseParent = new GameObject("Grid");
            var collidersParent = new GameObject("Colliders");
            collidersParent.transform.parent = baseParent.transform;
            var blocksParent = new GameObject("Blocks");
            blocksParent.transform.parent = baseParent.transform;
            var parents = new Dictionary<BlockType, GameObject>();

            var values = System.Enum.GetValues(typeof(BlockType));

            foreach (var value in values)
            {
                var type = (BlockType)value;

                var parent = new GameObject(type.ToString());
                parent.transform.parent = baseParent.transform;

                parents.Add(type, parent);
            }

            return parents;
        }

        public (GridBlock[,] grid, Dictionary<string, ColliderData> colliders, GridSprites sprites) LoadFromTxt(string relativePathInAssets, Action<GameObject> destructionCallback, Dictionary<BlockType, GameObject> parents)
        {
            if (string.IsNullOrWhiteSpace(relativePathInAssets)) throw new ArgumentException("Ruta inválida", nameof(relativePathInAssets));

            string fullPath = ToFullPath(relativePathInAssets);
            if (!File.Exists(fullPath))
            {
                Debug.LogError("File not found");
                return (null, null, null);
            }    

            string json = File.ReadAllText(fullPath);
            var dto = JsonUtility.FromJson<SaveFileDTO>(json);

            GridSprites sprites = LoadAssetByGuid<GridSprites>(dto.spritesGuid);

            var grid = new GridBlock[dto.rows, dto.cols];

            foreach (var cell in dto.cells)
            {
                var (x, y) = (cell.blocks[0].data.posX, cell.blocks[0].data.posY);
                var gb = grid[y, x];

                grid[y, x] = gb = cell.ToGridBlock(sprites, parents);
                gb.Position = ScreenToWorldPoint((y, x), dto.cols, dto.rows);

                foreach (var sbdto in gb.blocks)
                {
                    sbdto.Block.transform.position = ScreenToWorldPoint(sbdto, grid.GetLength(1), grid.GetLength(0), sprites);
                }
            }

            List<(string id, ColliderData data)> colliders = dto.colliders.Select(c => (c.id, c.ToColliderData(grid, destructionCallback))).ToList();
            Dictionary<string, ColliderData> colsDic = new();

            if (colliders.Count > 0)
                colliders.ForEach(c => colsDic.Add(c.id, c.data));

            SetCollidersParent(parents, colsDic);

            return (grid, colsDic, sprites);
        }

        private static void SetCollidersParent(Dictionary<BlockType, GameObject> parents, Dictionary<string, ColliderData> colliders)
        {
            var parent = parents.First().Value.transform.parent;
            Transform collidersParent = parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == "Colliders")
                    collidersParent = parent.GetChild(i);
            }

            foreach (var col in colliders)
            {
                col.Value.collider.transform.parent = collidersParent;
            }
        }

        private Vector3 ScreenToWorldPoint(SubBlock block, int columns, int rows, GridSprites sprites)
        {
            // Posición de cuadrícula (asegúrate que sea x,y en ese orden)
            int gridX = block.Position.x;
            int gridY = block.Position.y;

            // Tamaño del bloque de la grilla en píxeles
            var blockSize = new Vector2(
                (float)Camera.main.pixelWidth / columns,
                (float)Camera.main.pixelHeight / rows
            );

            // Cantidad de subdivisiones que ocupa el sprite
            var (countX, countY) = sprites.Get_SpriteOffset(
                block.data.sprite.color, block.data.sprite.type, block.data.sprite.idx
            );
            Vector2Int amount = new(
                countX > 0 ? countX : 1,
                countY > 0 ? countY : 1
            );

            // Tamaño de cada subcelda
            float sizeX = blockSize.x / amount.x;
            float sizeY = blockSize.y / amount.y;

            // IMPORTANTE: usar la mitad de la subcelda, no del bloque completo
            var subCellHalf = new Vector2(sizeX * 0.5f, sizeY * 0.5f);

            // Offset discreto del sprite dentro del bloque (asumo que ya lo traes entero)
            int offX = (int)block.data.spriteOffset.x;
            int offY = (int)block.data.spriteOffset.y;

            // Coordenadas de pantalla (en píxeles)
            var screen = new Vector2(
                sizeX * ((gridX * amount.x) + offX) + subCellHalf.x,
                sizeY * ((gridY * amount.y) + offY) + subCellHalf.y
            );

            // A mundo
            var world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            world.z = 0f;
            return world;
        }

        private Vector3 ScreenToWorldPoint((int y, int x) point, int columns, int rows)
        {
            var blockSize = new Vector2(
                (float)Camera.main.pixelWidth / columns,
                (float)Camera.main.pixelHeight / rows
            );

            Vector3 newPosition = Camera.main.ScreenToWorldPoint(new Vector2
            {
                x = point.x * blockSize.x + blockSize.x / 2,
                y = point.y * blockSize.y + blockSize.y / 2,
            });

            return new Vector3
            {
                x = newPosition.x,
                y = newPosition.y,
                z = 0
            };
        }


        private static string ToFullPath(string relativePathInAssets)
        {
            string rel = relativePathInAssets.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                ? relativePathInAssets.Substring("Assets/".Length)
                : relativePathInAssets;

            return Path.Combine(Application.dataPath, rel);
        }

        private static string GetGuidForAsset(UnityEngine.Object obj)
        {
            if (obj == null) return null;

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.AssetPathToGUID(path);
#else
        return null; // En runtime no hay GUIDs; esto es uso de editor
#endif
        }

        private static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(guid)) return null;

#if UNITY_EDITOR
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
        }
    }

    [Serializable]
    public class SaveFileDTO
    {
        public int rows;
        public int cols;
        public string spritesGuid;
        public List<ColliderDataDTO> colliders;
        public List<CellDTO> cells;
    }

    [Serializable]
    public class CellDTO
    {
        public int y;
        public int x;
        public List<SubBlockDTO> blocks;

        public GridBlock ToGridBlock(GridSprites sprites, Dictionary<BlockType, GameObject> parents)
        {
            var gb = new GridBlock();

            gb.blocks = new();
            foreach (var sbdto in blocks)
            {
                var sb = sbdto.ToSubBlock(sprites, parents);
                gb.blocks.Add(sb);
            }
            return gb;
        }
    }

    [Serializable]
    public class ColliderDataDTO
    {
        public Vector2 size;
        public Vector2 position;
        public Vector2 offset;
        public List<Vector3Int> blocks;
        public bool isTrigger;
        public bool isHorizontal;
        public bool hasDirection;
        public int headIdx;
        public string name;
        public int uLayer;
        public int interaction;
        public string id;

        public ColliderDataDTO From(ColliderData cd, string key)
        {
            if (cd == null) return null;
            try
            {
                var dto = new ColliderDataDTO
                {
                    size = cd.collider != null ? cd.collider.size : Vector2.zero,
                    position = cd.collider != null ? cd.collider.transform.position : Vector2.zero,
                    offset = cd.collider.offset,
                    blocks = cd.blocks != null ? new List<Vector3Int>(cd.blocks.ConvertAll(b =>
                        new Vector3Int(b.Position.x, b.Position.y, b.Layer))) : new List<Vector3Int>(),
                    isTrigger = cd.collider != null && cd.collider.isTrigger,
                    isHorizontal = cd.isHorizontal ?? false,
                    hasDirection = cd.isHorizontal.HasValue,
                    headIdx = cd.headIdx,
                    name = cd.item.name,
                    uLayer = cd.collider != null ? cd.collider.gameObject.layer : 0,
                    id = key,

                };
                if (cd.collider.TryGetComponent<InteractableObject>(out var io))
                {
                    dto.interaction = (int)io.Interaction;
                }
                else
                    dto.interaction = 0;

                return dto;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ColliderData ToColliderData(GridBlock[,] grid, Action<GameObject> destructionCallback)
        {
            bool? horizontal = hasDirection ? isHorizontal : null;

            var cd = new ColliderData(name, null, horizontal, Vector2Int.zero, destructionCallback)
            {
                headIdx = headIdx
            };
            cd.collider.size = size;
            cd.collider.transform.position = position;
            cd.collider.isTrigger = isTrigger;
            cd.collider.gameObject.layer = uLayer;
            cd.blocks = blocks.Select(b => grid[b.y, b.x][b.z]).ToList();
            cd.collider.offset = offset;
            if (interaction != 0)
            {
                var io = cd.collider.gameObject.AddComponent<InteractableObject>();
                io.Interaction = (Interaction)interaction;
            }
            return cd;
        }
    }

    [Serializable]
    public class SubBlockDTO
    {
        public BlockDataDTO data;
        public string colliderId;
        public int layer;
        public bool horizontal;
        public bool hasDirection;
        public float objPosX;
        public float objPosY;

        public static SubBlockDTO From(SubBlock sb)
        {
            var dto = new SubBlockDTO
            {
                data = BlockDataDTO.From(sb.data),
                colliderId = sb.ColliderId,
                layer = sb.Layer,
                horizontal = sb.IsHorizontal ?? false,
                hasDirection = sb.IsHorizontal.HasValue,
                objPosX = sb.Block != null ? sb.Block.transform.position.x : 0f,
                objPosY = sb.Block != null ? sb.Block.transform.position.y : 0f,
            };
            return dto;
        }

        public SubBlock ToSubBlock(GridSprites sprites, Dictionary<BlockType, GameObject> parents)
        {
            var bd = data.ToBlockData();
            var sb = new SubBlockBuilder()
                .WithData(bd)
                .WithCollider(colliderId)
                .WithLayer(layer)
                .WithDirection(hasDirection ? horizontal : null)
                .WithObjectPosition(new Vector2Int((int)objPosX, (int)objPosY))
                .Build(sprites);

            sb.Block.transform.parent = parents[sb.data.Type]?.transform;
            return sb;
        }
    }

    [Serializable]
    public class BlockDataDTO
    {
        public SpriteDataDTO sprite;
        public int posY;
        public int posX;
        public bool hasIsHorizontal;
        public bool isHorizontal;
        public Vector2 colliderSize;
        public Vector2 colliderPosition;
        public Vector2 spriteOffset;

        public static BlockDataDTO From(BlockData bd)
        {
            var dto = new BlockDataDTO
            {
                sprite = SpriteDataDTO.From(bd.sprite),
                posY = bd.position.y,
                posX = bd.position.x,
                hasIsHorizontal = bd.isHorizontal.HasValue,
                isHorizontal = bd.isHorizontal ?? false,
                colliderSize = bd.colliderSize,
                colliderPosition = bd.colliderPosition,
                spriteOffset = bd.spriteOffset
            };
            return dto;
        }

        public BlockData ToBlockData()
        {
            var bd = new BlockData();
            bd.sprite = sprite?.ToSpriteData() ?? new SpriteData(ColorType.None, BlockType.None, Vector2Int.zero, -1);
            bd.position = (posY, posX);
            bd.isHorizontal = hasIsHorizontal ? isHorizontal : null;
            bd.colliderSize = colliderSize;
            bd.colliderPosition = colliderPosition;
            bd.spriteOffset = spriteOffset;
            return bd;
        }
    }

    [Serializable]
    public class SpriteDataDTO
    {
        public ColorType color;
        public BlockType type;
        public int idx;
        public Vector2Int amount;

        public static SpriteDataDTO From(SpriteData sd)
        {
            if (sd == null) return null;
            return new SpriteDataDTO
            {
                color = sd.color,
                type = sd.type,
                idx = sd.idx,
                amount = sd.amount
            };
        }

        public SpriteData ToSpriteData()
        {
            return new SpriteData(color, type, amount, idx);
        }
    }

    internal static class GridBlockExtensions
    {
        public static IEnumerable<SubBlock> GetBlocksSafe(this GridBlock gb)
        {
            return gb.blocks;
        }
    }

    internal class SubBlockBuilder
    {
        private BlockData data;
        private string collider;
        private int layer;
        private bool? horizontal;
        private Vector2Int objPos;

        public SubBlockBuilder WithData(BlockData d) { data = d; return this; }
        public SubBlockBuilder WithCollider(string c) { collider = c; return this; }
        public SubBlockBuilder WithLayer(int l) { layer = l; return this; }
        public SubBlockBuilder WithDirection(bool? b) { horizontal = collider == null ? null : b; return this; }
        public SubBlockBuilder WithObjectPosition(Vector2Int p) { objPos = p; return this; }

        public SubBlock Build(GridSprites sprites)
        {
            var idxPos = data.position;
            var sb = new SubBlock(idxPos.y, idxPos.x, layer);
            sb.data = data;
            sb.ColliderId = collider;
            sb.IsHorizontal = horizontal;
            sb.Sprite = data.Type != BlockType.None ? sprites.GetSprite(data.sprite.color, data.sprite.type, data.sprite.idx) : null;
            sb.Block.transform.position = new Vector3(objPos.x, objPos.y, 0f);
            sb.Block.name = $"Block{idxPos.y},{idxPos.x}_Layer{layer}";
            return sb;
        }
    }

}
