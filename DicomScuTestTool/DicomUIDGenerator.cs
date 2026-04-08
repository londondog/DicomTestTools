using FellowOakDicom;

namespace DicomScuTestTool;

internal static class DicomUIDGenerator
{
    public static string GenerateNew() => DicomUID.Generate().UID;
}
