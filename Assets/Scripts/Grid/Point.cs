using UnityEngine;

namespace Xolito.Utilities
{
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

        public static bool operator ==(Point a, Point b)
        {
            if (a.y == b.y && a.x == b.x)
                return true;

            return false;
        }

        public static bool operator !=(Point a, Point b)
        {
            if (a.y != b.y || a.x != b.x)
                return true;

            return false;
        }
    }
}
