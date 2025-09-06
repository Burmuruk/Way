using UnityEngine;

namespace Xolito.Utilities
{
    public struct Point
    {
        public int x;
        public int y;
        public int layer;

        public Point(int y, int x, int layer) => (this.y, this.x, this.layer) = (y, x, layer);

        public Point(Vector2 vector)
        {
            x = (int)vector.x;
            y = (int)vector.y;
            layer = 0;
        }

        public Point((int y, int x, int layer) point) => (y, x, layer) = point;

        public Point((int y, int x) point, int layer) => (y, x, this.layer) = (point.y, point.x, layer);

        public static bool operator ==(Point a, Point b)
        {
            if (a.y == b.y && a.x == b.x && a.layer == b.layer)
                return true;

            return false;
        }

        public static bool operator !=(Point a, Point b)
        {
            if (a.y != b.y || a.x != b.x || a.layer != b.layer)
                return true;

            return false;
        }

        public void Deconstruct(out int y, out int x, out int layer)
        {
            y = this.y;
            x = this.x;
            layer = this.layer;
        }

        public override string ToString() => $"({y},{x},{layer})";
    }
}
