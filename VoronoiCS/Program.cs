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

//            var tempHeightMap = new Heightmap2(new Size(256, 256));
//            tempHeightMap.Run();
//            using (var image = new Bitmap(256, 256))
//            {
//                for (int i = 0; i < 256; i++)
//                {
//                    for (int j = 0; j < 256; j++)
//                    {
//                        var t = tempHeightMap.GetCell(i, j);
//                        switch (t)
//                        {
//                            case 0:
//                                image.SetPixel(i, j, Color.ForestGreen);
//                                break;
//                            case 1:
//                                image.SetPixel(i, j, Color.Green);
//                                break;
//                            case 2:
//                                image.SetPixel(i, j, Color.DarkGreen);
//                                break;
//                        }
//                    }
//                }
//                image.Save("LowlangsTexture.png");
//            }

            Console.WriteLine("Generating heightmap");
            var heightmap = new Heightmap2(new Size(128, 128));
            heightmap.Run();

//            DrawHeightmap(heightmap);

            int sliceSize = 256;
            int slicesWidth = 16;
            int slicesHeight = 8;

            int width = sliceSize * slicesWidth;
            int height = sliceSize * slicesHeight;
            int dotCount = 1000;

            var rnd = new Random();
            var points = new HashSet<Point>();

            Console.WriteLine("Generating random points");
            LoadPoints(dotCount, rnd, width, height, points);

            var voronoi = new Voronoi();
            Console.WriteLine("Computing base voronoi");
            voronoi.Compute(points, width, height);
//            DrawBaseMap(voronoi, points, "VoronoiBase.png", width, height);

            Console.WriteLine("Duplicating points for wraparound map");
            var pointsForWrapAroundMap = MakePointsForWrapAroundMap(points, width);

            Console.WriteLine("Computing wraparound voronoi");
            voronoi.Compute(pointsForWrapAroundMap, width * 2, height);
//            DrawWrapAroundMap(voronoi, pointsForWrapAroundMap, "VoronoiWrapAround.png", width * 2, height);

            // do y times:
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("Fixing faulty edges");
                var semiEdges = voronoi.Edges.Where(e => (IsOutOfMap(e.Start, width * 2, height) && !IsOutOfMap(e.End, width * 2, height)) || (!IsOutOfMap(e.Start, width * 2, height) && IsOutOfMap(e.End, width * 2, height)));
                foreach (var semiEdge in semiEdges)
                {
                    FixFaultyEdge(semiEdge, width * 2, height);
                }

                Console.WriteLine("Looking for center of cells");
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

                Console.WriteLine("Setting new start point");
                points = new HashSet<Point>(centerOfRealCells);

                // create new wraparound for these points
                Console.WriteLine("Duplicating points for wraparound map");
                pointsForWrapAroundMap = MakePointsForWrapAroundMap(points, width);
                
                // generate voronoi
                Console.WriteLine("Computing wraparound voronoi");
                voronoi.Compute(pointsForWrapAroundMap, width * 2, height);
