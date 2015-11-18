namespace VoronoiCS
{
    internal class Event
    {
        public Event(Point point, bool isParabola)
        {
            this.Point = point;
            IsParabola = isParabola;
        }

        public Point Point { get; set; }
        public bool IsParabola { get; set; }
        public Parabola Arch { get; set; }
    }
}