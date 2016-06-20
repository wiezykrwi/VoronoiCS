using System.Collections.Generic;
using System.Linq;

namespace VoronoiCS.Internal
{
    internal class CenterFinder
    {
        private readonly MapHelper _mapHelper;

        public CenterFinder(MapHelper mapHelper)
        {
            _mapHelper = mapHelper;
        }

        internal List<Point> FindAllCenters(Voronoi voronoi, VoronoiSettings settings)
        {
            var realCells = voronoi.Cells
                // filter the "real" cells
                .Where(c => c.Site.RealPoint).ToList();
            var centerOfRealCells = realCells
                // find center for points
                .Select(c =>
                {
                    var centerPointOfPoints = GetCenterPointOfPoints(c.Vertices, settings.Width * 2, settings.Height);
                    // account for cells having their center shifted to the other side
                    if (centerPointOfPoints.X > ((double)settings.Width / 2d) * 3d)
                    {
                        centerPointOfPoints = new Point(centerPointOfPoints.X - settings.Width, centerPointOfPoints.Y);
                    }
                    else if (centerPointOfPoints.X < ((double)settings.Width / 2))
                    {
                        centerPointOfPoints = new Point(centerPointOfPoints.X + settings.Width, centerPointOfPoints.Y);
                    }
                    // fix points to be real again
                    var point = new Point(centerPointOfPoints.X - settings.Width / 2, centerPointOfPoints.Y, c.Site.Name);
                    if (_mapHelper.IsOutOfMap(point, settings.Width * 2, settings.Height))
                    {
                        return c.Site;
                    }
                    return point;
                }).ToList();
            return centerOfRealCells;
        }

        private Point GetCenterPointOfPoints(IEnumerable<Point> points, int width, int height)
        {
            double x = 0.0d;
            double y = 0.0d;
            int count = 0;

            foreach (var point in points.Where(point => !_mapHelper.IsOutOfMap(point, width, height)))
            {
                x += point.X;
                y += point.Y;
                count++;
            }

            return new Point(x / count, y / count);
        }
    }
}