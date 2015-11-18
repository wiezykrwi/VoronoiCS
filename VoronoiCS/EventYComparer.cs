using System.Collections.Generic;

namespace VoronoiCS
{
    internal class EventYComparer : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            return x < y ? 1 : -1;
        }
    }
}