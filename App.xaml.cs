using System.Windows;
using LungCancerIdentifierFrontEnd.Services;

namespace LungCancerIdentifierFrontEnd
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Adjust path. If shipping the model with the app, set its Build Action to
            // "Content" + Copy to Output Directory, then use a relative path here.
            //OnnxModelService.Load(@"Models\unet3d.onnx");
            // Relative path
            OnnxModelService.Load(@"..\Models\unet3d.onnx");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            OnnxModelService.Dispose();
            base.OnExit(e);
        }
    }
}