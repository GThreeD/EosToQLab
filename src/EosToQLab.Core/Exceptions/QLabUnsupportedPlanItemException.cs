namespace EosToQLab.Core.Exceptions;

public sealed class QLabUnsupportedPlanItemException : EosToQLabException
{
    public QLabUnsupportedPlanItemException(string itemType)
        : base(
            "QLAB_PLAN_ITEM_UNSUPPORTED",
            $"The QLab import plan item type '{itemType}' is not supported.")
    {
    }
}