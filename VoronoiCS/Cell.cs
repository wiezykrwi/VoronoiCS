using System.Collections.Generic;

namespace VoronoiCS
{
    internal class Cell
    {
        private int _size;

        public Cell(Point site)
        {
            Site = site;
            Vertices = new List<Point>();
        }

        public Point Site { get; private set; }
        public Point Last { get; private set; }
        public Point First { get; private set; }
        public List<Point> Vertices { get; private set; }
        
        public int Height { get; set; }

        public void AddLeft(Point p)
        {
            Vertices.Insert(0, p);

            _size++;
            First = p;
            if (_size == 1) Last = p;
        }

        public void AddRight(Point p)
        {
            Vertices.Add(p);
            _size++;
            Last = p;
            if (_size == 1) First = p;
        }
    }
}