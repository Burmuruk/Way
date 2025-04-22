using System;
using System.Collections.Generic;
using UnityEngine;
using Xolito.Core;

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
        [SerializeField, Range(0, 9)] int curLayer;

        LevelController lvlController;

        const int INTERACTION_LAYER = 10;
        Vector2Int elementsPerUnit;
        int spriteIndex = 0;
        (int y, int x) currentCell = default;
        (float x, float y) currentOffset = default;
        (int y, int x, bool hasValue) lastCell = default;

        Block[,] grid;
        List<ColliderData> colliders = new List<ColliderData>();
        GameObject collidersParent;
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
            Get_Sprite();
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
                col.item.transform.position = col.Head.block.transform.position;

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
                        block.Collider = null;
                        continue;
                    }

                    blockSlices[i].Add(new Point(block.Position));
                    block.Collider = null;
                }

                Destroy(colliders[colliderIndex].item);
                colliders[colliderIndex] = null;
            }

            void Set_PositionToCollider(int colliderIdx, Point point)
            {
                var col = colliders[colliderIdx];
                col.item.transform.position = grid[point.y, point.x].block.transform.position;
            }
        }
        #endregion

        #region Private methods

        #region Inputs
        private void Painting_Blocks()
        {
            try
            {
                if (Input.GetKey(KeyCode.N) && Input.GetMouseButton(0))
                {
                    Draw_Line();
                }
                else if (Input.GetKey(KeyCode.C) && Input.GetMouseButton(0))
                {
                    Fill_Place();
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    Set_Piece();
                }
            }
            catch (IndexOutOfRangeException) { }
        }

        private void Change_BlockType()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                var values = Enum.GetValues(typeof(BlockType));
                var newValue = (int)type + 1;

                if (newValue > (int)values.GetValue(values.Length - 1))
                    newValue = 1;

                type = (BlockType)newValue;
                elementsPerUnit = sprites.GetSpriteAmount(color, type);
                Get_Sprite();
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                var values = Enum.GetValues(typeof(BlockType));
                var newValue = (int)type - 1;

                if (newValue < (int)values.GetValue(1))
                    newValue = values.Length - 1;

                type = (BlockType)newValue;
                elementsPerUnit = sprites.GetSpriteAmount(color, type);
                Get_Sprite();
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
                    Get_Sprite();
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

                Get_Sprite();
            }

            if (Input.GetMouseButtonDown(1))
            {
                Remove_Piece(curLayer);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                random = !random;
                OnRandomEnabled?.Invoke(random);
            }
        }
        #endregion

        private void Set_Piece()
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                grid[currentCell.y, currentCell.x][curLayer].Sprite = null;
                grid[currentCell.y, currentCell.x][curLayer].data.sprite = null;
                return;
            }

            Block cur = grid[currentCell.y, currentCell.x][curLayer];
            cur.Sprite = sprites.GetSprite(color, type, spriteIndex);
            cur.data.sprite = new SpriteData(color, type, elementsPerUnit);
            cur.block.transform.parent = parents[type].transform;
            cur.block.transform.position = spriteInMouse.transform.position;
            cur.data.Type = type;

            lastCell = (currentCell.y, currentCell.x, true);
            Get_Sprite();

            Set_Action(null, new Point(currentCell));
        }

        void Remove_Piece(int layer, Point? point = null)
        {
            var block = grid[currentCell.y, currentCell.x][layer];

            if (point.HasValue)
                block = grid[point.Value.y, point.Value.x][layer];

            try
            {
                block.Sprite = null;
                block.data.sprite = new SpriteData(color, type, elementsPerUnit);
                block.block.transform.parent = parents[BlockType.None].transform;
                Remove_Collider(new Point(block.Position.y, block.Position.x));
                lastCell.hasValue = false;
            }
            catch (IndexOutOfRangeException) { }
        }

        #region Interaction
        private void Set_Action(object args, params Point[] points)
        {
            switch (sprites.GetAction(color, type, spriteIndex))
            {
                case ActionType.None:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x];

                        if (block.Collider.HasValue)
                            Remove_Collider(new Point(block.Position));

                        Remove_Interactable(block);
                    }

                    break;

                case ActionType.Platform:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x][curLayer];

                        if (block.Collider.HasValue)
                            Remove_Collider(new Point(block.Position));

                        SetUp_Trigger(ref block, "Platform");

                        Remove_Interactable(block);
                    }

                    break;

                case ActionType.Collider:

                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x][curLayer];

                        Remove_Interactable(block);
                    }
                    Find_Pairs(points);
                    break;

                case ActionType.Coin:

                    SetUp_InteractableBlocks(points, Interaction.Coin, out List<(Block block, InteractableObject interact)> interactables);

                    foreach (var interactable in interactables)
                    {
                        int layer = curLayer;
                        interactable.interact.OnInteract += () => Remove_Piece(layer, new Point(interactable.block.Position));
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

                        if (block.Collider.HasValue)
                            Remove_Collider(new Point(block.Position));

                        Remove_Interactable(block);

                        FindObjectOfType<LevelController>().AddStartPoint(block.block, block.data.sprite.color);
                        var point = new Point(block.Position);

                        startPoints.Add((point, color));
                        lvlController.Set_StartPoint(block.block, color == ColorType.White, true);
                    }

                    Find_Pairs(points);
                    break;

                default:
                    break;
            }
        }

        void Remove_Interactable(Block block)
        {
            InteractableObject interactable;
            if (block.Collider.HasValue)
            {
                if (colliders[block.Collider.Value].collider.gameObject.TryGetComponent(out interactable))
                {
                    Destroy(interactable);
                }
            }
        }

        void SetUp_InteractableBlocks(Point[] points, Interaction interaction, out List<(Block, InteractableObject)> interactables)
        {
            interactables = null;

            for (int i = 0; i < points.Length; i++)
            {
                var block = grid[points[i].y, points[i].x][curLayer];

                if (block.Collider.HasValue)
                    Remove_Collider(new Point(block.Position));

                block.block.tag = "Untagged";
                SetUp_Trigger(ref block, INTERACTION_LAYER);

                Add_Interaction(block, interaction, out var interactable);
                (interactables??= new List<(Block, InteractableObject)>()).Add((block, interactable));
            }
        }

        void Add_Interaction(Block block, Interaction interaction, out InteractableObject interactable)
        {
            interactable = null;

            if (!block.Collider.HasValue) return;

            if (colliders[block.Collider.Value].collider.gameObject.TryGetComponent(out interactable))
            {
                interactable.Interaction = interaction;
            }
            else
            {
                interactable = colliders[block.Collider.Value].collider.gameObject.AddComponent<InteractableObject>();
                interactable.Interaction = interaction;
            }
        } 
        #endregion

        private void Fill_Place()
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                grid[currentCell.y, currentCell.x][curLayer].Sprite = null;
                return;
            }

            if (grid[currentCell.y, currentCell.x][curLayer].Sprite != null) return;

            Color_Place(currentCell.x, currentCell.y, null);
        }

        private void Draw_Line()
        {
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
                        grid[i, lastCell.x].Sprite = spriteInMouse.sprite;
                        grid[i, lastCell.x].data.sprite = new SpriteData(color, type, elementsPerUnit);
                        grid[i, lastCell.x].data.Type = type;
                        Get_Sprite();

                        pointsList.Add(new Point(i, lastCell.x));

                        if (toUp) ++i;
                        else --i;
                    }

                    int first = 0;

                    Find_Pairs(pointsList.ToArray(), toUp ? Directions.Down : Directions.Up, first);
                }
                else
                {
                    var toRitgh = lastCell.x <= currentCell.x ? true : false;
                    var (start, end) = toRitgh ? (points.start, points.end + 1) : (points.end, points.start - 1);

                    for (int i = start; i != end;)
                    {
                        grid[lastCell.y, i].Sprite = spriteInMouse.sprite;
                        grid[lastCell.y, i].data.sprite = new SpriteData(color, type, elementsPerUnit);
                        grid[lastCell.y, i].data.Type = type;
                        Get_Sprite();

                        pointsList.Add(new Point(lastCell.y, i));

                        if (toRitgh) ++i;
                        else --i;
                    }

                    int first = 0;

                    Find_Pairs(pointsList.ToArray(), toRitgh ? Directions.Left : Directions.Right, first);
                }

                lastCell = (currentCell.y, currentCell.x, true);
            }
        }

        private void Remove_Collider(in Point blockInd)
        {
            ref var block = ref grid[blockInd.y, blockInd.x];

            ReportPieceDeletion(in blockInd);

            if (!block.Collider.HasValue) return;

            if (colliders[block.Collider.Value].blocks.Count > 1)
                RemoveInList(blockInd, block.Collider.Value);
            else
            {
                Destroy(colliders[block.Collider.Value].item);
                colliders[block.Collider.Value] = null;
                block.Collider = null;
            }
        }

        private void ReportPieceDeletion(in Point startPoint)
        {
            int i = 0;

            foreach (var point in startPoints)
            {
                if (point.point == startPoint)
                {
                    var go = grid[startPoint.y, startPoint.x][curLayer].block;

                    lvlController.Set_StartPoint(go, point.color == ColorType.White, false);
                    startPoints.RemoveAt(i);

                    return;
                }
                i++;
            }
        }

        private void Color_Place(int y, int x, int? pd)
        {
            if (grid[y, x][curLayer].Sprite != null) return;

            grid[y, x][curLayer].Sprite = spriteInMouse.sprite;
            grid[y, x][curLayer].data.sprite = new SpriteData(color, type, elementsPerUnit);
            Get_Sprite();

            var directions = new[]
            {
                new { X = -1, Y = 0, Limit = 0 },
                new { X = 1, Y = 0, Limit = columns },
                new { X = 0, Y = -1, Limit = 0 },
                new { X = 0, Y = 1, Limit = rows },
            };
            int j = x, k = y;

            for (int i = 0; i < directions.Length; i++)
            {
                if (i == pd) continue;

                if (k + directions[i].Y == directions[i].Limit || j + directions[i].X == directions[i].Limit)
                    continue;

                Color_Place(k + directions[i].Y, j + directions[i].X, i);
            }
        }

        private void Find_Pairs(Point[] points, Directions direction = Directions.All, int firstIdx = 0)
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
            List< (bool isH, bool isAfter, Point first, Point[] points)> pointsData = new List<(bool isH, bool isAfter, Point first, Point[] points)>();

            Find_Colliders(ref firstP, pointsData);

            if (founded)
            {
                foreach (var data in pointsData)
                {
                    SetUp_Collider(data.isH, data.isAfter, data.first, data.points); 
                }
            }
            else
            {
                if (points.Length -1 > 1)
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

            void Find_Colliders(ref Point firstP, List<(bool isH, bool, Point, Point[])> pointsData)
            {
                for (int dirIdx = 0; dirIdx < directions.Length; dirIdx++)
                {
                    if ((direction & directions[dirIdx].Direction) == 0) continue;

                    if (firstP.y + directions[dirIdx].Y == directions[dirIdx].Limit || firstP.x + directions[dirIdx].X == directions[dirIdx].Limit)
                        continue;

                    ref var dir = ref directions[dirIdx];
                    ref var cur = ref grid[firstP.y + dir.Y, firstP.x + dir.X];

                    if (cur.Sprite && cur.data.sprite.type == BlockType.Ground)
                    {
                        if (cur.HasAnyCollider(out int layer).HasValue && (cur.data.isHorizontal.HasValue && cur.data.isHorizontal != dir.isH))
                            continue;

                        if (cur.Collider.HasValue && (colliders[cur.Collider.Value].IsTrigger || colliders[cur.Collider.Value].IsTrigger))
                            continue;

                        switch (dirIdx)
                        {
                            case 0:
                            case 1:

                                if (founded && Check_IsAfter(directions[dirIdx]))
                                {
                                    var newPoint = new Point(firstP.y + dir.Y, firstP.x + dir.X);
                                    var blockPoints = colliders[ grid[newPoint.y, newPoint.x].Collider.Value ].GetPoints();
                                    pointsData.Add((true, false, firstP, blockPoints));
                                }
                                else if (!founded)
                                    pointsData.Add((true, Check_IsAfter(directions[dirIdx]), new Point(firstP.y + dir.Y, firstP.x + dir.X), points));
                                goto default;

                            case 2:
                            case 3:

                                if (founded && Check_IsAfter(directions[dirIdx]))
                                {
                                    var newPoint = new Point(firstP.y + dir.Y, firstP.x + dir.X);
                                    var blockPoints = colliders[grid[newPoint.y, newPoint.x].Collider.Value].GetPoints();
                                    pointsData.Add((false, false, firstP, blockPoints));
                                }
                                else if (!founded)
                                    pointsData.Add((false, Check_IsAfter(directions[dirIdx]), new Point(firstP.y + dir.Y, firstP.x + dir.X), points));
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
            ref var cur = ref grid[first.y, first.x];

            if (!cur.Collider.HasValue)
            {
                var data = cur.data.sprite;
                col = Create_NewCollider(horizontal, cur, data.amount);
            }
            else
                col = colliders[cur.Collider.Value];

            cur.IsHorizontal = horizontal;

            if (atRight)
            {
                //InvertArrayOrder(ref blockSlices);
                
                if (points.Length > 0)
                    if (!horizontal.HasValue)
                    {
                        col.item.transform.position = grid[points[0].y, points[0].x].block.transform.position;
                        col.headIdx = 0;
                    }
                    else
                    {
                        var end = points.Length- 1;
                        col.item.transform.position = grid[points[end].y, points[end].x].block.transform.position;
                        col.headIdx = end;
                    }
                else
                {
                    col.item.transform.position = grid[first.y, first.x].block.transform.position;
                    col.headIdx = 0;
                }
            }
            //else if (blockPoints != null && blockPoints.Length > 0)
            //    col.item.transform.position = grid[blockPoints[0].y, blockPoints[0].x].block.transform.position;
            //else
            //    col.item.transform.position = cur.block.transform.position;

            Set_BlocksCollider(ref cur, col, atRight, points);
            Set_ColliderSize(horizontal, col);

        }

        void SetUp_Trigger(ref Block block, int layer, string tag = "Untagged")
        {
            SetUp_Trigger(ref block, tag);

            if (block.Collider.HasValue)
                colliders[block.Collider.Value].collider.gameObject.layer = layer;
        }

        void SetUp_Trigger(ref Block block, string tag = "Untagged")
        {
            var data = Create_NewCollider(true, block, block.data.sprite.amount);
            data.IsTrigger = true;
            data.Tag = tag;

            Set_BlocksCollider(ref block, data, true);
            Set_ColliderSize(true, data);
        }

        private void Set_BlocksCollider(ref Block cur, ColliderData data, bool atRight, params Point[] points)
        {
            foreach (var point in points)
            {
                ref var block = ref grid[point.y, point.x];

                if (block.Collider.HasValue)
                    Remove_Collider(point);

                block.Collider = cur.Collider.Value;
                block.data.isHorizontal = cur.data.isHorizontal;

                if (!atRight)
                    data.AddLast(block);
                else
                    data.AddFirst(block);
            }
        }

        private ColliderData Create_NewCollider(bool? horizontal, Block cur, Vector2Int elementsPerUnit)
        {
            ColliderData col;
            var data = new ColliderData($"Col{colliders.Count}", cur, horizontal, elementsPerUnit, Destroy);
            bool added = false;

            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] == null)
                {
                    colliders[i] = data;
                    cur.Collider = i;
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                colliders.Add(data);
                cur.Collider = colliders.Count - 1;
            }

            cur.data.isHorizontal = horizontal;
            col = colliders[cur.Collider.Value];

            col.collider.transform.parent = collidersParent.transform;
            col.item.transform.position = cur.block.transform.position;
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

        private void Change_Layer()
        {
            if (Input.GetKeyDown(KeyCode.Comma))
            {
                if (curLayer == Block.MAX_LAYERS)
                {
                    curLayer = 0;
                }
                else
                {
                    ++curLayer;
                }

                OnLayerChanged?.Invoke(curLayer);
            }
            else if (Input.GetKeyDown(KeyCode.Minus))
            {
                if (curLayer == 0)
                {
                    curLayer = Block.MAX_LAYERS;
                }
                else
                {
                    --curLayer;
                }

                OnLayerChanged?.Invoke(curLayer);
            }
        }

        private void Show_Cursor()
        {
            var countX = sprites.Get_SpriteOffset(color, type, spriteIndex).x;
            var countY = sprites.Get_SpriteOffset(color, type, spriteIndex).y;
            var (x, y) = (blockSize.x / (countX > 0 ? countX : 1), blockSize.y / (countY > 0 ? countY : 1));

            (int x, int y) cellPos = ((int)(Input.mousePosition.x / x), (int)(Input.mousePosition.y / y));

            var position = Camera.main.ScreenToWorldPoint(new Vector2
            {
                x = x * cellPos.x + x / 2,
                y = y * cellPos.y + y / 2,
            });

            currentCell = ((int)(Input.mousePosition.y / blockSize.y), (int)(Input.mousePosition.x / blockSize.x));

            spriteInMouse.transform.position = new Vector3
            {
                x = position.x,
                y = position.y,
                z = 0
            };
        }

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

            grid = new Block[rows, columns];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    grid[i, j] = new Block(i, j);
                    grid[i, j].block.transform.parent = parents[BlockType.None].transform;

                    grid[i, j].block.transform.position = ScreenToWorldPoint((i, j));
                    grid[i, j].renderer.size = spriteInMouse.size;
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

        private void Get_Sprite()
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
