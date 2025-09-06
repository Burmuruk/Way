using System;
using System.Collections.Generic;
using UnityEngine;
using Xolito.Core;
using static Unity.Collections.AllocatorManager;

namespace Xolito.Utilities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GridController : MonoBehaviour
    {
        #region Variables
        [Header("References")]
        [SerializeField] GridSprites sprites;
        [SerializeField] LevelDesign level;

        [Header("Settings")]
        [SerializeField] SpriteRenderer spriteInMouse;
        [SerializeField] int rows = 0;
        [SerializeField] int columns = 0;
        [SerializeField] bool drawLines = true;

        [Header("Edition")]
        [SerializeField] BlockType type;
        [SerializeField] ColorType color;
        [SerializeField] bool random = false;
        [SerializeField] bool showCursor = true;
        [SerializeField, Range(0, 9)] int curLayer = 1;

        [Header("Saving")]
        [SerializeField] string fileName = "grid";
        [SerializeField] bool save;
        [SerializeField] bool load;

        LevelController lvlController;

        const int INTERACTION_LAYER = 10;
        Vector2Int elementsPerUnit;
        int spriteIndex = 0;
        (int y, int x) currentCell = default;
        (float x, float y) currentOffset = default;
        (int y, int x, bool hasValue) lastCell = default;
        int lastLayer = 0;

        GridBlock[,] grid;
        List<ColliderData> colliders = new List<ColliderData>();
        GameObject collidersParent;
        GameObject blocksParent;
        List<(Point point, ColorType color)> startPoints;
        Vector2 blockSize = default;
        Vector2 blockExtends = default;
        Vector2 platformSize = default;

        Dictionary<BlockType, GameObject> parents;

        [Flags]
        public enum Directions
        {
            None = 0,
            Left = 1,
            Right = 1 << 1,
            Down = 1 << 2,
            Up = 1 << 3,
            Horizontal = Left | Right,
            Vertical = Down | Up,
            All = Left | Right | Down | Up,
        }

        #endregion

        #region Properties
        public event Action<bool> OnShowCursorEnabled;
        public event Action<bool> OnRandomEnabled;
        public event Action<bool> OnDrawLinesEnabled;
        public event Action<int> OnLayerChanged;

        public bool ShowCursor { get => showCursor; }
        public bool Random { get => random; }
        public bool DrawLines { get => drawLines; }
        #endregion

        #region Unity methods
        void Start()
        {
            blockSize = new Vector2(Camera.main.pixelWidth / columns, Camera.main.pixelHeight / rows);
            platformSize = new Vector2(Camera.main.pixelWidth / columns, Camera.main.pixelHeight / (rows * 2));
            blockExtends = new Vector2(blockSize.x / 2, blockSize.y / 2);
            lvlController = FindObjectOfType<LevelController>();
            startPoints = new List<(Point point, ColorType color)>();

            Initialize();
            Set_SpriteInMouse();
        }

        void Update()
        {
            Draw_Grid();
            Move_Players();
            Edit_Settings();

            if (!showCursor) return;

            Change_Layer();
            Show_Cursor();
            Painting_Blocks();

            Change_Sprite();
            Change_BlockType();
        }

        private void FixedUpdate()
        {
            if (save)
            {
                Save();
                save = false;
            }

            if (load)
            {
                Load();
                load = false;
            }
        }

        private void Move_Players()
        {
            if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                SleepPlayers(true);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                RespawnPlayers();
            }
        }

        private void RespawnPlayers()
        {
            lvlController.Restart_Players();
            Invoke("SleepPlayers", .1f);
        }

        private void SleepPlayers() => SleepPlayers(false);

        private void SleepPlayers(bool shouldAwake = false)
        {
            var rbs = FindObjectsOfType<Rigidbody2D>();

            foreach (var rb in rbs)
            {
                if (shouldAwake && rb.IsSleeping())
                    rb.WakeUp();
                else
                    rb.Sleep();
            }
        }

        #endregion

        #region Saving
        public void Save()
        {
            GridSaver saver = new GridSaver();
            saver.SaveToTxt(grid, colliders, sprites, "Assets/Data/" + fileName + ".json");
        }

        public void Load()
        {
            GridSaver saver = new GridSaver();
            (grid, colliders, sprites) = saver.LoadFromTxt("Assets/Data/" + fileName + ".json", Destroy, parents);
        }
        #endregion

        #region Public methods
        public void RemoveInList(in Point point, int colliderIndex)
        {
            if (colliders[colliderIndex].First.Position == (point.y, point.x))
            {
                colliders[colliderIndex].RemoveFirst();
                Resize_Collider(colliderIndex);
                return;
            }
            else if (colliders[colliderIndex].Last.Position == (point.y, point.x))
            {
                colliders[colliderIndex].RemoveLast();
                Resize_Collider(colliderIndex);
                return;
            }

            var blockSlices = new List<List<Point>>();
            var isH = colliders[colliderIndex].isHorizontal;

            Split_Collider(in point);

            for (int i = 0; i < blockSlices.Count; i++)
            {
                var first = blockSlices[i][0];
                blockSlices[i].RemoveAt(0);

                SetUp_Collider(isH, false, first, blockSlices[i].ToArray());
            }

            //Set_PositionToCollider(colliderIndex, blockSlices[0][0]);
            //Set_ColliderSize(colliders[colliderIndex].isHorizontal, colliders[colliderIndex]);

            //Point first = blockSlices[1][0];
            //blockSlices[1].RemoveAt(0);
            //bool isAfter = false;

            //if (colliders[colliderIndex].isHorizontal.Value && blockSlices[1][0].x > blockSlices[0][0].x)
            //{
            //    isAfter = true;
            //}
            //else if (!colliders[colliderIndex].isHorizontal.Value && blockSlices[1][0].y > blockSlices[0][0].y)
            //    isAfter = true;
            //else
            //    isAfter = false;

            //SetUp_Collider(colliders[colliderIndex].isHorizontal, isAfter, first, blockSlices[1].ToArray());

            void Resize_Collider(int index)
            {
                //List<Point> newPoints = new List<Point>();
                //foreach (var item in colliders[index].blocks)
                //    newPoints.Add(new Point(item.Position));

                //Point first = newPoints[0];
                //newPoints.RemoveAt(0);
                var col = colliders[index];
                col.item.transform.position = col.Head.Block.transform.position;

                Set_ColliderSize(col.isHorizontal, col);

                //SetUp_Collider(colliders[index].isHorizontal, false, first, newPoints.ToArray());
            }

            void Split_Collider(in Point point)
            {
                blockSlices.Add(new List<Point>());
                int i = 0;

                foreach (var block in colliders[colliderIndex].blocks)
                {
                    if (block.Position == (point.y, point.x))
                    {
                        blockSlices.Add(new List<Point>());
                        ++i;
                        block.collider = null;
                        continue;
                    }

                    blockSlices[i].Add(new Point(block.Position, block.Layer));
                    block.collider = null;
                }

                Destroy(colliders[colliderIndex].item);
                colliders[colliderIndex] = null;
            }

            void Set_PositionToCollider(int colliderIdx, Point point)
            {
                var col = colliders[colliderIdx];
                col.item.transform.position = grid[point.y, point.x].Position;
            }
        }
        #endregion

        #region Private methods

        #region Inputs
        private void Painting_Blocks()
        {
            try
            {
                if (Input.GetKey(KeyCode.L) && Input.GetMouseButton(0))
                {
                    if (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                        Draw_Line(null);
                    else
                        Draw_Line(curLayer);
                }
                else if (Input.GetKey(KeyCode.U) && Input.GetMouseButton(0))
                {
                    if (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                        Fill_Place(null);
                    else
                        Fill_Place(curLayer);
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    Set_Piece(currentCell);
                }
            }
            catch (IndexOutOfRangeException) { }
        }

        private void Change_BlockType()
        {
            if (Input.GetKeyDown(KeyCode.J) && !(Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)))
            {
                var values = Enum.GetValues(typeof(BlockType));
                var newValue = (int)type + 1;

                if (newValue > (int)values.GetValue(values.Length - 1))
                    newValue = 1;

                type = (BlockType)newValue;
                elementsPerUnit = sprites.GetSpriteAmount(color, type);
                Set_SpriteInMouse();
            }
            else if (Input.GetKeyDown(KeyCode.K) && !(Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)))
            {
                var values = Enum.GetValues(typeof(BlockType));
                var newValue = (int)type - 1;

                if (newValue < (int)values.GetValue(1))
                    newValue = values.Length - 1;

                type = (BlockType)newValue;
                elementsPerUnit = sprites.GetSpriteAmount(color, type);
                Set_SpriteInMouse();
            }
        }

        private void Change_Layer()
        {
            if (Input.GetKey(KeyCode.O))
            {
                if (curLayer != 0)
                {
                    lastLayer = curLayer;
                    curLayer = 0;
                    OnLayerChanged?.Invoke(curLayer);
                }
                return;
            }
            else if (curLayer == 0)
            {
                curLayer = lastLayer;
                OnLayerChanged?.Invoke(curLayer);
            }

            if (Input.GetKeyDown(KeyCode.K) && (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)))
            {
                if (curLayer >= GridBlock.MAX_LAYERS - 1)
                {
                    curLayer = 1;
                }
                else
                {
                    ++curLayer;
                }

                OnLayerChanged?.Invoke(curLayer);
            }
            if (Input.GetKeyDown(KeyCode.J) && (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)))
            {
                if (curLayer <= 1)
                {
                    curLayer = GridBlock.MAX_LAYERS - 1;
                }
                else
                {
                    --curLayer;
                }

                OnLayerChanged?.Invoke(curLayer);
            }
        }

        private void Change_Sprite()
        {
            if (Input.mouseScrollDelta.y > 0)
                Get_NextSprite(true);
            else if (Input.mouseScrollDelta.y < 0)
                Get_NextSprite(false);
        }

        private void Edit_Settings()
        {
            if (Input.GetKeyDown(KeyCode.P)) Initialize();

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (Cursor.lockState == CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.Confined;
                else if (Cursor.lockState == CursorLockMode.Confined)
                    Cursor.lockState = CursorLockMode.None;

                print(Cursor.lockState);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                var value = !showCursor;
                showCursor = value;

                if (value)
                    Set_SpriteInMouse();
                else
                    spriteInMouse.sprite = null;

                OnShowCursorEnabled?.Invoke(value);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                drawLines = !drawLines;
                OnDrawLinesEnabled?.Invoke(drawLines);
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (color != ColorType.White)
                    color = ColorType.White;
                else
                    color = ColorType.Black;

                Set_SpriteInMouse();
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (!Input.GetKey(KeyCode.RightAlt) && !Input.GetKey(KeyCode.AltGr))
                    Remove_Piece(curLayer);
                else
                    Remove_PieceAt(new Point(currentCell, curLayer));
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                random = !random;
                OnRandomEnabled?.Invoke(random);
            }
        }
        #endregion

        private void Set_Piece((int y, int x) point)
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                var block = grid[point.y, point.x][curLayer];
                block.Clear();

                if (block.collider.HasValue)
                {
                    Remove_Collider(new Point(block.Position, block.Layer));
                    Remove_Interactable(block);
                }

                return;
            }
            else if (type == BlockType.Ground && grid[point.y, point.x].ContainsBlockType(BlockType.Ground, out var layer))
                return;

            var lastType = grid[point.y, point.x][curLayer].data.Type;
            if (lastType != BlockType.None && lastType != type)
                return;

            var curBlock = grid[point.y, point.x][curLayer];

            Set_Sprite(curBlock, color, type, elementsPerUnit, spriteIndex);

            lastCell = (point.y, point.x, true);
            Set_SpriteInMouse();

            Set_Action(new Point(point, curLayer));
        }

        private void Set_Sprite(SubBlock block, SpriteData data)
        {
            Set_Sprite(block, data.color, data.type, data.amount, data.idx);
        }

        private void Set_Sprite(SubBlock block, ColorType color, BlockType type, Vector2Int amount, int spriteIdx)
        {
            block.Sprite = sprites.GetSprite(color, type, spriteIdx);
            block.data.sprite = new SpriteData(color, type, amount, spriteIdx);
            block.Block.transform.parent = parents[type].transform;
            block.data.Type = type;
            block.Block.transform.position = Get_SpritePosition(block, out var offset);
            block.data.spriteOffset = offset;
        }

        void Remove_Piece(int layer, Point? point = null)
        {
            try
            {
                var block = grid[currentCell.y, currentCell.x][layer];

                if (point.HasValue)
                    block = grid[point.Value.y, point.Value.x][layer];

                block.Sprite = null;
                block.data.sprite = new SpriteData(ColorType.None, BlockType.None, elementsPerUnit, -1);
                block.Block.transform.parent = parents[BlockType.None].transform;
                Remove_Collider(new Point(block.Position.y, block.Position.x, layer));
                lastCell.hasValue = false;
            }
            catch (IndexOutOfRangeException)
            {
                Set_CurrentCell();
                if (currentCell.y > 0 && currentCell.y < rows && currentCell.x > 0 && currentCell.y < columns)
                    Remove_Piece(layer, point);
            }
        }

        void Remove_PieceAt(Point? point)
        {
            int layer = 0;

            try
            {
                var gBlock = grid[currentCell.y, currentCell.x];

                if (gBlock.GetSpriteCloserTo(spriteInMouse.transform.position) is var l && !l.HasValue) return;

                layer = l.Value;
                var block = gBlock[layer];

                if (point.HasValue)
                    block = grid[point.Value.y, point.Value.x][layer];

                block.Sprite = null;
                block.data.sprite = new SpriteData(color, BlockType.None, elementsPerUnit, -1);
                block.Block.transform.parent = parents[BlockType.None].transform;
                Remove_Collider(new Point(block.Position.y, block.Position.x, layer));
                lastCell.hasValue = false;
            }
            catch (IndexOutOfRangeException)
            {
                Set_CurrentCell();
                if (currentCell.y > 0 && currentCell.y < rows && currentCell.x > 0 && currentCell.y < columns)
                    Remove_PieceAt(point);
            }
        }

        #region Interaction
        private void Set_Action(params Point[] points)
        {
            switch (sprites.GetAction(color, type, spriteIndex))
            {
                case ActionType.None:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x][points[i].layer];

                        if (block.collider.HasValue)
                            Remove_Collider(new Point(block.Position, block.Layer));

                        Remove_Interactable(block);
                    }

                    break;

                case ActionType.Platform:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x][curLayer];

                        if (block.collider.HasValue)
                            Remove_Collider(block);

                        SetUp_Trigger(block, "Platform");

                        Remove_Interactable(block);
                    }

                    break;

                case ActionType.Collider:

                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x][curLayer];

                        Remove_Interactable(block);
                    }
                    Find_Pairs(points, curLayer);
                    break;

                case ActionType.Coin:

                    SetUp_InteractableBlocks(points, Interaction.Coin, out List<(SubBlock block, InteractableObject interact)> interactables);

                    foreach (var interactable in interactables)
                    {
                        int layer = curLayer;
                        interactable.interact.OnInteract += () => Remove_Piece(layer, new Point(interactable.block.Position, interactable.block.Layer));
                    }

                    break;

                case ActionType.Enemy:

                    SetUp_InteractableBlocks(points, Interaction.Damage, out _);

                    break;

                case ActionType.FinalPoint:

                    SetUp_InteractableBlocks(points, Interaction.EndPoint, out _);

                    break;

                case ActionType.StartPoint:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x][curLayer];

                        if (block.collider.HasValue)
                            Remove_Collider(block);

                        Remove_Interactable(block);

                        FindObjectOfType<LevelController>().AddStartPoint(block.Block, block.data.sprite.color);
                        var point = new Point(block.Position, block.Layer);

                        startPoints.Add((point, color));
                        lvlController.Set_StartPoint(block.Block, color == ColorType.White, true);
                    }

                    Find_Pairs(points, curLayer);
                    break;

                default:
                    break;
            }
        }

        void Remove_Interactable(SubBlock block)
        {
            InteractableObject interactable;
            if (block.collider.HasValue)
            {
                if (colliders[block.collider.Value].collider.gameObject.TryGetComponent(out interactable))
                {
                    Destroy(interactable);
                }
            }
        }

        void SetUp_InteractableBlocks(Point[] points, Interaction interaction, out List<(SubBlock, InteractableObject)> interactables)
        {
            interactables = null;

            for (int i = 0; i < points.Length; i++)
            {
                var block = grid[points[i].y, points[i].x][curLayer];

                if (block.collider.HasValue)
                    Remove_Collider(block);

                block.Block.tag = "Untagged";
                SetUp_Trigger(block, INTERACTION_LAYER);

                Add_Interaction(block, interaction, out var interactable);
                (interactables ??= new List<(SubBlock, InteractableObject)>()).Add((block, interactable));
            }
        }

        void Add_Interaction(SubBlock block, Interaction interaction, out InteractableObject interactable)
        {
            interactable = null;

            if (!block.collider.HasValue) return;

            if (colliders[block.collider.Value].collider.gameObject.TryGetComponent(out interactable))
            {
                interactable.Interaction = interaction;
            }
            else
            {
                interactable = colliders[block.collider.Value].collider.gameObject.AddComponent<InteractableObject>();
                interactable.Interaction = interaction;
            }
        }
        #endregion

        private void Fill_Place(int? layer)
        {
            if (color == ColorType.None || type == BlockType.None)
                return;

            var block = grid[currentCell.y, currentCell.x][curLayer];
            var data = new SpriteData(block.data.sprite.color, block.data.sprite.type, block.data.sprite.amount, block.data.sprite.idx);

            if (block.data.sprite == new SpriteData(color, type, elementsPerUnit, spriteIndex)) return;

            Color_Place(currentCell.y, currentCell.x, layer, data);
        }

        private void Draw_Line(int? layer)
        {
            int curLayer = layer.HasValue ? layer.Value : this.curLayer;

            if (color == ColorType.None || type == BlockType.None)
            {
                grid[currentCell.y, currentCell.x][curLayer].Sprite = null;
                return;
            }

            if (lastCell.hasValue)
            {
                (int start, int end) points = default;
                bool isX = true;

                if (currentCell.x == lastCell.x && currentCell.y != lastCell.y)
                {
                    if (currentCell.y > lastCell.y)
                    {
                        points.end = currentCell.y;
                        points.start = lastCell.y + 1;
                    }
                    else
                    {
                        points.end = lastCell.y - 1;
                        points.start = currentCell.y;
                    }
                }
                else if (currentCell.y == lastCell.y && currentCell.x != lastCell.x)
                {
                    if (currentCell.x > lastCell.x)
                    {
                        points.end = currentCell.x;
                        points.start = lastCell.x + 1;
                    }
                    else
                    {
                        points.end = lastCell.x - 1;
                        points.start = currentCell.x;
                    }
                    isX = false;
                }
                else
                    return;

                List<Point> pointsList = new List<Point>();

                if (isX)
                {
                    var toUp = lastCell.y <= currentCell.y ? true : false;
                    var (start, end) = toUp ? (points.start, points.end + 1) : (points.end, points.start - 1);

                    for (int i = start; i != end;)
                    {
                        Get_LinePoints(i, lastCell.x, layer, ref pointsList);

                        if (toUp) ++i;
                        else --i;
                    }

                    int first = 0;

                    if (sprites.GetAction(color, type, spriteIndex) == ActionType.Collider)
                        Find_Pairs(pointsList.ToArray(), layer, toUp ? Directions.Down : Directions.Up, first);
                    else
                    {
                        Draw_Triggers(pointsList);
                    }
                }
                else
                {
                    var toRitgh = lastCell.x <= currentCell.x ? true : false;
                    var (start, end) = toRitgh ? (points.start, points.end + 1) : (points.end, points.start - 1);

                    for (int i = start; i != end;)
                    {
                        Get_LinePoints(lastCell.y, i, layer, ref pointsList);

                        if (toRitgh) ++i;
                        else --i;
                    }

                    int first = 0;

                    if (sprites.GetAction(color, type, spriteIndex) == ActionType.Collider)
                        Find_Pairs(pointsList.ToArray(), layer, toRitgh ? Directions.Left : Directions.Right, first);
                    else
                    {
                        Draw_Triggers(pointsList);
                    }
                }

                lastCell = (currentCell.y, currentCell.x, true);
            }
        }

        private void Draw_Triggers(List<Point> points)
        {
            var data = new SpriteData(color, type, elementsPerUnit, spriteIndex);

            foreach (var point in points)
            {
                (int y, int x, int l) = point;
                Set_Sprite(grid[y, x][l], data);
            }

            Set_Action(points.ToArray());
        }

        private void Get_LinePoints(int y, int x, int? layer, ref List<Point> pointsList)
        {
            int curLayer = layer.HasValue ? layer.Value : this.curLayer;

            if (grid[y, x].ContainsBlockType(type, out var newLayer))
            {
                if (layer.HasValue && newLayer != layer)
                {
                    Remove_Piece(newLayer, new Point(y, x, newLayer));
                }
                else if (!layer.HasValue)
                    curLayer = newLayer;
            }

            var block = grid[y, x][curLayer];
            Set_Sprite(block, color, type, elementsPerUnit, spriteIndex);

            Set_SpriteInMouse();

            pointsList.Add(new Point(y, x, curLayer));
        }

        private void Remove_Collider(SubBlock block) => Remove_Collider(new Point(block.Position, block.Layer));

        private void Remove_Collider(in Point blockIdx)
        {
            var block = grid[blockIdx.y, blockIdx.x][blockIdx.layer];

            ReportPieceDeletion(in blockIdx);

            if (!block.collider.HasValue) return;

            if (colliders[block.collider.Value].blocks.Count > 1)
                RemoveInList(blockIdx, block.collider.Value);
            else
            {
                Destroy(colliders[block.collider.Value].item);
                colliders[block.collider.Value] = null;
                block.collider = null;
            }
        }

        private void ReportPieceDeletion(in Point startPoint)
        {
            int i = 0;

            foreach (var point in startPoints)
            {
                if (point.point == startPoint)
                {
                    var go = grid[startPoint.y, startPoint.x][curLayer].Block;

                    lvlController.Set_StartPoint(go, point.color == ColorType.White, false);
                    startPoints.RemoveAt(i);

                    return;
                }
                i++;
            }
        }

        private void Color_Place(int y, int x, in int? layer, in SpriteData data)
        {
            var directions = new[]
            {
                new { X = -1, Y = 0, Limit = -1 },
                new { X = 1, Y = 0, Limit = columns },
                new { X = 0, Y = -1, Limit = -1 },
                new { X = 0, Y = 1, Limit = rows },
            };

            List<SubBlock> filled = new();
            LinkedList<(SubBlock block, int idx)> checkedBlocks = new();
            int index = 0;

            do
            {
            BlockCheck:
                SubBlock block = null;
                index = 0;

                if (Verify_Block(grid[y, x], in layer, in data, out block))
                {
                    checkedBlocks.AddLast((block, 0));
                    filled.Add(block);
                }
                else if (checkedBlocks.Count > 0)
                {
                    index = checkedBlocks.Last.Value.idx;
                    (y, x) = checkedBlocks.Last.Value.block.Position;
                }
                else
                    return;

                for (int i = index; i < directions.Length; i++)
                {
                    var lastValue = checkedBlocks.Last.Value;
                    checkedBlocks.Last.Value = (lastValue.block, i + 1);

                    Vector2Int newPos = new(x + directions[i].X, y + directions[i].Y);

                    if (newPos.y == directions[i].Limit || newPos.x == directions[i].Limit)
                        continue;

                    (y, x) = (newPos.y, newPos.x);
                    goto BlockCheck;
                }

                checkedBlocks.RemoveLast();

                if (checkedBlocks.Count > 0)
                    (y, x) = checkedBlocks.Last.Value.block.Position;

            } while (checkedBlocks.Count > 0);

            foreach (var subBlock in filled)
            {
                Set_Sprite(subBlock, new SpriteData(color, type, elementsPerUnit, Get_RandomSpriteIdx(color, type)));
            }
        }

        private bool Verify_Block(GridBlock gBlock, in int? layer, in SpriteData data, out SubBlock block)
        {
            block = null;
            if (layer.HasValue)
            {
                if (gBlock[layer.Value].data.sprite != data) return false;

                block = gBlock[layer.Value];
                Fill_Block(block, new Point(block.Position, layer.Value));
            }
            else
            {
                if (gBlock.ContainsBlockType(data.type, out int idx)
                    && gBlock[idx].data.sprite == data)
                {
                    block = gBlock[idx];
                    Fill_Block(block, new Point(block.Position, idx));
                }
                else
                    return false;
            }

            return true;
        }

        private void Fill_Block(SubBlock block, Point position)
        {
            Remove_Piece(position.layer, position);
            Set_Sprite(block, new SpriteData(color, type, elementsPerUnit, spriteIndex));
        }

        private void Find_Pairs(Point[] points, int? layer, Directions direction = Directions.All, int firstIdx = 0)
        {
            if (type == BlockType.Platform || type == BlockType.Enemies || points == null) return;

            ref Point firstP = ref points[firstIdx];
            var directions = new[]
            {
                new { X = -1, Y = 0, isH = true, Limit = -1, Direction = Directions.Left},
                new { X = 1, Y = 0, isH = true, Limit = columns, Direction = Directions.Right},
                new { X = 0, Y = -1, isH = false, Limit = -1, Direction = Directions.Down},
                new { X = 0, Y = 1, isH = false, Limit = rows, Direction = Directions.Up},
            };
            bool founded = false;
            List<(bool isH, bool isAfter, Point first, Point[] points)> pointsData = new List<(bool isH, bool isAfter, Point first, Point[] points)>();

            Find_Colliders(ref firstP, pointsData, layer.HasValue);

            if (founded)
            {
                foreach (var data in pointsData)
                {
                    SetUp_Collider(data.isH, data.isAfter, data.first, data.points);
                }
            }
            else
            {
                if (points.Length - 1 > 0)
                {
                    Point[] lastBlocks = new Point[points.Length - 1];

                    for (int i = 0; i < lastBlocks.Length; i++)
                    {
                        lastBlocks[i] = points[i + 1];
                    }

                    bool? isH = null;

                    if (points[0].x == points[1].x)
                        isH = false;
                    else if (points[0].y == points[1].y)
                        isH = true;

                    bool isAfter = direction.HasFlag(Directions.Right) || direction.HasFlag(Directions.Up) ? true : false;
                    SetUp_Collider(isH, isAfter, firstP, lastBlocks);
                }
                else
                    SetUp_Collider(null, true, firstP);
            }

            void Find_Colliders(ref Point firstP, List<(bool isH, bool, Point, Point[])> pointsData, bool ignoreLayer)
            {
                for (int dirIdx = 0; dirIdx < directions.Length; dirIdx++)
                {
                    if ((direction & directions[dirIdx].Direction) == 0) continue;

                    if (firstP.y + directions[dirIdx].Y == directions[dirIdx].Limit || firstP.x + directions[dirIdx].X == directions[dirIdx].Limit)
                        continue;

                    ref var dir = ref directions[dirIdx];
                    var newPoint = new Point(firstP.y + dir.Y, firstP.x + dir.X, firstP.layer);
                    ref var curGBlock = ref grid[newPoint.y, newPoint.x];

                    if (curGBlock.ContainsBlockType(type, out var idx) && curGBlock[idx].Sprite)
                    {
                        SubBlock curBlock = curGBlock[idx];

                        if (ignoreLayer && firstP.layer != idx)
                        {
                            Set_Sprite(curGBlock[firstP.layer], curGBlock[idx].data.sprite);
                            Remove_Piece(idx, newPoint);
                        }
                        else
                            newPoint = new Point(newPoint.y, newPoint.x, idx);

                        if (curBlock.collider.HasValue && (curBlock.data.isHorizontal.HasValue && curBlock.data.isHorizontal != dir.isH))
                            continue;

                        if (curBlock.collider.HasValue && (colliders[curBlock.collider.Value].IsTrigger || colliders[curBlock.collider.Value].IsTrigger))
                            continue;

                        switch (dirIdx)
                        {
                            case 0:
                            case 1:

                                if (founded && Check_IsAfter(directions[dirIdx]))
                                {
                                    var blockPoints = colliders[curBlock.collider.Value].GetPoints();
                                    pointsData.Add((true, false, firstP, blockPoints));
                                }
                                else if (!founded)
                                    pointsData.Add((true, Check_IsAfter(directions[dirIdx]), newPoint, points));
                                goto default;

                            case 2:
                            case 3:

                                if (founded && Check_IsAfter(directions[dirIdx]))
                                {
                                    var blockPoints = colliders[curBlock.collider.Value].GetPoints();
                                    pointsData.Add((false, false, firstP, blockPoints));
                                }
                                else if (!founded)
                                    pointsData.Add((false, Check_IsAfter(directions[dirIdx]), newPoint, points));
                                goto default;

                            default:

                                founded = true;
                                //dirIdx = directions.Length;
                                //return;
                                break;
                        }
                    }
                }
            }

            bool Check_IsAfter(dynamic id) => id.X + id.Y > 0 ? true : false;
        }

        private void SetUp_Collider(bool? horizontal, bool atRight, Point first, params Point[] points)
        {
            if (points == null) points = new Point[0];

            ColliderData col = null;
            var firstBlock = grid[first.y, first.x][first.layer];

            if (!firstBlock.collider.HasValue)
            {
                var amount = firstBlock.data.sprite != null ? firstBlock.data.sprite.amount : Vector2Int.zero;
                col = Create_NewCollider(horizontal, firstBlock, amount);
            }
            else
                col = colliders[firstBlock.collider.Value];

            firstBlock.IsHorizontal = horizontal;

            if (atRight)
            {
                //InvertArrayOrder(ref blockSlices);

                if (points.Length > 0)
                    if (!horizontal.HasValue)
                    {
                        col.item.transform.position = grid[points[0].y, points[0].x].Position;
                        col.headIdx = 0;
                    }
                    else
                    {
                        var end = points.Length - 1;
                        col.item.transform.position = grid[points[end].y, points[end].x].Position;
                        col.headIdx = end;
                    }
                else
                {
                    col.item.transform.position = grid[first.y, first.x].Position;
                    col.headIdx = 0;
                }
            }
            //else if (blockPoints != null && blockPoints.Length > 0)
            //    col.item.transform.position = grid[blockPoints[0].y, blockPoints[0].x].block.transform.position;
            //else
            //    col.item.transform.position = cur.block.transform.position;

            Set_BlocksCollider(firstBlock, col, atRight, points);
            Set_ColliderSize(horizontal, col);

        }

        void SetUp_Trigger(SubBlock block, int unityLayer, string tag = "Untagged")
        {
            SetUp_Trigger(block, tag);

            if (block.collider.HasValue)
                colliders[block.collider.Value].collider.gameObject.layer = unityLayer;
        }

        void SetUp_Trigger(SubBlock block, string tag = "Untagged")
        {
            var data = Create_NewCollider(true, block, block.data.sprite.amount);
            data.IsTrigger = true;
            data.Tag = tag;

            Set_BlocksCollider(block, data, true);
            Set_ColliderSize(true, data);
        }

        private void Set_BlocksCollider(SubBlock cur, ColliderData data, bool atRight, params Point[] points)
        {
            foreach (var point in points)
            {
                ref var gBlock = ref grid[point.y, point.x];
                SubBlock block = null;

                if (gBlock.ContainsBlockType(BlockType.Ground, out var layer))
                {
                    Remove_Collider(gBlock[layer]);
                    block = gBlock[layer];
                }
                else
                {
                    block = gBlock[cur.Layer];

                    if (block.collider.HasValue)
                        Remove_Collider(block);
                }

                block.collider = cur.collider.Value;
                block.data.isHorizontal = cur.data.isHorizontal;

                if (!atRight)
                    data.AddLast(block);
                else
                    data.AddFirst(block);
            }
        }

        private ColliderData Create_NewCollider(bool? horizontal, SubBlock block, Vector2Int elementsPerUnit)
        {
            var curBlock = block;
            ColliderData col;
            var data = new ColliderData($"Col{colliders.Count}", block, horizontal, elementsPerUnit, Destroy);
            bool added = false;

            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] == null)
                {
                    colliders[i] = data;
                    curBlock.collider = i;
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                colliders.Add(data);
                curBlock.collider = colliders.Count - 1;
            }

            curBlock.data.isHorizontal = horizontal;
            col = colliders[curBlock.collider.Value];

            col.collider.transform.parent = collidersParent.transform;
            col.item.transform.position = curBlock.Block.transform.position;
            return col;
        }

        void Set_ColliderSize(bool? horizontal, ColliderData col)
        {
            var amountPerUnit = new Vector2Int()
            {
                x = col.ElementsPerUnit.x <= 0 ? 1 : col.ElementsPerUnit.x,
                y = col.ElementsPerUnit.y <= 0 ? 1 : col.ElementsPerUnit.y
            };

            if (!horizontal.HasValue || horizontal.Value)
            {
                col.collider.offset = new Vector2(.445f * (col.blocks.Count - 1), 0);
                col.collider.size = new Vector2(.89f * col.blocks.Count / amountPerUnit.x, .83f / amountPerUnit.y);
            }
            else
            {
                col.collider.offset = new Vector2(0, .42f * (col.blocks.Count - 1));
                col.collider.size = new Vector2(.89f / amountPerUnit.x, .83f * col.blocks.Count / amountPerUnit.y);
            }
        }

        private void InvertArrayOrder(ref Point[] points)
        {
            var values = (Point[])points.Clone();
            points = new Point[values.Length];

            for (int i = values.Length - 1, j = 0; i >= 0; i--, j++)
            {
                points[j] = values[i];
            }
        }

        private void Show_Cursor()
        {
            var (countX, countY) = sprites.Get_SpriteOffset(color, type, spriteIndex);
            var (x, y) = (blockSize.x / (countX > 0 ? countX : 1), blockSize.y / (countY > 0 ? countY : 1));

            (int x, int y) cellPos = ((int)(Input.mousePosition.x / x), (int)(Input.mousePosition.y / y));

            var position = Camera.main.ScreenToWorldPoint(new Vector2
            {
                x = x * cellPos.x + x / 2,
                y = y * cellPos.y + y / 2,
            });

            Set_CurrentCell();

            spriteInMouse.transform.position = new Vector3
            {
                x = position.x,
                y = position.y,
                z = 0
            };
        }

        private Vector3 Get_SpritePosition(SubBlock block, out Vector2Int offset)
        {
            var gridPosition = new Vector2(block.Position.x, block.Position.y);
            var (countX, countY) = sprites.Get_SpriteOffset(block.data.sprite.color, block.data.sprite.type, block.data.sprite.idx);
            Vector2Int amount = new(countX > 0 ? countX : 1, countY > 0 ? countY : 1);
            var (sizeX, sizeY) = (blockSize.x / amount.x, blockSize.y / amount.y);

            Vector2Int pos = new ((int)(Input.mousePosition.x / blockSize.x), (int)(Input.mousePosition.y / blockSize.y));
            Vector2Int PosOffset = new ((int)(Input.mousePosition.x / sizeX), (int)(Input.mousePosition.y / sizeY));
            offset = new (PosOffset.x - (pos.x * amount.x), PosOffset.y - (pos.y * amount.y));

            var position = Camera.main.ScreenToWorldPoint(new Vector2
            {
                x = sizeX * ((gridPosition.x * amount.x) + offset.x) + sizeX / 2,
                y = sizeY * ((gridPosition.y * amount.y) + offset.y) + sizeY / 2,
            });

            Set_CurrentCell();

            return new Vector3
            {
                x = position.x,
                y = position.y,
                z = 0
            };
        }

        private void Set_CurrentCell() =>
            currentCell = ((int)(Input.mousePosition.y / blockSize.y), (int)(Input.mousePosition.x / blockSize.x));

        private void Get_NextSprite(bool isForward)
        {
            if (color == ColorType.None || type == BlockType.None) return;

            var index = 0;
            if (isForward)
            {
                index = spriteIndex + 1;
                if (index >= sprites.Get_SpritesCount(color, type))
                    index = 0;
            }
            else
            {
                index = spriteIndex - 1;
                if (index < 0)
                    index = sprites.Get_SpritesCount(color, type) - 1;
            }

            spriteInMouse.sprite = sprites.GetSprite(color, type, index);
            spriteIndex = index;
        }

        private void Initialize()
        {
            parents = new Dictionary<BlockType, GameObject>();
            var values = System.Enum.GetValues(typeof(BlockType));

            foreach (var value in values)
            {
                BlockType type = (BlockType)value;

                var parent = new GameObject(type.ToString());
                parent.transform.parent = transform;

                parents.Add(type, parent);
            }

            collidersParent = new GameObject("Colliders");
            collidersParent.transform.parent = transform;
            blocksParent = new GameObject("Blocks");
            blocksParent.transform.parent = transform;

            curLayer = 1;
            lastLayer = curLayer;
            grid = new GridBlock[rows, columns];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var pos = ScreenToWorldPoint((i, j));
                    var size = spriteInMouse.size;

                    grid[i, j] = new GridBlock(i, j, pos, size);
                    grid[i, j].SetParent(parents[BlockType.None].transform);
                }
            }
        }

        private Vector3 ScreenToWorldPoint((int y, int x) point)
        {
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(new Vector2
            {
                x = point.x * blockSize.x + blockExtends.x,
                y = point.y * blockSize.y + blockExtends.y,
            });

            return new Vector3
            {
                x = newPosition.x,
                y = newPosition.y,
                z = 0
            };
        }

        private void Set_SpriteInMouse()
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                spriteInMouse.sprite = null;
                return;
            }

            if (random)
            {
                var rand = new System.Random();
                var value = rand.Next(sprites.Get_SpritesCount(color, type));
                spriteInMouse.sprite = sprites.GetSprite(color, type, value);
                spriteIndex = value;
            }
            else
            {
                if (spriteIndex >= sprites.Get_SpritesCount(color, type))
                    spriteIndex = 0;

                spriteInMouse.sprite = sprites.GetSprite(color, type, spriteIndex);
            }
        }

        private int Get_RandomSpriteIdx(ColorType color, BlockType type)
        {
            var rand = new System.Random();
            return rand.Next(sprites.Get_SpritesCount(color, type));
        }

        private void Draw_Grid()
        {
            if (drawLines)
            {
                for (int i = 1; i < rows; i++)
                {
                    Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(0, blockSize.y * i, 58f)),
                        Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, blockSize.y * i, 58f)), Color.red);
                }

                for (int i = 1; i < columns; i++)
                {
                    Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(blockSize.x * i, 0, 58f)),
                        Camera.main.ScreenToWorldPoint(new Vector3(blockSize.x * i, Camera.main.pixelHeight, 58f)), Color.red);
                }
            }
        }
        #endregion
    }
}
