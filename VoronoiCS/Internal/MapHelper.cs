namespace VoronoiCS.Internal
{
    internal class MapHelper
    {
        internal bool IsOutOfMap(Edge edge, int width, int height)
        {
            return IsOutOfMap(edge.Start, width, height) || IsOutOfMap(edge.End, width, height);
        }

        internal bool IsOutOfMap(Point point, int width, int height)
        {
            return double.IsNaN(point.X) || double.IsNaN(point.Y) || point.X < 0 || point.Y < 0 || point.X > width || point.Y > height;
        }
    }
}