using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LungCancerIdentifierFrontEnd.Services
{
    public static class MhdLoader
    {
        public static Volume Load(string mhdPath)
        {
            var header = ParseHeader(mhdPath);

            var dims = header["DimSize"].Split(' ').Select(int.Parse).ToArray();
            var spacing = header["ElementSpacing"].Split(' ')
                .Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
            var origin = header.TryGetValue("Offset", out var off)
                ? off.Split(' ').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray()
                : new[] { 0.0, 0.0, 0.0 };

            if (header.TryGetValue("CompressedData", out var c) && c.Equals("True", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Compressed .zraw not supported. Re-save the scan uncompressed.");

            var elementType = header["ElementType"];
            if (elementType != "MET_SHORT")
                throw new NotSupportedException($"Only MET_SHORT (int16) supported, got {elementType}.");

            var dataFile = header["ElementDataFile"];
            var rawPath = Path.IsPathRooted(dataFile)
                ? dataFile
                : Path.Combine(Path.GetDirectoryName(mhdPath) ?? "", dataFile);

            var bytes = File.ReadAllBytes(rawPath);
            long voxelCount = (long)dims[0] * dims[1] * dims[2];

            if (bytes.Length != voxelCount * 2)
                throw new InvalidDataException($"Raw file size mismatch: expected {voxelCount * 2} bytes, got {bytes.Length}.");

            var data = new short[voxelCount];
            Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);

            return new Volume
            {
                Data = data,
                Width = dims[0],
                Height = dims[1],
                Depth = dims[2],
                Spacing = (spacing[0], spacing[1], spacing[2]),
                Origin = (origin[0], origin[1], origin[2])
            };
        }

        private static Dictionary<string, string> ParseHeader(string mhdPath)
        {
            var result = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(mhdPath))
            {
                var idx = line.IndexOf('=');
                if (idx < 0) continue;
                result[line[..idx].Trim()] = line[(idx + 1)..].Trim();
            }
            return result;
        }
    }
}
