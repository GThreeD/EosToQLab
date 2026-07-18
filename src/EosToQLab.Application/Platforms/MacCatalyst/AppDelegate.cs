using Foundation;

namespace EosToQLab.Application;

[Register("AppDelegate")]
internal class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }
}