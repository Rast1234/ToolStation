using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PkgMaker.Utils;

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Generic extensions are cool")]
public static class Extensions
{
    public static void InvokeCurrentCommandHelp(this ParseResult parseResult) => (parseResult.CommandResult.Command.Options.OfType<HelpOption>().Single().Action as SynchronousCommandLineAction)!.Invoke(parseResult);

    extension<TEnum>(TEnum)
        where TEnum : struct, Enum
    {
        public static string SerializeAsHelp()
        {
            var items = new List<string>();
            foreach (var x in Enum.GetValues<TEnum>())
            {
                var hex = $"{x:X}".TrimStart('0');
                if (string.IsNullOrEmpty(hex))
                {
                    hex = "0";
                }

                items.Add($"{x}=0x{hex}");
            }

            return $"{typeof(TEnum).Name}: " + string.Join(", ", items);
        }

        /// <summary>
        /// Try parsing any INT, any 0xHEX, or known string
        /// </summary>
        public static TEnum? ParseAny(string? value) =>
            value switch
            {
                _ when string.IsNullOrWhiteSpace(value) => null,
                _ when ulong.TryParse(value, out var a) => Enum.Parse<TEnum>(a.ToString()),
                _ when value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && ulong.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b) => Enum.Parse<TEnum>(b.ToString()),
                _ when Enum.TryParse<TEnum>(value, true, out var c) => c,
                _ => throw new ArgumentOutOfRangeException($"Can't parse value [{value}] to enum [{typeof(TEnum).Name}]")
            };
    }
}
