using System.CommandLine;

namespace PkgMaker.Models.Commands;

internal sealed class Root : RootCommand
{
    public readonly Launcher Launcher = new();
    public readonly Prepare Prepare = new();
    public readonly Installer Installer = new();
    public readonly Manual Manual = new();
    public readonly Test Test = new();

    public Root() : base(Desc)
    {
        Add(Launcher);
        Add(Prepare);
        Add(Installer);
        Add(Manual);
        Add(Test);
    }

    private const string Desc = """
                                Launch (shortcut) and installer package generator
                                Creates .pkg that launch games from iso or folders, or install files anywhere. Can customize app name, XMB icons/pictures, use existing games as templates, or even execute arbitrary Webman commands

                                For examples and details see help for specific command

                                Greetz to aldostools, Roet-Ivar, InvoxiPlayGames and whole PS3 homebrew scene, all you people are amazing!
                                """;
}
