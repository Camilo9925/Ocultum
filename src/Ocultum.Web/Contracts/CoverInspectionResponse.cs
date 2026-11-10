using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record CoverInspectionResponse(StegoFormat Format, string FormatDescription, long SizeBytes, long CapacityBytes)
{
    public static CoverInspectionResponse From(CoverInspection inspection) =>
        new(inspection.Format, inspection.FormatDescription, inspection.SizeBytes, inspection.CapacityBytes);
}
