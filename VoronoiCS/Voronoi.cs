using System;
using System.Collections.Generic;

namespace VoronoiCS
{
    internal class Voronoi
    {
        private Point _fp;
        private int _height;
        private double _lasty;
        private double _ly;
        private Queue _queue;
        private Parabola _root;
        private int _width;

        public List<Edge> Edges { get; private set; }
        public List<Cell> Cells { get; private set; }

        public void Compute(HashSet<Point> points, int width, int height)
        {
            _root = null;
            _width = width;
            _height = height;

            if (points.Count < 2)
            {
                return;
            }

            Edges = new List<Edge>();
            Cells = new List<Cell>();
            _queue = new Queue();

            foreach (Point point in points)
            {
                var @event = new Event(point, true);
                var cell = new Cell(point);
                point.Cell = cell;
                _queue.Enqueue(@event);
                Cells.Add(cell);
            }

            while (!_queue.IsEmpty())
            {
                Event @event = _queue.Dequeue();
                _ly = @event.Point.Y;

                if (@event.IsParabola)
                {
                    InsertParabola(@event.Point);
                }
                else
                {
                    RemoveParabola(@event);
                }

                _lasty = @event.Point.Y;
            }

            FinishEdge(_root);

            for (var i = 0; i < Edges.Count; i++)
            {
                if (Edges[i].Neighbour != null)
                {
                    Edges[i].Start = Edges[i].Neighbour.End;
                }
            }
        }

        private void FinishEdge(Parabola n)
        {
            double mx;
            if (n.Edge.Direction.X > 0.0f)
            {
                mx = Math.Max(_width, n.Edge.Start.X + 10);
            }
            else
            {
                mx = Math.Min(0.0f, n.Edge.Start.X - 10);
            }
            n.Edge.End = new Point(mx, n.Edge.F * mx + n.Edge.G);

            if (!n.Left.IsLeaf) this.FinishEdge(n.Left);
            if (!n.Right.IsLeaf) this.FinishEdge(n.Right);
        }

        private void RemoveParabola(Event e)
        {
            Parabola p1 = e.Arch;

            Parabola xl = GetLeftParent(p1);
            Parabola xr = GetRightParent(p1);

            Parabola p0 = GetLeftChild(xl);
            Parabola p2 = GetRightChild(xr);

            if (p0.CircleEvent != null)
            {
                _queue.Remove(p0.CircleEvent);
                p0.CircleEvent = null;
            }
            if (p2.CircleEvent != null)
            {
                _queue.Remove(p2.CircleEvent);
                p2.CircleEvent = null;
            }

            var p = new Point(e.Point.X, GetY(p1.Site, e.Point.X));


            if (Equals(p0.Site.Cell.Last, p1.Site.Cell.First)) p1.Site.Cell.AddLeft(p);
            else p1.Site.Cell.AddRight(p);

            p0.Site.Cell.AddRight(p);
            p2.Site.Cell.AddLeft(p);

            _lasty = e.Point.Y;

            xl.Edge.End = p;
            xr.Edge.End = p;

            Parabola higher = null;
            Parabola par = p1;
            while (par != _root)
            {
                par = par.Parent;
                if (par == xl)
                {
                    higher = xl;
                }
                if (par == xr)
                {
                    higher = xr;
                }
            }

            higher.Edge = new Edge(p, p0.Site, p2.Site);

            Edges.Add(higher.Edge);

            Parabola gparent = p1.Parent.Parent;
            if (p1.Parent.Left == p1)
            {
                if (gparent.Left == p1.Parent) gparent.Left = p1.Parent.Right;
                else p1.Parent.Parent.Right = p1.Parent.Right;
            }
            else
            {
                if (gparent.Left == p1.Parent) gparent.Left = p1.Parent.Left;
                else gparent.Right = p1.Parent.Left;
            }

            CheckCircle(p0);
            CheckCircle(p2);
        }

        private void InsertParabola(Point p)
        {
            if (_root == null)
            {
                _root = new Parabola(p);
                _fp = p;
                return;
            }

            if (_root.IsLeaf && _root.Site.Y - p.Y < 0.01)
            {
                _root.IsLeaf = false;
                _root.Left = new Parabola(_fp);
                _root.Right = new Parabola(p);

                var s = new Point((p.X + _fp.X)/2, _height);
                if (p.X > _fp.X)
                {
                    _root.Edge = new Edge(s, _fp, p);
                }
                else
                {
                    _root.Edge = new Edge(s, p, _fp);
                }

                Edges.Add(_root.Edge);
                return;
            }

            Parabola par = GetParabolaByX(p.X);

            if (par.CircleEvent != null)
            {
                _queue.Remove(par.CircleEvent);
                par.CircleEvent = null;
            }

            var start = new Point(p.X, GetY(par.Site, p.X));

            var el = new Edge(start, par.Site, p);
            var er = new Edge(start, p, par.Site);

            el.Neighbour = er;
            Edges.Add(el);

            par.Edge = er;
            par.IsLeaf = false;

            var p0 = new Parabola(par.Site);
            var p1 = new Parabola(p);
            var p2 = new Parabola(par.Site);

            par.Right = p2;
            par.Left = new Parabola();

            par.Left.Edge = el;
            par.Left.Left = p0;
            par.Left.Right = p1;

            CheckCircle(p0);
            CheckCircle(p2);
        }

