using System.CommandLine;
using System.CommandLine.Help;
using PkgMaker.Utils;
using ToolStation.Ps3Formats.Pkg.Enums;

namespace PkgMaker.Models.Commands;

internal sealed class Manual : Command
{
    public Manual() : base(nameof(Manual).ToLowerInvariant(), Desc)
    {
        Options.Add(Input);
        Options.Add(PkgName);
        Options.Add(ContentType);
        Options.Add(PackageType);
        Options.Add(Output);
        Options.Add(Force);
        Options.Add(new HelpOption { Action = new ExtendedHelpAction(Examples) });
    }

    private static string Examples(string app) =>
        $"""

         Examples:

           Pack something
           > {app} manual -i "path/to/example"
             out/example.pkg with contents of example directory, not that it is useful because ContentId is empty

           Pack mod for a game
           > {app} manual -i "my_mod" -c GameData -n TEST00-BLES00000_00-0000000000000000
             pkg that writes data to dev_hdd0/game/BLES00000 because package name has valid ContentId

           Pack with specific types
           > {app} manual -i "example" -c 16 -p 0x123ABC
             Enum values: content type as int, arbitrary package type as hex

         Enum values:
           Supported formats: int (10), hex (0xA), known name (Widget). All case-insensitive. Unknown values also supported when passed as numbers.
           {PkgContentType.SerializeAsHelp()}
           {PkgType.SerializeAsHelp()}

         Notes:
         * Files are packed with flags = Raw|Overwrites
         * All EBOOT.BIN files are packed with flags = NPDRM|Overwrites, because it's required i guess?
         * PARAM.SFO is not interpreted in any way. You have to set package and content types yourself
         * Creating pkgs that write to absolute paths is not allowed - use "installer" command for that
         """;

    private const string Desc = """
                                Generate .pkg with manually controlled parameters
                                """;

    public static readonly Option<DirectoryInfo> Input = new("--input", "-i")
    {
        Description = "Directory to pack",
        HelpName = "dir"
    };

    public static readonly Option<DirectoryInfo> Output = new("--output", "-o")
    {
        Description = "Directory to place generated .pkg",
        DefaultValueFactory = _ => new DirectoryInfo("out"),
        HelpName = "dir"
    };

    public static readonly Option<bool> Force = new("--force", "-f") { Description = "Overwrite existing files" };

    public static readonly Option<string?> PkgName = new("--name", "-n")
    {
        Description = "Package name. [default: name of input directory]",
        HelpName = "name",
    };

    public static readonly Option<string?> ContentType = new("--content-type", "-c")
    {
        Description = "PkgContentType for metadata header. See below for supported values [default: autodetect from input files]",
        HelpName = "x"
    };

    public static readonly Option<string?> PackageType = new("--package-type", "-p")
    {
        Description = "PkgType for metadata header. See below for supported values [default: autodetect from content type]",
        HelpName = "x"
    };

}
