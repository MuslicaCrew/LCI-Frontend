using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LungCancerIdentifierFrontEnd.Services
{
    public static class PatchExtractor
    {
        public const int PatchSize = 64;

        // *** IMPORTANT *** these must match precompute_patches.py exactly.
        // The most common LUNA16 setup: clip to [-1000, 400] HU and rescale to [0, 1].
        // If your pipeline used different bounds (e.g. [-1000, 1000]) change them here.
        private const float HuMin = -1000f;
        private const float HuMax = 400f;

        public static float[] ExtractAndNormalize(Volume volume, int cx, int cy, int cz)
        {
            const int half = PatchSize / 2;
            var patch = new float[PatchSize * PatchSize * PatchSize];

            int idx = 0;
            for (int z = 0; z < PatchSize; z++)
            {
                int vz = cz - half + z;
                for (int y = 0; y < PatchSize; y++)
                {
                    int vy = cy - half + y;
                    for (int x = 0; x < PatchSize; x++)
                    {
                        int vx = cx - half + x;
                        float hu = volume.GetVoxel(vx, vy, vz);
                        float clipped = Math.Clamp(hu, HuMin, HuMax);
                        patch[idx++] = (clipped - HuMin) / (HuMax - HuMin);
                    }
                }
            }
            return patch;
        }
    }
}
