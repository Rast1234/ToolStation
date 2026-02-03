using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace PkgMaker.Utils;

internal class ExtendedHelpAction(Func<string, string> exampleProvider) : SynchronousCommandLineAction
{
    public override bool ClearsParseErrors => actualAction.ClearsParseErrors;
    private readonly HelpAction actualAction = new();

    public override int Invoke(ParseResult parseResult)
    {
        var result = actualAction.Invoke(parseResult);
        Console.WriteLine(exampleProvider(parseResult.RootCommandResult.IdentifierToken.Value));
        return result;
    }
}