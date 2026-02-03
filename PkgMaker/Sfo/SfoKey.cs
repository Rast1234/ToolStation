namespace PkgMaker.Sfo;

public class SfoKey
{
    public required string Name { get; init; }
    public required byte[] ByteValue { get; init; }
    public required int Length { get; init; }
    public required int MaxLength { get; init; }
    public required object Value { get; init; }
}