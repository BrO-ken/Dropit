using Dropit.Core;
using Dropit.Core.Operations;

namespace Dropit.Linux;

public static class LinuxOperations
{
    /// <summary>Conversions and tools (no UI needed), as used by the CLI and the wheel.</summary>
    public static IReadOnlyList<Operation> Conversions(LinuxSettings settings) => [.. Catalog.Build(), new TesseractOcr(settings)];
}