//                DrawWrapAroundMap(voronoi, pointsForWrapAroundMap, string.Format("VoronoiWrapAround{0}.png", i + 1), width * 2, height);
            }

            Console.WriteLine("Fixing faulty edges");
            var semiFinalEdges = voronoi.Edges.Where(e => (IsOutOfMap(e.Start, width * 2, height) && !IsOutOfMap(e.End, width * 2, height)) || (!IsOutOfMap(e.Start, width * 2, height) && IsOutOfMap(e.End, width * 2, height)));
            foreach (var semiEdge in semiFinalEdges)
            {
                FixFaultyEdge(semiEdge, width * 2, height);
            }

            Console.WriteLine("Looking for final cells");
            var finalCells = voronoi.Cells
                // filter the "real" cells
                .Where(c => c.Site.RealPoint).ToList();
            var ratioX = (width * 2) / 128d;
            var ratioY = height / 128d;

            Console.WriteLine("Applying heightmap to final cells");
            foreach (var finalCell in finalCells)
            {
                if (!finalCell.Vertices.Any())
                {
                    continue;
                }

                if (finalCell.Vertices.Any(p => p.Y <= 0d || p.Y >= height))
                {
                    finalCell.Height = -1;
                    continue;
                }

                finalCell.Height = heightmap.GetCell((int)(finalCell.Site.X / ratioX) - 1, (int)(finalCell.Site.Y / ratioY) - 1);
            }

            Console.WriteLine("Generating heightmap image");
            DrawHeightmap2(voronoi, finalCells, string.Format("VoronoiHeightmap_{0}.png", DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss")), width * 2, height);
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

        private static void DrawHeightmap2(Voronoi voronoi, IEnumerable<Cell> cells, string filename, int width, int height)
        {
            int halfwidth = width / 2;

            using (var image = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);

                    foreach (var cell in cells)
                    {
                        Brush color;
                        switch (cell.Height)
                        {
                            case 0:
                                color = Brushes.ForestGreen;
                                break;
                            case 1:
                                color = Brushes.DarkGoldenrod;
                                break;
                            case 2:
                                color = Brushes.DarkSlateGray;
                                break;
                            default:
                                color = Brushes.White;
                                break;
                        }

                        // fix vertices
                        //                        var vertices = cell.Vertices.Select(vertex => new Point(vertex.X - halfwidth, vertex.Y, vertex.Name));
                        var center = GetCenterPointOfPoints(cell.Vertices, width, height);
                        var orderedCells = cell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center));

                        graphics.FillPolygon(color, orderedCells.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray());

                        var otherCell = voronoi.Cells.Single(c => c.Site == cell.Site.DoublePoint);
                        if (otherCell.Vertices.Count == 0)
                        {
                            continue;
                        }

                        var center2 = GetCenterPointOfPoints(otherCell.Vertices, width, height);
                        var orderedCells2 = otherCell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center2));

                        graphics.FillPolygon(color, orderedCells2.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray());
                    }
                    foreach (var cell in cells)
                    {
                        // fix vertices
//                        var vertices = cell.Vertices.Select(vertex => new Point(vertex.X - halfwidth, vertex.Y, vertex.Name));
                        var center = GetCenterPointOfPoints(cell.Vertices, width, height);
                        var orderedCells = cell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center));

                        graphics.DrawPolygon(Pens.Blue, orderedCells.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray());

                        var otherCell = voronoi.Cells.Single(c => c.Site == cell.Site.DoublePoint);
                        if (otherCell.Vertices.Count == 0)
                        {
                            continue;
                        }

                        var center2 = GetCenterPointOfPoints(otherCell.Vertices, width, height);
                        var orderedCells2 = otherCell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center2));

                        graphics.DrawPolygon(Pens.Blue, orderedCells2.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray());
                    }
                }

                image.Save(filename);

                using (var croppedImage = new Bitmap(halfwidth, height))
                {
                    using (var croppedGraphics = Graphics.FromImage(croppedImage))
                    {
                        croppedGraphics.DrawImage(image, new Rectangle(0, 0, halfwidth, height), new Rectangle(halfwidth / 2, 0, halfwidth, height), GraphicsUnit.Pixel);
                    }

                var directory = new DirectoryInfo(filename.Replace(".png", string.Empty));
                directory.Create();

                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        using (var slicedImage = new Bitmap(256, 256))
                        {
                            using (var slicedGraphics = Graphics.FromImage(slicedImage))
                            {
                                slicedGraphics.DrawImage(croppedImage, new Rectangle(0, 0, 256, 256), new Rectangle(j * 256, i * 256, 256, 256), GraphicsUnit.Pixel);
                            }

                            slicedImage.Save(Path.Combine(directory.Name, string.Format("slices_{0}_{1}.png", j, i)));
                        }
                    }
                }

                    croppedImage.Save(filename.Replace(".png", "-cropped.png"));
                }
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