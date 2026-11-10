using Ocultum.Core.Models;

namespace Ocultum.Core.Errors;

public sealed class CapacityExceededException : OcultumException
{
    public CapacityExceededException(long? payloadBytes = null, long? capacityBytes = null)
        : base(
            "CAPACITY_EXCEEDED",
            "Not enough capacity in the cover file.",
            payloadBytes is not null && capacityBytes is not null
                ? $"Payload is {ByteSize.Format(payloadBytes.Value)} but the cover holds {ByteSize.Format(capacityBytes.Value)}."
                : null)
    {
        PayloadBytes = payloadBytes;
        CapacityBytes = capacityBytes;
    }

    public long? PayloadBytes { get; }

    public long? CapacityBytes { get; }

    public override IReadOnlyDictionary<string, object?> Extensions => new Dictionary<string, object?>
    {
        ["payloadBytes"] = PayloadBytes,
        ["capacityBytes"] = CapacityBytes,
    };
}
