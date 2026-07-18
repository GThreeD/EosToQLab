using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Models;

namespace EosToQLab.Core.Planning;

public interface IQLabImportPlanBuilder
{
    QLabImportPlan Build(
        IReadOnlyList<EosCue> cues,
        QLabImportOptions options,
        ICollection<EosDiagnostic>? diagnostics = null);
}
