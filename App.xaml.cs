using System.Windows;
using LungCancerIdentifierFrontEnd.Services;

namespace LungCancerIdentifierFrontEnd
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            OnnxModelService.Load(@"..\Models\unet3d.onnx");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            OnnxModelService.Dispose();
            base.OnExit(e);
        }
    }
}