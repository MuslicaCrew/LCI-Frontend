using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LungCancerIdentifierFrontEnd.Services
{
    public class InferenceResult
    {
        public required float ClsProbability { get; init; }
        public required float[] SegLogits { get; init; }
        public required int[] SegShape { get; init; }
    }

    public static class OnnxModelService
    {
        private static InferenceSession? _session;
        public static bool IsLoaded => _session is not null;

        public static void Load(string modelPath)
        {
            try
            {
                if (!File.Exists(modelPath))
                {
                    Debug.WriteLine($"[ONNX] Model not found: {modelPath}");
                    return;
                }
                _session = new InferenceSession(modelPath, new SessionOptions());
                Debug.WriteLine($"[ONNX] Loaded model: {modelPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ONNX] Load failed: {ex.Message}");
            }
        }

        public static InferenceResult? RunInference(float[] data, int[] shape)
        {
            if (_session is null)
            {
                Debug.WriteLine("[ONNX] Session is null — model not loaded.");
                return null;
            }

            try
            {
                var inputName = _session.InputMetadata.Keys.First();
                var tensor = new DenseTensor<float>(data, shape);
                var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

                using var results = _session.Run(inputs);

                float clsProb = 0f;
                float[] segLogits = Array.Empty<float>();
                int[] segShape = Array.Empty<int>();

                foreach (var r in results)
                {
                    var t = r.AsTensor<float>();
                    var arr = t.ToArray();
                    if (r.Name == "cls_prob") clsProb = Sigmoid(arr[0]);
                    else if (r.Name == "seg_map")
                    {
                        segLogits = arr;
                        segShape = t.Dimensions.ToArray();
                    }
                }

                Debug.WriteLine($"[ONNX] cls_prob = {clsProb:F4}  seg min/max/mean = " +
                                $"{segLogits.Min():F3}/{segLogits.Max():F3}/{segLogits.Average():F3}");

                return new InferenceResult
                {
                    ClsProbability = clsProb,
                    SegLogits = segLogits,
                    SegShape = segShape
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ONNX] Inference failed: {ex.Message}");
                return null;
            }
        }

        public static void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }

        private static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));
    }
}