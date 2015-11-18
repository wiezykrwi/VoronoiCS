using System.Diagnostics;

namespace VoronoiCS
{
    [DebuggerDisplay("Edge ({Start.X}, {Start.Y} - {End.X}, {End.Y})")]
    internal class Edge
    {
        public Edge(Point s, Point a, Point b)
        {
            Start = s;
            Left = a;
            Right = b;

            F = (a.X - b.X)/(b.Y - a.Y);
            G = s.Y - F*s.X;
            Direction = new Point(b.Y - a.Y, -(b.X - a.X));
            B = new Point(s.X + Direction.X, s.Y + Direction.Y);

            Intersected = false;
            Counted = false;
        }

        public Point Start { get; set; }
        public Point End { get; set; }
        public Point Left { get; set; }
        public Point Right { get; set; }

        public double F { get; set; }
        public double G { get; set; }
        public Point Direction { get; set; }
        public Point B { get; set; }

        public bool Counted { get; set; }
        public bool Intersected { get; set; }
        
        public Edge Neighbour { get; set; }
    }
}