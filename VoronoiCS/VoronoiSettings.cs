namespace VoronoiCS
{
    public class VoronoiSettings
    {
        public int Seed { get; set; }
        public int SliceSize { get; set; }
        public int SlicesWidth { get; set; }
        public int SlicesHeight { get; set; }
        public int SiteCount { get; set; }

        public int Width
        {
            get { return SliceSize * SlicesWidth; }
        }

        public int Height
        {
            get { return SliceSize * SlicesHeight; }
        }
    }
}