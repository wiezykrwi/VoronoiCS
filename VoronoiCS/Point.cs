using System.Diagnostics;

namespace VoronoiCS
{
    [DebuggerDisplay("Point ({X}, {Y})")]
    internal class Point
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public string Name { get; private set; }
        public bool RealPoint { get; private set; }
        public Cell Cell { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point(double x, double y, string name)
        {
            X = x;
            Y = y;

            Name = name;
            RealPoint = true;
        }

        protected bool Equals(Point other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode()*397) ^ Y.GetHashCode();
            }
        }
    }
}