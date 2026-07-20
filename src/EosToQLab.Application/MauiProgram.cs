using EosToQLab.Core.Import;
using EosToQLab.Core.Planning;
using EosToQLab.Core.QLab;
using EosToQLab.Core.Security;
using EosToQLab.Infrastructure.Import;
using EosToQLab.Infrastructure.Import.Csv;
using EosToQLab.Infrastructure.Import.EosShowArchive;
using EosToQLab.Infrastructure.QLab;
using EosToQLab.Infrastructure.QLab.Workflow;

namespace EosToQLab.Application;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<Application>();

        builder.Services.AddMauiBlazorWebView();
        // Default: keep passcodes only in the current app process. Nothing is persisted.
        builder.Services.AddSingleton<IQLabPasscodeStore, SessionQLabPasscodeStore>();

        // Optional persistent implementation. Enable this registration only for a signed build
        // that is allowed to use the platform keychain, and remove the session registration above.
        // builder.Services.AddSingleton<IQLabPasscodeStore, MauiSecurePasscodeStore>();
        builder.Services.AddSingleton<IEosShowArchiveCompatibility, EmbeddedEosShowArchiveCompatibility>();
        builder.Services.AddSingleton<IEosCueImporter, CsvEosCueImporter>();
        builder.Services.AddSingleton<IEosCueImporter, EosShowArchiveCueImporter>();
        builder.Services.AddSingleton<IEosCueImporterFactory, EosCueImporterFactory>();
        builder.Services.AddSingleton<IQLabImportPlanBuilder, QLabImportPlanBuilder>();
        builder.Services.AddSingleton<IQLabOscService, QLabOscService>();
        builder.Services.AddSingleton<IQLabPlanItemMapper, QLabMemoCuePlanMapper>();
        builder.Services.AddSingleton<IQLabPlanItemMapper, QLabNetworkCuePlanMapper>();
        builder.Services.AddSingleton<QLabImportPlanExecutor>();
        builder.Services.AddSingleton<QLabImportWorkflow>();
        builder.Services.AddSingleton<IQLabService, QLabService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        //builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}