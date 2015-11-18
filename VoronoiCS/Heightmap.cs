using System;
using System.Drawing;
using System.Runtime.Remoting;

namespace VoronoiCS
{
    public class Heightmap
    {
        private readonly double[,] _map;
        private readonly double _midValue;
        private readonly Random _random;
        private readonly double _variability;
        private Size _size;

        public Heightmap(Size size) : this(size, HeightmapOptions.Default)
        {
        }

        public Heightmap(Size size, HeightmapOptions options)
        {
            _variability = options.Variability;
            _midValue = Math.Floor((options.LowValue + options.HighValue)/2);
            _size = size;

            _map = new double[size.Width + 1, size.Height + 1];
            _random = new Random();
        }

        public double GetCell(int x, int y)
        {
            return _map[y, x];
        }

        private void SetCell(int x, int y, double v)
        {
            _map[y, x] = v;
        }

        public void Run()
        {
            DiamondSquare(0, 0, _size.Width, _size.Height, _midValue);
        }

        private void DiamondSquare(int left, int top, int right, int bottom, double baseHeight)
        {
            int xCentre = (int) Math.Floor((double) (left + right)/2);
            int yCentre = (int) Math.Floor((double) (top + bottom)/2);
            double centrePointValue = Math.Floor(((GetCell(left, top) + GetCell(right, top) + GetCell(left, bottom) + GetCell(right, bottom))/4) - (Math.Floor((_random.NextDouble() - 0.5)*baseHeight*2)));

            SetCell(xCentre, yCentre, centrePointValue);
            SetCell(xCentre, top, Math.Floor((GetCell(left, top) + GetCell(right, top))/2 + ((_random.NextDouble() - 0.5)*baseHeight)));
            SetCell(xCentre, bottom, Math.Floor((GetCell(left, bottom) + GetCell(right, bottom))/2 + ((_random.NextDouble() - 0.5)*baseHeight)));
            SetCell(left, yCentre, Math.Floor((GetCell(left, top) + GetCell(left, bottom))/2 + ((_random.NextDouble() - 0.5)*baseHeight)));
            SetCell(right, yCentre, Math.Floor((GetCell(right, top) + GetCell(right, bottom))/2 + ((_random.NextDouble() - 0.5)*baseHeight)));

            if ((right - left) > 2)
            {
                baseHeight = Math.Floor(baseHeight*Math.Pow(2.0, -_variability));
                DiamondSquare(left, top, xCentre, yCentre, baseHeight);
                DiamondSquare(xCentre, top, right, yCentre, baseHeight);
                DiamondSquare(left, yCentre, xCentre, bottom, baseHeight);
                DiamondSquare(xCentre, yCentre, right, bottom, baseHeight);
            }
        }
    }

    public class Heightmap2
    {
        private readonly int[,] _map;
        private readonly Random _random;
        private Size _size;

        public Heightmap2(Size size)
        {
            _size = size;

            _map = new int[size.Width, size.Height];
            _random = new Random();
        }

        public int GetCell(int x, int y)
        {
            return _map[y, x];
        }

        public void Run()
        {
            // 0 = sea
            // 1 = lake
            // 2 = land
            // 3 = hill
            // 4 = mountain

            var inputMap = BuildInputMap();

            for (int i = 0; i < _size.Width; i++)
            {
                for (int j = 0; j < _size.Height; j++)
                {
                    _map[i, j] = Noise(inputMap, i, j);
                }
            }
        }

        private int[,] BuildInputMap()
        {
            var inputMap = new int[_size.Width + 1, _size.Height + 1];

            for (int i = 0; i < _size.Width + 1; i++)
            {
                for (int j = 0; j < _size.Height + 1; j++)
                {
                    inputMap[i, j] = _random.Next(_random.Next(0, 5), 5);
                }
            }
            return inputMap;
        }

        private int Noise(int[,] inputMap, int i, int j)
        {
            int first = inputMap[i, j];
            int second = inputMap[i, j + 1];
            int third = inputMap[i + 1, j];
            int fourth = inputMap[i + 1, j + 1];

            return (first + second + third + fourth) / 4;
        }

        private int Noise1(int[,] inputMap, int i, int j)
        {
            int first = inputMap[i, j];
            int second = inputMap[i, j + 1];
            int third = inputMap[i + 1, j];
            int fourth = inputMap[i + 1, j + 1];

            int smallest = Math.Min(first, second);
            smallest = Math.Min(smallest, third);
            smallest = Math.Min(smallest, fourth);

            return smallest;
        }
    }
}