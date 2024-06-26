﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Xolito.Utilities
{
    [Serializable]
    public class SpritesType
    {
        public List<Sprite> sprites;
        public BlockType type;
        public ActionType action;
        public Vector2Int amount;

        public Sprite this [int index] { get => sprites[index]; }
    }

    public enum ColorType
    {
        None,
        Black,
        White
    }

    public enum BlockType
    {
        None,
        Platform,
        Ground,
        Corner,
        Special,
        Final,
        Enemies,
        Background,
        Collectables
    }

    public enum ActionType
    {
        None,
        Platform,
        Collider,
        Coin,
        Enemy,
        FinalPoint,
        StartPoint
    }

    public enum Offset
    {
        None,
        x2,
        x4,
    }
}
