using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LungCancerIdentifierFrontEnd.Services
{
    public class Volume
    {
        public required short[] Data { get; init; }   // raw HU values, z-major (z*W*H + y*W + x)
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required int Depth { get; init; }
        public required (double X, double Y, double Z) Spacing { get; init; }
        public required (double X, double Y, double Z) Origin { get; init; }

        public short GetVoxel(int x, int y, int z)
        {
            if ((uint)x >= Width || (uint)y >= Height || (uint)z >= Depth)
                return -1000; // pad with air outside the volume
            return Data[(long)z * Width * Height + y * Width + x];
        }
    }
}
