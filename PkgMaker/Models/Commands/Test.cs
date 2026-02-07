using System.CommandLine;

namespace PkgMaker.Models.Commands;

internal sealed class Test : Command
{
    public Test() : base(nameof(Test).ToLowerInvariant(), "test") => Hidden = true;
}
