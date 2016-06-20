using System.Collections.Generic;

namespace VoronoiCS.Internal
{
    internal class MapExtender
    {
        internal HashSet<Point> MakePointsForWrapAroundMap(IEnumerable<Point> points, double width)
        {
            double halfWidth = width / 2;
            var newPoints = new HashSet<Point>();

            foreach (var point in points)
            {
                if (point.X > halfWidth)
                {
                    var doublePoint = new Point(point.X - halfWidth, point.Y);
                    newPoints.Add(doublePoint); // pad left half
                    newPoints.Add(new Point(point.X + halfWidth, point.Y, point.Name, doublePoint)); // real point
                }
                else
                {
                    var doublePoint = new Point(point.X + halfWidth * 3, point.Y);
                    newPoints.Add(doublePoint); // pad right half
                    newPoints.Add(new Point(point.X + halfWidth, point.Y, point.Name, doublePoint)); // real point
                }
            }

            return newPoints;
        }
    }
}