        private void CheckCircle(Parabola b)
        {
            Parabola lp = GetLeftParent(b);
            Parabola rp = GetRightParent(b);

            Parabola a = GetLeftChild(lp);
            Parabola c = GetRightChild(rp);

            if (a == null || c == null || Equals(a.Site, c.Site)) return;

            Point s = GetEdgeIntersection(lp.Edge, rp.Edge);
            if (s == null) return;

            double d = Distance(a.Site, s);

            if (s.Y - d >= _ly) return;

            var e = new Event(new Point(s.X, s.Y - d), false);

            b.CircleEvent = e;
            e.Arch = b;
            _queue.Enqueue(e);
        }

        private double Distance(Point a, Point b)
        {
            return (double) (Math.Sqrt((b.X - a.X)*(b.X - a.X) + (b.Y - a.Y)*(b.Y - a.Y)));
        }

        private Point GetEdgeIntersection(Edge a, Edge b)
        {
            Point I = GetLineIntersection(a.Start, a.B, b.Start, b.B);

            // wrong direction of edge
            bool wd = (I.X - a.Start.X)*a.Direction.X < 0 || (I.Y - a.Start.Y)*a.Direction.Y < 0
                      || (I.X - b.Start.X)*b.Direction.X < 0 || (I.Y - b.Start.Y)*b.Direction.Y < 0;

            if (wd) return null;
            return I;
        }

        private Point GetLineIntersection(Point a1, Point a2, Point b1, Point b2)
        {
            double dax = (a1.X - a2.X);
            double dbx = (b1.X - b2.X);
            double day = (a1.Y - a2.Y);
            double dby = (b1.Y - b2.Y);

            double den = dax*dby - day*dbx;
            if (Math.Abs(den) < 0.00001) return null; // parallel

            double a = (a1.X*a2.Y - a1.Y*a2.X);
            double b = (b1.X*b2.Y - b1.Y*b2.X);

            return new Point((a*dbx - dax*b)/den, (a*dby - day*b)/den);
        }

        private Parabola GetRightParent(Parabola n)
        {
            Parabola par = n.Parent;
            Parabola pLast = n;
            while (par.Right == pLast)
            {
                if (par.Parent == null) return null;
                pLast = par;
                par = par.Parent;
            }
            return par;
        }

        private Parabola GetLeftParent(Parabola n)
        {
            Parabola par = n.Parent;
            Parabola pLast = n;
            while (par.Left == pLast)
            {
                if (par.Parent == null) return null;
                pLast = par;
                par = par.Parent;
            }
            return par;
        }

        private double GetY(Point p, double x)
        {
            double dp = 2*(p.Y - _ly);
            double b1 = -2*p.X/dp;
            double c1 = _ly + dp/4 + p.X*p.X/dp;

            return (x*x/dp + b1*x + c1);
        }

        private Parabola GetParabolaByX(double xx)
        {
            Parabola par = _root;
            double x;

            while (!par.IsLeaf)
            {
                x = GetXOfEdge(par, _ly);
                if (x > xx) par = par.Left;
                else par = par.Right;
            }
            return par;
        }

        private double GetXOfEdge(Parabola par, double y)
        {
            Parabola left = GetLeftChild(par);
            Parabola right = GetRightChild(par);

            Point p = left.Site;
            Point r = right.Site;

            double dp = 2*(p.Y - y);
            double a1 = 1/dp;
            double b1 = -2*p.X/dp;
            double c1 = y + dp*0.25 + p.X*p.X/dp;

            dp = 2*(r.Y - y);
            double a2 = 1/dp;
            double b2 = -2*r.X/dp;
            double c2 = y + dp*0.25 + r.X*r.X/dp;

            double a = a1 - a2;
            double b = b1 - b2;
            double c = c1 - c2;

            double disc = b*b - 4*a*c;
            double x1 = (-b + Math.Sqrt(disc))/(2*a);
            double x2 = (-b - Math.Sqrt(disc))/(2*a);

            double ry;
            if (p.Y < r.Y) ry = Math.Max(x1, x2);
            else ry = Math.Min(x1, x2);

            return ry;
        }

        private Parabola GetRightChild(Parabola n)
        {
            if (n == null) return null;

            Parabola par = n.Right;
            while (!par.IsLeaf) par = par.Left;
            return par;
        }

        private Parabola GetLeftChild(Parabola n)
        {
            if (n == null) return null;

            Parabola par = n.Left;
            while (!par.IsLeaf) par = par.Right;
            return par;
        }
    }
}