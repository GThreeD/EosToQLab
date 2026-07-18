using EosToQLab.Core.Import;
using EosToQLab.Core.Planning;
using EosToQLab.Core.QLab;
using EosToQLab.Infrastructure.Import;
using EosToQLab.Infrastructure.Import.Csv;
using EosToQLab.Infrastructure.Import.Esf3d;
using EosToQLab.Infrastructure.QLab;

namespace EosToQLab.Application;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<Application>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<IEosCueImporter, CsvEosCueImporter>();
        builder.Services.AddSingleton<IEosCueImporter, Esf3dEosCueImporter>();
        builder.Services.AddSingleton<IEosCueImporterFactory, EosCueImporterFactory>();
        builder.Services.AddSingleton<IQLabImportPlanBuilder, QLabImportPlanBuilder>();
        builder.Services.AddSingleton<IQLabService, QLabOscService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        //builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
