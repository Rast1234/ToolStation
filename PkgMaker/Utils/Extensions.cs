using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Numerics;

namespace PkgMaker.Utils;

public static class Extensions
{
    public static T PaddingTo16Length<T>(this T value)
        where T : IBinaryInteger<T>
    {
        var x = T.One << 4;
        return (x - value % x) % x;
    }

    public static void InvokeCurrentCommandHelp(this ParseResult parseResult)
    {
        (parseResult.CommandResult.Command.Options.OfType<HelpOption>().Single().Action as SynchronousCommandLineAction)!.Invoke(parseResult);
    }
}