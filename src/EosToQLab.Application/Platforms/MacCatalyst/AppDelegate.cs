using Foundation;

namespace EosToQLab.Application;

[Register("AppDelegate")]
internal sealed class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }
}