using System.Diagnostics.CodeAnalysis;

namespace ToolStation.Ps3Formats.Sfo;

[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Binary packable")]
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Value not used")]
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Name is valid")]
public enum SfoDataFormat : short
{
    Utf8 = 0x0204,
    Int32 = 0x0404
}
