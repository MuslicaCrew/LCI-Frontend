using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static LungCancerIdentifierFrontEnd.Services.OnnxModelService;
using static LungCancerIdentifierFrontEnd.Views.StartPage;

namespace LungCancerIdentifierFrontEnd.Services
{
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
                    Debug.WriteLine($"\n\n[ONNX] Model not found: {modelPath}\n\n");
                    return;
                }

         
                _session = new InferenceSession(modelPath);

                Debug.WriteLine($"[ONNX] Loaded model: {modelPath}");
                foreach (var kv in _session.InputMetadata)
                    Debug.WriteLine($"[ONNX]  input  '{kv.Key}' shape=[{string.Join(",", kv.Value.Dimensions)}] dtype={kv.Value.ElementType}");
                foreach (var kv in _session.OutputMetadata)
                    Debug.WriteLine($"[ONNX]  output '{kv.Key}' shape=[{string.Join(",", kv.Value.Dimensions)}] dtype={kv.Value.ElementType}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ONNX] Load failed: {ex.Message}");
            }
        }

        public class InferenceResult
        {
            public required float ClsProbability { get; init; }
            public required float[] SegLogits { get; init; }
        }

        public static InferenceResult? RunInference(float[] data, int[] shape)
        {     
            float ThresholdLogit = (float)Math.Log(0.221f / 0.779f);  // logit(0.221) — the F1-optimal threshold
            const float T = 0.4f;

            if (_session is null) return null;

            var inputName = _session.InputMetadata.Keys.First();
            var tensor = new DenseTensor<float>(data, shape);
            var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

            using var results = _session.Run(inputs);

            float clsLogit = 0f;
            float[] segLogits = Array.Empty<float>();
            foreach (var result in results)
            {
                var arr = result.AsTensor<float>().ToArray();
                if (result.Name == "cls_prob") clsLogit = arr[0];
                else if (result.Name == "seg_map") segLogits = arr;
            }

            return new InferenceResult
            {
              
                ClsProbability = 1f / (1f + MathF.Exp(-(clsLogit - ThresholdLogit) / T)),  // sigmoid
                SegLogits = segLogits
            };
        }

        public static void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }
}