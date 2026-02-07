namespace ToolStation.Ps3Formats.Sfo;

public class SfoKey
{
    public required string Name { get; init; }
    public required byte[] ByteValue { get; init; }
    public required int Length { get; init; }
    public required int MaxLength { get; init; }
    public required object Value { get; init; }

    public override string ToString() => $"{(Value is int ? "int": "str")} {Name,16}({Length,4}/{MaxLength,4}) = [{Value}]";
}
