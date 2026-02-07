using System.CommandLine;
using System.CommandLine.Invocation;

namespace PkgMaker.Utils;

internal sealed class RecognizedCommandAction
(
    Func<ParseResult, CancellationToken, Task> func
) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = new())
    {
        await func(parseResult, cancellationToken);
        return 0;
    }
}
