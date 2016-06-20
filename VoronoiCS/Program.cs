using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

using Newtonsoft.Json;

using VoronoiCS.Internal;

namespace VoronoiCS
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine("Generating heightmap");
            var heightmap = new Heightmap2(new Size(128, 128));
            heightmap.Run();

//            DrawHeightmap(heightmap);

            var basePath = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
            Directory.CreateDirectory(basePath);

            int seed;

            if (args.Length == 0)
            {
                var seedRandom = new Random();
                seed = seedRandom.Next();
            }
            else
            {
                seed = int.Parse(args[0]);
            }

            var settings = new VoronoiSettings
            {
                Seed = seed,
                SliceSize = 256,
                SlicesWidth = 16,
                SlicesHeight = 8,
                SiteCount = 2000
            };

            var jsonSerializer = JsonSerializer.CreateDefault();
            using (var writer = new StreamWriter(new FileStream(Path.Combine(basePath, "settings.json"), FileMode.CreateNew)))
            {
                jsonSerializer.Serialize(writer, settings);
                writer.Flush();
            }

            var rnd = new Random(settings.Seed);
            var points = new HashSet<Point>();

            Console.WriteLine("Generating random points");
            LoadPoints(rnd, settings, points);
            using (var writer = new StreamWriter(new FileStream(Path.Combine(basePath, "points.json"), FileMode.CreateNew)))
            {
                jsonSerializer.Serialize(writer, points);
                writer.Flush();
            }

            Console.WriteLine("Duplicating points for wraparound map");
            var pointsForWrapAroundMap = MakePointsForWrapAroundMap(points, settings.Width);
            
            var voronoi = new Voronoi();
            Console.WriteLine("Computing wraparound voronoi");
            voronoi.Compute(pointsForWrapAroundMap, settings.Width * 2, settings.Height);
//            DrawWrapAroundMap(voronoi, pointsForWrapAroundMap, "VoronoiWrapAround.png", width * 2, height);

            var mapHelper = new MapHelper();
            var centerFinder = new CenterFinder(mapHelper);
            var faultyEdgeFixer = new FaultyEdgeFixer(mapHelper);

            // do y times:
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("Fixing faulty edges");
                faultyEdgeFixer.FixFaultyEdges(voronoi, settings);

                Console.WriteLine("Looking for center of cells");
                var centerOfRealCells = centerFinder.FindAllCenters(voronoi, settings);

                Console.WriteLine("Setting new start point");
                points = new HashSet<Point>(centerOfRealCells);

                // create new wraparound for these points
                Console.WriteLine("Duplicating points for wraparound map");
                pointsForWrapAroundMap = MakePointsForWrapAroundMap(points, settings.Width);

                // generate voronoi
                Console.WriteLine("Computing wraparound voronoi");
                voronoi.Compute(pointsForWrapAroundMap, settings.Width * 2, settings.Height);
