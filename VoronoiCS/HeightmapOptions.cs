namespace VoronoiCS
{
    public class HeightmapOptions
    {
        public double LowValue { get; set; }
        public double HighValue { get; set; }
        public double Variability { get; set; }

        public static HeightmapOptions Default
        {
            get
            {
                return new HeightmapOptions
                {
                    LowValue = 0,
                    HighValue = 255,
                    Variability = 0.75
                };
            }
        }
    }
}