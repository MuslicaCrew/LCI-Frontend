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
        private static InferenceSession? session;
        public static bool IsLoaded => session is not null;

        public static void Load(string modelPath)
        {
            try
            {
                if (!File.Exists(modelPath))
                {
                    Debug.WriteLine($"\n\n[ONNX] Model not found: {modelPath}\n\n");
                    return;
                }                       
                session = new InferenceSession(modelPath);
                Debug.WriteLine($"\n\n[ONNX] Loaded model: {modelPath}\n\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n\n[ONNX] Load failed: {ex.Message}\n\n");
            }
        }

        public class InferenceResult
        {
            public required float ClsProbability { get; init; }
            public required float[] SegLogits { get; init; }
        }

        public static InferenceResult? RunInference(float[] data, int[] shape)
        {     
            float ThresholdLogit = (float)Math.Log(0.263f / 0.737f);
            const float T = 0.6927f;

            if (session is null) return null;

            var inputName = session.InputMetadata.Keys.First();
            var tensor = new DenseTensor<float>(data, shape);
            var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

            using var results = session.Run(inputs);

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
            session?.Dispose();
            session = null;
        }
    }
}