//                DrawWrapAroundMap(voronoi, pointsForWrapAroundMap, string.Format("VoronoiWrapAround{0}.png", i + 1), width * 2, height);
            }

            Console.WriteLine("Fixing faulty edges");
            faultyEdgeFixer.FixFaultyEdges(voronoi, settings);

            Console.WriteLine("Looking for final cells");
            var finalCells = voronoi.Cells
                // filter the "real" cells
                .Where(c => c.Site.RealPoint).ToList();
            var ratioX = (settings.Width * 2) / 128d;
            var ratioY = settings.Height / 128d;

            Console.WriteLine("Applying heightmap to final cells");
            foreach (var finalCell in finalCells)
            {
                if (!finalCell.Vertices.Any())
                {
                    continue;
                }

                if (finalCell.Vertices.Any(p => p.Y <= 0d || p.Y >= settings.Height))
                {
                    finalCell.Height = -1;
                    continue;
                }

                finalCell.Height = heightmap.GetCell((int) (finalCell.Site.X / ratioX) - 1, (int) (finalCell.Site.Y / ratioY) - 1);
            }

            Console.WriteLine("Generating heightmap image");
            var filename = Path.Combine(basePath, string.Format("VoronoiHeightmap_{0}.png", DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss")));
            DrawHeightmap2(voronoi, finalCells, filename, settings.Width * 2, settings.Height);
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

        private static void LoadPoints(Random rnd, VoronoiSettings settings, HashSet<Point> points)
        {
            for (int i = 0; i < settings.SiteCount; i++)
            {
                var x = (double) (rnd.NextDouble() * (settings.Width));
                var y = (double) (rnd.NextDouble() * (settings.Height));

                points.Add(new Point(x, y, (i + 1).ToString()));
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

                    foreach (var cell in cells.Where(c => c.Height == 0))
                    {
                        float maxX = (float) cell.Vertices.Max(p => p.X);
                        float minX = (float) cell.Vertices.Min(p => p.X);
                        int cellWidth = (int) (maxX - minX);
                        float maxY = (float) cell.Vertices.Max(p => p.Y);
                        float minY = (float) cell.Vertices.Min(p => p.Y);
                        int cellHeight = (int) (maxY - minY);
                        var noiseMap = new Heightmap2(new Size(cellWidth, cellHeight));
                        noiseMap.Run();

//                        using (var memStrm = new MemoryStream())
                        {
                            using (var noiseImage = new Bitmap(cellWidth, cellHeight))
                            {
                                for (int i = 0; i < cellWidth; i++)
                                {
                                    for (int j = 0; j < cellHeight; j++)
                                    {
                                        var t = noiseMap.GetCell(i, j);
                                        switch (t)
                                        {
                                            case 0:
                                                noiseImage.SetPixel(i, j, Color.ForestGreen);
                                                break;
                                            case 1:
                                                noiseImage.SetPixel(i, j, Color.Green);
                                                break;
                                            case 2:
                                                noiseImage.SetPixel(i, j, Color.DarkGreen);
                                                break;
                                        }
                                    }
                                }

                                graphics.DrawImage(noiseImage, minX, minY);

//                                noiseImage.Save("noise.png");
                            }

//                            memStrm.Flush();
//                            memStrm.Position = 0;

//                            using (var tempImage = new Bitmap(memStrm))
//                            {
//                                graphics.DrawImage(tempImage, minX, minY);

//                                graphics.DrawLine(Pens.Blue, minX, minY - 5, minX, minY + tempImage.Height + 5);
//                                graphics.DrawLine(Pens.Blue, minX - 5, minY, minX + tempImage.Width + 5, minY);
//                                graphics.DrawLine(Pens.Blue, minX - 5, minY + tempImage.Height, minX + tempImage.Width + 5, minY + tempImage.Height);
//                                graphics.DrawLine(Pens.Blue, minX + tempImage.Width, minY - 5, minX + tempImage.Width, minY + tempImage.Height + 5);
//                            }
                        }
//                        var otherCell = voronoi.Cells.Single(c => c.Site == cell.Site.DoublePoint);
//                        if (otherCell.Vertices.Count == 0)
//                        {
//                            continue;
//                        }

//                        var center2 = GetCenterPointOfPoints(otherCell.Vertices, width, height);
//                        var orderedCells2 = otherCell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center2));

//                        graphics.FillPolygon(color, orderedCells2.Select(p => new PointF((float) p.X, (float) p.Y)).ToArray());
                    }
                    image.Save(filename);
                    return;
//                    foreach (var cell in cells.Where(c => c.Height != 0))
//                    {
//                        Brush color;
//                        switch (cell.Height)
//                        {
//                            case 1:
//                                color = Brushes.DarkGoldenrod;
//                                break;
//                            case 2:
//                                color = Brushes.DarkSlateGray;
//                                break;
//                            default:
//                                color = Brushes.White;
//                                break;
//                        }
//
//                        // fix vertices
//                        //                        var vertices = cell.Vertices.Select(vertex => new Point(vertex.X - halfwidth, vertex.Y, vertex.Name));
//                        var center = GetCenterPointOfPoints(cell.Vertices, width, height);
//                        var orderedCells = cell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center));
//
//                        var pointFs = orderedCells.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();
//                        graphics.FillPolygon(color, pointFs);
//
//                        var otherCell = voronoi.Cells.Single(c => c.Site == cell.Site.DoublePoint);
//                        if (otherCell.Vertices.Count == 0)
//                        {
//                            continue;
//                        }
//
//                        var center2 = GetCenterPointOfPoints(otherCell.Vertices, width, height);
//                        var orderedCells2 = otherCell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center2));
//
//                        graphics.FillPolygon(color, orderedCells2.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray());
//                    }
//                    foreach (var cell in cells)
//                    {
//                        // fix vertices
////                        var vertices = cell.Vertices.Select(vertex => new Point(vertex.X - halfwidth, vertex.Y, vertex.Name));
//                        var center = GetCenterPointOfPoints(cell.Vertices, width, height);
//                        var orderedCells = cell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center));
//
//                        graphics.DrawPolygon(Pens.Blue, orderedCells.Select(p => new PointF((float) p.X, (float) p.Y)).ToArray());
//
//                        var otherCell = voronoi.Cells.Single(c => c.Site == cell.Site.DoublePoint);
//                        if (otherCell.Vertices.Count == 0)
//                        {
//                            continue;
//                        }
//
//                        var center2 = GetCenterPointOfPoints(otherCell.Vertices, width, height);
//                        var orderedCells2 = otherCell.Vertices.OrderBy(v => v, new ClockwisePointComparer(center2));
//
//                        graphics.DrawPolygon(Pens.Blue, orderedCells2.Select(p => new PointF((float) p.X, (float) p.Y)).ToArray());
//                    }
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
                    var zoomDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "20"));
                    zoomDirectory.Create();

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

                                var path = Path.Combine(directory.Name, zoomDirectory.Name, string.Format("slices_{0}_{1}.png", j, i));
                                slicedImage.Save(path);
                            }
                        }
                    }
                    
                    using (var resizedImage = new Bitmap(halfwidth/2, height/2))
                    {
                        using (var resizedGraphics = Graphics.FromImage(resizedImage))
                        {
                            resizedGraphics.DrawImage(croppedImage, new Rectangle(0, 0, halfwidth / 2, height / 2), new Rectangle(0, 0, halfwidth, height), GraphicsUnit.Pixel);
                        }

                        zoomDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "19"));
                        zoomDirectory.Create();

                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                using (var slicedImage = new Bitmap(256, 256))
                                {
                                    using (var slicedGraphics = Graphics.FromImage(slicedImage))
                                    {
                                        slicedGraphics.DrawImage(resizedImage, new Rectangle(0, 0, 256, 256), new Rectangle(j * 256, i * 256, 256, 256), GraphicsUnit.Pixel);
                                    }

                                    slicedImage.Save(Path.Combine(directory.Name, zoomDirectory.Name, string.Format("slices_{0}_{1}.png", j, i)));
                                }
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
                    graphics.DrawString(point.Name, new Font(FontFamily.GenericMonospace, 10.0f), Brushes.Black, (float) point.X, (float) point.Y);
                }
            }
        }
    }
}