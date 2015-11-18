namespace VoronoiCS
{
    internal class Parabola
    {
        private Parabola _left;
        private Parabola _right;

        public Parabola()
        {
        }

        public Parabola(Point site)
        {
            this.Site = site;
            IsLeaf = site != null;
        }

        public Point Site { get; private set; }
        public bool IsLeaf { get; set; }

        public Parabola Left
        {
            get { return _left; }
            set
            {
                _left = value;
                value.Parent = this;
            }
        }

        public Parabola Right
        {
            get { return _right; }
            set
            {
                _right = value;
                value.Parent = this;
            }
        }

        public Edge Edge { get; set; }
        public Event CircleEvent { get; set; }
        public Parabola Parent { get; set; }
    }
}