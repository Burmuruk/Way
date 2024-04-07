using System;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.UIElements;
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

        int spriteIndex = 0;
        (int y, int x) currentCell = default;
        (float x, float y) currentOffset = default;
        (int y, int x, bool hasValue) lastCell = default;

        Block[,] grid;
        List<ColliderData> colliders = new List<ColliderData>();
        GameObject collidersParent;
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
        public class Block
        {
            public GameObject block;
            public GameObject backGround;
            public SpriteRenderer renderer;
            public SpriteRenderer bRenderer;
            public BlockData data;
            public int? collider = null;

            public Sprite Sprite { get => renderer.sprite; set => renderer.sprite = value; }
            public Sprite BSprite
            {
                get => bRenderer?.sprite;
                set
                {
                    if (backGround is null)
                    {
                        backGround = new GameObject(block.name + " Back", typeof(SpriteRenderer));
                        backGround.transform.position = block.transform.position;
                        bRenderer = backGround.GetComponent<SpriteRenderer>();
                    }

                    if (value is null)
                        backGround = null;
                    else
                        bRenderer.sprite = value;
                }
            }
            public bool? IsHorizontal { get => data.isHorizontal; set => data.isHorizontal = value; }
            public int? Collider
            {
                get => collider;
                set
                {
                    collider = value;
                    data.colliderPosition = new Vector2
                    {
                        x = Int32.Parse(block.name.Substring(5, 1)),
                        y = Int32.Parse(block.name.Substring(5, 1)),
                    };
                }
            }
            public (int y, int x) Position { get => data.position; }

            public static bool operator true(Block block) => block != null;
            public static bool operator false(Block block) => block == null;
            public static bool operator !(Block block)
            {
                if (block != null)
                    return true;
                else
                    return false;
            }

            public Block(int row, int column)
            {
                block = new GameObject($"Block{row},{column}", typeof(SpriteRenderer));
                renderer = block.GetComponent<SpriteRenderer>();
                data = new BlockData((row, column));
            }
        }
        public class ColliderData
        {
            public GameObject item;
            public List<Block> blocks;
            public BoxCollider2D collider;
            public bool? isHorizontal = null;
            public int headIdx = 0;
            private bool isTrigger = false;

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

            public ColliderData(string name, Block block, bool? isHorizontal)
            {
                this.item = new GameObject(name, typeof(BoxCollider2D));
                collider = item.GetComponent<BoxCollider2D>();
                blocks = new List<Block>();
                this.isHorizontal = isHorizontal;

                AddFirst(block);
            }

            public void AddFirst(Block block) => blocks.Insert(0, block);
            public void AddLast(Block block) => blocks.Insert(blocks.Count, block);
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
                Destroy(item);
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
        public struct Point
        {
            public int x;
            public int y;

            public Point(int y, int x) => (this.y, this.x) = (y, x);

            public Point(Vector2 vector)
            {
                x = (int)vector.x;
                y = (int)vector.y;
            }

            public Point((int y, int x) point) => (y, x) = point;
        }

        #endregion

        #region Unity methods
        void Start()
        {
            blockSize = new Vector2(Camera.main.pixelWidth / columns, Camera.main.pixelHeight / rows);
            platformSize = new Vector2(Camera.main.pixelWidth / columns, Camera.main.pixelHeight / (rows * 2));
            blockExtends = new Vector2(blockSize.x / 2, blockSize.y / 2);

            Initialize();
            Get_Sprite();
        }

        void Update()
        {
            Draw_Grid();
            Move_Players();
            Edit_Settings();

            if (!showCursor) return;

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
            var lvlController = FindObjectOfType<LevelController>();
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
                Get_Sprite();
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                var values = Enum.GetValues(typeof(BlockType));
                var newValue = (int)type - 1;

                if (newValue < (int)values.GetValue(1))
                    newValue = values.Length - 1;

                type = (BlockType)newValue;
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
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                drawLines = !drawLines;
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
                try
                {
                    grid[currentCell.y, currentCell.x].Sprite = null;
                    grid[currentCell.y, currentCell.x].data.sprite = new SpriteData(color, type);
                    grid[currentCell.y, currentCell.x].block.transform.parent = parents[BlockType.None].transform;
                    Remove_Collider(new Point(currentCell.y, currentCell.x));
                    lastCell.hasValue = false;
                }
                catch (IndexOutOfRangeException) { }
            }

            if (Input.GetKeyDown(KeyCode.R)) random = !random;
        }
        #endregion

        private void Set_Piece()
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                grid[currentCell.y, currentCell.x].Sprite = null;
                grid[currentCell.y, currentCell.x].data.sprite = null;
                return;
            }

            var cur = grid[currentCell.y, currentCell.x];
            cur.Sprite = sprites.GetSprite(color, type, spriteIndex);
            cur.data.sprite = new SpriteData(color, type);
            cur.block.transform.parent = parents[type].transform;
            cur.block.transform.position = spriteInMouse.transform.position;
            cur.data.Type = type;

            lastCell = (currentCell.y, currentCell.x, true);
            Get_Sprite();

            Set_Action(null, new Point(currentCell));
        }

        private void Set_Action(object args, params Point[] points)
        {
            switch (sprites.GetAction(color, type, spriteIndex))
            {
                case ActionType.None:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x];

                        if (block.Collider.HasValue && block.data.Type != type)
                            Remove_Collider(new Point(block.Position));

                        block.block.tag = "Untagged";
                        Remove_Interactable(block.block);
                    }

                    break;

                case ActionType.Platform:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x];

                        if (block.Collider.HasValue && block.data.Type != type)
                            Remove_Collider(new Point(block.Position));

                        block.block.tag = "Platform";
                        block.block.AddComponent<BoxCollider2D>().isTrigger = true;

                        Remove_Interactable(block.block);
                    }

                    break;

                case ActionType.Collider:

                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x];

                        //if (block.data.Type != type)
                        //    Remove_Collider(new Point(block.Position));

                        block.block.tag = "Untagged";
                        Remove_Interactable(block.block);
                    }
                    Find_Pairs(points);
                    break;

                //case ActionType.Coin:

                //    if (block.Collider.HasValue && block.data.Type != type)
                //        Remove_Collider(new Point(block.Position));

                //    block.block.AddComponent<InteractableObject>().Interaction = Interaction.Coin;
                //    block.block.AddComponent<BoxCollider2D>().isTrigger = true;

                //    break;

                //case ActionType.Enemy:

                //    if (block.Collider.HasValue && block.data.Type != type)
                //        Remove_Collider(new Point(block.Position));

                //    block.block.AddComponent<InteractableObject>().Interaction = Interaction.Damage;
                //    block.block.AddComponent<BoxCollider2D>().isTrigger = true;

                //    break;

                case ActionType.FinalPoint:

                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x];

                        if (block.Collider.HasValue && block.data.Type != type)
                            Remove_Collider(new Point(block.Position));

                        SetpUp_Trigger(ref block);
                        block.block.tag = "Untagged";

                        FindObjectOfType<LevelController>().AddStartPoint(block.block, block.data.sprite.color);

                        InteractableObject interactable;
                        if (block.block.TryGetComponent(out interactable))
                        {
                            interactable.Interaction = Interaction.EndPoint;
                        }
                        else
                            block.block.AddComponent<InteractableObject>().Interaction = Interaction.EndPoint;
                    }

                    break;

                case ActionType.StartPoint:
                    for (int i = 0; i < points.Length; i++)
                    {
                        var block = grid[points[i].y, points[i].x];

                        if (block.Collider.HasValue && block.data.Type != type)
                            Remove_Collider(new Point(block.Position));

                        block.block.tag = "Untagged";
                        Remove_Interactable(block.block);

                        FindObjectOfType<LevelController>().AddStartPoint(block.block, block.data.sprite.color);
                    }

                    Find_Pairs(points);
                    break;

                default:
                    break;
            }

            void Remove_Interactable(GameObject obj)
            {
                InteractableObject interactable;
                if (obj.TryGetComponent(out interactable))
                {
                    Destroy(interactable);
                }
            }
        }

        private void Fill_Place()
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                grid[currentCell.y, currentCell.x].Sprite = null;
                return;
            }

            if (grid[currentCell.y, currentCell.x].Sprite != null) return;

            Color_Place(currentCell.x, currentCell.y, null);
        }

        private void Draw_Line()
        {
            if (color == ColorType.None || type == BlockType.None)
            {
                grid[currentCell.y, currentCell.x].Sprite = null;
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
                        grid[i, lastCell.x].data.sprite = new SpriteData(color, type);
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
                        grid[lastCell.y, i].data.sprite = new SpriteData(color, type);
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

            if (!block.Collider.HasValue) return;

            if (colliders[block.Collider.Value].blocks.Count > 1)
                RemoveInList(blockInd, block.Collider.Value);
            else
            {
                Destroy(colliders[block.Collider.Value].item);
                colliders[block.collider.Value] = null;
                block.collider = null;
            }
        }

        private void Color_Place(int y, int x, int? pd)
        {
            if (grid[y, x].Sprite != null) return;

            grid[y, x].Sprite = spriteInMouse.sprite;
            grid[y, x].data.sprite = new SpriteData(color, type);
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

                    if (cur.Sprite && cur.data.sprite.type != BlockType.Platform && cur.data.sprite.type != BlockType.Enemies)
                    {
                        if (cur.collider.HasValue && (cur.data.isHorizontal.HasValue && cur.data.isHorizontal != dir.isH))
                            continue;

                        if (colliders[cur.collider.Value].IsTrigger || colliders[cur.collider.Value].IsTrigger)
                            continue;

                        switch (dirIdx)
                        {
                            case 0:
                            case 1:

                                if (founded && Check_IsAfter(directions[dirIdx]))
                                {
                                    var newPoint = new Point(firstP.y + dir.Y, firstP.x + dir.X);
                                    var blockPoints = colliders[ grid[newPoint.y, newPoint.x].collider.Value ].GetPoints();
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
                                    var blockPoints = colliders[grid[newPoint.y, newPoint.x].collider.Value].GetPoints();
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

            if (!cur.collider.HasValue)
            {
                col = Create_NewCollider(horizontal, cur);
            }
            else
                col = colliders[cur.collider.Value];

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

        void SetpUp_Trigger(ref Block block)
        {
            var data = Create_NewCollider(true, block);
            data.IsTrigger = true;

            Set_BlocksCollider(ref block, data, true);
            Set_ColliderSize(true, data);
        }

        private void Set_BlocksCollider(ref Block cur, ColliderData data, bool atRight, params Point[] points)
        {
            foreach (var point in points)
            {
                ref var block = ref grid[point.y, point.x];

                if (block.collider.HasValue)
                    Remove_Collider(point);

                block.Collider = cur.collider.Value;
                block.data.isHorizontal = cur.data.isHorizontal;

                if (!atRight)
                    data.AddLast(block);
                else
                    data.AddFirst(block);
            }
        }

        private ColliderData Create_NewCollider(bool? horizontal, Block cur)
        {
            ColliderData col;
            var data = new ColliderData($"Col{colliders.Count}", cur, horizontal);
            bool added = false;

            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] == null)
                {
                    colliders[i] = data;
                    cur.collider = i;
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                colliders.Add(data);
                cur.collider = colliders.Count - 1;
            }

            cur.data.isHorizontal = horizontal;
            col = colliders[cur.collider.Value];

            col.collider.transform.parent = collidersParent.transform;
            col.item.transform.position = cur.block.transform.position;
            return col;
        }

        void Set_ColliderSize(bool? horizontal, ColliderData col)
        {
            if (!horizontal.HasValue || horizontal.Value)
            {
                col.collider.offset = new Vector2(.445f * (col.blocks.Count - 1), 0);
                col.collider.size = new Vector2(.89f * col.blocks.Count, .83f);
            }
            else
            {
                col.collider.offset = new Vector2(0, .42f * (col.blocks.Count - 1));
                col.collider.size = new Vector2(.89f, .83f * col.blocks.Count);
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
