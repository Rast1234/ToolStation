using System.CommandLine;

namespace PkgMaker.Models.Commands;

public class Test : Command
{
    public Test() : base(nameof(Test).ToLowerInvariant(), "test")
    {
        Hidden = true;
    }
}