namespace EosToQLab.Core.Exceptions;

public sealed class QLabSlipFrameException : EosToQLabException
{
    public QLabSlipFrameException(byte escapeByte)
        : base(
            "QLAB_SLIP_FRAME_INVALID",
            $"The SLIP frame contains an invalid escape byte 0x{escapeByte:X2}.")
    {
    }
}