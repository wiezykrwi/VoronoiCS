using System.Collections.Generic;

namespace VoronoiCS
{
    internal class ClockwisePointComparer : IComparer<Point>
    {
        private readonly Point _center;

        public ClockwisePointComparer(Point center)
        {
            _center = center;
        }

        public int Compare(Point a, Point b)
        {
            if (a == b)
            {
                return 0;
            }

            if (a.X - _center.X >= 0 && b.X - _center.X < 0)
            {
                return -1;
            }

            if (a.X - _center.X < 0 && b.X - _center.X >= 0)
            {
                return 1;
            }

            if (a.X - _center.X == 0 && b.X - _center.X == 0)
            {
                if (a.Y - _center.Y >= 0 || b.Y - _center.Y >= 0)
                {
                    return a.Y > b.Y ? -1 : 1;
                }

                return b.Y > a.Y ? -1 : 1;
            }

            // compute the cross product of vectors (center -> a) x (center -> b)
            double det = (a.X - _center.X) * (b.Y - _center.Y) - (b.X - _center.X) * (a.Y - _center.Y);
            if (det < 0)
            {
                return -1;
            }

            if (det > 0)
            {
                return 1;
            }

            // points a and b are on the same line from the center
            // check which point is closer to the center
            double d1 = (a.X - _center.X) * (a.X - _center.X) + (a.Y - _center.Y) * (a.Y - _center.Y);
            double d2 = (b.X - _center.X) * (b.X - _center.X) + (b.Y - _center.Y) * (b.Y - _center.Y);
            return d1 > d2 ? -1 : 1;
        }
    }
}