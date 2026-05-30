using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

                var options = new SessionOptions();
                // options.AppendExecutionProvider_CUDA(0); // if using the GPU package

                _session = new InferenceSession(modelPath, options);

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

        public static void RunInference(float[] data, int[] shape)
        {
            if (_session is null)
            {
                Debug.WriteLine("[ONNX] Session is null — model not loaded.");
                return;
            }

            var expected = shape.Aggregate(1, (a, b) => a * b);
            if (data.Length != expected)
            {
                Debug.WriteLine($"[ONNX] Size mismatch: data={data.Length}, expected={expected}");
                return;
            }

            try
            {
                var inputName = _session.InputMetadata.Keys.First();
                var tensor = new DenseTensor<float>(data, shape);
                var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

                using var results = _session.Run(inputs);

                foreach (var r in results)
                {
                    var t = r.AsTensor<float>();
                    var arr = t.ToArray();
                    var dims = string.Join(",", t.Dimensions.ToArray());

                    Debug.WriteLine($"[ONNX] output '{r.Name}' shape=[{dims}] n={arr.Length}");

                    if (arr.Length == 1)
                    {
                        // classifier head — sigmoid is baked in, so this is a probability
                        Debug.WriteLine($"[ONNX]   cancer probability = {arr[0]:F6}");
                    }
                    else
                    {
                        // segmentation head — raw logits, apply sigmoid externally if you want probs
                        Debug.WriteLine($"[ONNX]   logits  min={arr.Min():F4}  max={arr.Max():F4}  mean={arr.Average():F4}");
                        var preview = string.Join(", ", arr.Take(10).Select(v => v.ToString("F4")));
                        Debug.WriteLine($"[ONNX]   first 10 = [{preview}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ONNX] Inference failed: {ex.Message}");
            }
        }

        public static void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }
}