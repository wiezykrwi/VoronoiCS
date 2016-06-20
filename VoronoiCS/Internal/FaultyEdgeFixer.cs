using System;
using System.Linq;

namespace VoronoiCS.Internal
{
    internal class FaultyEdgeFixer
    {
        private readonly MapHelper _mapHelper;

        public FaultyEdgeFixer(MapHelper mapHelper)
        {
            _mapHelper = mapHelper;
        }

        internal void FixFaultyEdges(Voronoi voronoi, VoronoiSettings settings)
        {
            var semiEdges =
                voronoi.Edges.Where(
                    e => (_mapHelper.IsOutOfMap(e.Start, settings.Width * 2, settings.Height)
                          && !_mapHelper.IsOutOfMap(e.End, settings.Width * 2, settings.Height))
                         || (!_mapHelper.IsOutOfMap(e.Start, settings.Width * 2, settings.Height)
                             && _mapHelper.IsOutOfMap(e.End, settings.Width * 2, settings.Height)));
            foreach (var semiEdge in semiEdges)
            {
                FixFaultyEdge(semiEdge, settings.Width * 2, settings.Height);
            }
        }

        private void FixFaultyEdge(Edge faultyEdge, int width, int height)
        {
            double deltaX = faultyEdge.End.X - faultyEdge.Start.X;
            double deltaY = faultyEdge.End.Y - faultyEdge.Start.Y;

            if (_mapHelper.IsOutOfMap(faultyEdge.Start, width, height))
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

            if (_mapHelper.IsOutOfMap(faultyEdge.End, width, height))
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
    }
}