using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace VoronoiCS
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var heightmap = new Heightmap2(new Size(128, 128));
            heightmap.Run();

            DrawHeightmap(heightmap);

            int width = 1920;
            int height = 1080;
            int dotCount = 200;

            var rnd = new Random(1);
            var points = new HashSet<Point>();

            LoadPoints(dotCount, rnd, width, height, points);

            var voronoi = new Voronoi();
            voronoi.Compute(points, width, height);
            DrawBaseMap(voronoi, points, "VoronoiBase.png", width, height);
            
            var pointsForWrapAroundMap = MakePointsForWrapAroundMap(points, width);

            voronoi.Compute(pointsForWrapAroundMap, width * 2, height);
            DrawWrapAroundMap(voronoi, pointsForWrapAroundMap, "VoronoiWrapAround.png", width * 2, height);

            var semiEdges = voronoi.Edges.Where(e => (IsOutOfMap(e.Start, width*2, height) && !IsOutOfMap(e.End, width*2, height)) || (!IsOutOfMap(e.Start, width*2, height) && IsOutOfMap(e.End, width*2, height)));

            foreach (var semiEdge in semiEdges)
            {
                FixFaultyEdge(semiEdge, width * 2, height);
            }

            var cell = voronoi.Cells.Single(c => c.Site.Name == "1");
            var edges = voronoi.Edges.Where(e => cell.Vertices.Contains(e.Start) || cell.Vertices.Contains(e.End)).ToList();

            // do y times:
            for (int i = 0; i < 3; i++)
            {
                var realCells = voronoi.Cells
                    // filter the "real" cells
                    .Where(c => c.Site.RealPoint).ToList();
                var centerOfRealCells = realCells
                    // find center for points
                    .Select(c =>
                    {
                        var centerPointOfPoints = GetCenterPointOfPoints(c.Vertices, width * 2, height);
                        // account for cells having their center shifted to the other side
                        if (centerPointOfPoints.X > ((double)width / 2d) * 3d)
                        {
                            centerPointOfPoints = new Point(centerPointOfPoints.X - width, centerPointOfPoints.Y);
                        }
                        else if (centerPointOfPoints.X < ((double)width / 2))
                        {
                            centerPointOfPoints = new Point(centerPointOfPoints.X + width, centerPointOfPoints.Y);
                        }
                        // fix points to be real again
                        var point = new Point(centerPointOfPoints.X - width / 2, centerPointOfPoints.Y, c.Site.Name);
                        if (IsOutOfMap(point, width * 2, height))
                        {
                            return c.Site;
                        }
                        return point;
                    }).ToList();

                points = new HashSet<Point>(centerOfRealCells);

                // create new wraparound for these points
                pointsForWrapAroundMap = MakePointsForWrapAroundMap(points, width);
                
                // generate voronoi
                voronoi.Compute(pointsForWrapAroundMap, width * 2, height);
                DrawWrapAroundMap(voronoi, pointsForWrapAroundMap, string.Format("VoronoiWrapAround{0}.png", i + 1), width * 2, height);
            }

            var finalCells = voronoi.Cells
                // filter the "real" cells
                .Where(c => c.Site.RealPoint).ToList();
            var ratioX = width * 2 / 128;
            var ratioY = height / 128;

            foreach (var finalCell in finalCells)
            {
                finalCell.Height = heightmap.GetCell((int) (finalCell.Site.X / ratioX), (int) (finalCell.Site.Y / ratioY));
            }

            DrawHeightmap2(finalCells, "VoronoiHeightmap.png", width, height);
        }

        private static void FixFaultyEdge(Edge faultyEdge, int width, int height)
        {
            double deltaX = faultyEdge.End.X - faultyEdge.Start.X;
            double deltaY = faultyEdge.End.Y - faultyEdge.Start.Y;

            if (IsOutOfMap(faultyEdge.Start, width, height))
            {
                // Try fixing X axis
                if (faultyEdge.Start.X < 0)
                {
                    double newX = 0.0d;
                    double newY = FindNewPosition(faultyEdge.Start.X, newX, faultyEdge.Start.Y, deltaY, deltaX);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Start = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.Start);
                }
                else if (faultyEdge.Start.X > width)
                {
                    double newX = width;
                    double newY = FindNewPosition(faultyEdge.Start.X, newX, faultyEdge.Start.Y, deltaY, deltaX);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Start = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.Start);
                }
                else if (faultyEdge.Start.Y < 0)
                {
                    double newY = 0.0d;
                    double newX = FindNewPosition(faultyEdge.Start.Y, newY, faultyEdge.Start.X, deltaX, deltaY);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Start = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.Start);
                }
                else if (faultyEdge.Start.Y > height)
                {
                    double newY = height;
                    double newX = FindNewPosition(faultyEdge.Start.Y, newY, faultyEdge.Start.X, deltaX, deltaY);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.Start);
                    faultyEdge.Start = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.Start);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.Start);
                }
            }

            if (IsOutOfMap(faultyEdge.End, width, height))
            {
                // Try fixing X axis
                if (faultyEdge.End.X < 0)
                {
                    double newX = 0.0d;
                    double newY = FindNewPosition(faultyEdge.Start.X, newX, faultyEdge.Start.Y, deltaY, deltaX);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.End = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.End);
                }
                else if (faultyEdge.End.X > width)
                {
                    double newX = width;
                    double newY = FindNewPosition(faultyEdge.Start.X, newX, faultyEdge.Start.Y, deltaY, deltaX);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.End = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.End);
                }
                else if (faultyEdge.End.Y < 0)
                {
                    double newY = 0.0d;
                    double newX = FindNewPosition(faultyEdge.Start.Y, newY, faultyEdge.Start.X, deltaX, deltaY);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.End = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.End);
                }
                else if (faultyEdge.End.Y > height)
                {
                    double newY = height;
                    double newX = FindNewPosition(faultyEdge.Start.Y, newY, faultyEdge.Start.X, deltaX, deltaY);

                    faultyEdge.Left.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Remove(faultyEdge.End);
                    faultyEdge.End = new Point(newX, newY);
                    faultyEdge.Left.Cell.Vertices.Add(faultyEdge.End);
                    faultyEdge.Right.Cell.Vertices.Add(faultyEdge.End);
                }
            }
        }

        private static double FindNewPosition(double startPosition, double newPosition, double otherPosition, double deltaTarget, double deltaSource)
        {
            double offsetY = newPosition - startPosition;
            double ratio = offsetY / deltaSource;
            double offsetX = deltaTarget * ratio;
            double newX = otherPosition + offsetX;
            return newX;
        }

        private static bool IsOutOfMap(Edge edge, int width, int height)
        {
            return IsOutOfMap(edge.Start, width, height) || IsOutOfMap(edge.End, width, height);
        }

        private static bool IsOutOfMap(Point point, int width, int height)
        {
            return double.IsNaN(point.X) || double.IsNaN(point.Y) || point.X < 0 || point.Y < 0 || point.X > width || point.Y > height;
        }

        private static void DrawHeightmap(Heightmap2 heightmap)
        {
            using (var image = new Bitmap(128, 128))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);

                    for (int i = 0; i < 128; i++)
                    {
                        for (int j = 0; j < 128; j++)
                        {
                            Color color;
                            var value = heightmap.GetCell(i, j);
                            switch (value)
                            {
                                case 0:
                                    color = Color.DarkBlue;
                                    break;
                                case 1:
                                    color = Color.DeepSkyBlue;
                                    break;
                                case 2:
                                    color = Color.ForestGreen;
                                    break;
                                case 3:
                                    color = Color.DarkGoldenrod;
                                    break;
                                case 4:
                                    color = Color.DarkSlateGray;
                                    break;
                                default:
                                    color = Color.White;
                                    break;
                            }
                            image.SetPixel(i, j, color);
                        }
                    }
                }

                image.Save("heightmap.png");
            }
        }

        private static Point GetCenterPointOfPoints(IEnumerable<Point> points, int width, int height)
        {
            double x = 0.0d;
            double y = 0.0d;
            int count = 0;

            foreach (var point in points.Where(point => !IsOutOfMap(point, width, height)))
            {
                x += point.X;
                y += point.Y;
                count++;
            }

            return new Point(x / count, y / count);
        }

        private static HashSet<Point> MakePointsForWrapAroundMap(IEnumerable<Point> points, double width)
        {
            double halfWidth = width / 2;
            var newPoints = new HashSet<Point>();

            foreach (var point in points)
            {
                if (point.X > halfWidth)
                {
                    newPoints.Add(new Point(point.X - halfWidth, point.Y)); // pad left half
                    newPoints.Add(new Point(point.X + halfWidth, point.Y, point.Name)); // real point
                }
                else
                {
                    newPoints.Add(new Point(point.X + halfWidth, point.Y, point.Name)); // real point
                    newPoints.Add(new Point(point.X + halfWidth * 3, point.Y)); // pad right half
                }
            }

            return newPoints;
        }

        private static void LoadPoints(int dotCount, Random rnd, int width, int height, HashSet<Point> points)
        {
            for (int i = 0; i < dotCount; i++)
            {
                var x = (double) (rnd.NextDouble()*(width));
                var y = (double) (rnd.NextDouble()*(height));

                points.Add(new Point(x, y, (i+1).ToString()));
            }
        }

        private static void DrawBaseMap(Voronoi voronoi, IEnumerable<Point> points, string filename, int width, int height)
        {
            using (var image = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);

                    DrawPoints(points, graphics);
                    DrawEdges(voronoi, graphics);
                }

                image.Save(filename);
            }
        }

        private static void DrawWrapAroundMap(Voronoi voronoi, IEnumerable<Point> points, string filename, int width, int height)
        {
            using (var image = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);

                    DrawPoints(points, graphics);
                    DrawEdges(voronoi, graphics);

                    graphics.DrawLine(Pens.Black, width / 2, 0, width / 2, height);
                    graphics.DrawLine(Pens.Black, width / 4, 0, width / 4, height);
                    graphics.DrawLine(Pens.Black, (width / 4) * 3, 0, (width / 4) * 3, height);
                }

                image.Save(filename);
            }
        }

        private static void DrawHeightmap2(IEnumerable<Cell> cells, string filename, int width, int height)
        {
            int halfwidth = width / 2;

            using (var image = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);

                    foreach (var cell in cells)
                    {
                        // fix vertices
                        var vertices = cell.Vertices.Select(vertex => new Point(vertex.X - halfwidth, vertex.Y, vertex.Name));
                        var center = GetCenterPointOfPoints(vertices, width, height);
                        var orderedCells = vertices.OrderBy(v => v, new ClockwisePointComparer(center));

                        Brush color;
                        switch (cell.Height)
                        {
                            case 0:
                                color = Brushes.DarkBlue;
                                break;
                            case 1:
                                color = Brushes.DeepSkyBlue;
                                break;
                            case 2:
                                color = Brushes.ForestGreen;
                                break;
                            case 3:
                                color = Brushes.DarkGoldenrod;
                                break;
                            case 4:
                                color = Brushes.DarkSlateGray;
                                break;
                            default:
                                color = Brushes.White;
                                break;
                        }

                        graphics.FillPolygon(color, orderedCells.Select(p => new PointF((float) p.X, (float) p.Y)).ToArray());
                    }
                }

                image.Save(filename);
            }
        }

        private static void DrawEdges(Voronoi voronoi, Graphics graphics)
        {
            foreach (Edge edge in voronoi.Edges)
            {
                graphics.DrawLine(Pens.Blue, (float) edge.Start.X, (float) edge.Start.Y, (float) edge.End.X, (float) edge.End.Y);
            }
        }

        private static void DrawPoints(IEnumerable<Point> points, Graphics graphics)
        {
            foreach (Point point in points)
            {
                graphics.DrawRectangle(Pens.Red, (float) point.X, (float) point.Y, 1, 1);
                if (!string.IsNullOrEmpty(point.Name))
                {
                    graphics.DrawString(point.Name, new Font(FontFamily.GenericMonospace, 10.0f), Brushes.Black, (float)point.X, (float)point.Y);
                }
            }
        }
    }

    internal class ClockwisePointComparer : IComparer<Point>
    {
        private readonly Point _center;

        public ClockwisePointComparer(Point center)
        {
            _center = center;
        }

        public int Compare(Point a, Point b)
        {
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