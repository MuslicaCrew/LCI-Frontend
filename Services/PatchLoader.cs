using System.IO;

namespace LungCancerIdentifierFrontEnd.Services
{
    public static class PatchLoader
    {
        public const int Size = 64;

        public static float[] Load(string binPath)
        {
            var bytes = File.ReadAllBytes(binPath);
            int expected = Size * Size * Size * sizeof(float);
            if (bytes.Length != expected)
                throw new InvalidDataException($"Expected {expected} bytes, got {bytes.Length}.");

            var floats = new float[Size * Size * Size];
            System.Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }
    }
}