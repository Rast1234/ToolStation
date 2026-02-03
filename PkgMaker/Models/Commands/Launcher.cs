using System.CommandLine;
using System.CommandLine.Help;
using PkgMaker.Services;
using PkgMaker.Utils;

namespace PkgMaker.Models.Commands;

public class Launcher : Command
{
    public Launcher() : base(nameof(Launcher).ToLowerInvariant(), Desc)
    {
        Options.Add(Base);
        Options.Add(Title);
        Options.Add(Game);
        Options.Add(TitleId);
        Options.Add(Label);
        Options.Add(Command);
        Options.Add(Script);
        Options.Add(Timeout);
        Options.Add(Icon);
        Options.Add(Background);
        Options.Add(Overlay);
        Options.Add(OverlaySd);
        Options.Add(Video);
        Options.Add(Sound);
        Options.Add(ParamSfo);
        Options.Add(ParamHis);
        Options.Add(Preview);
        Options.Add(Output);
        Options.Add(Force);
        Options.Add(new HelpOption {Action = new ExtendedHelpAction(Examples)});
    }

    private static string Examples(string app)
    {
        return $"""

                Examples:

                  Simple shortcut
                  > {app} launcher -t "My launcher" -g "overkill" -icon "my icon.png" -p
                    Specify app title, game to launch, set icon, generate preview. Webman will search for iso or folder with "overkill" in its name, and launch if found only 1 game
                    
                  Using base game
                  > {app} launcher -b "JD4 BLUS31033.iso"
                    With ISO as base, copy its pictures. Set launcher title "Just Dance 4", and game to launch "BLUS31033" - both read from PARAM.SFO 
                    
                  Using base game with overrides
                  > {app} launcher -b "backups/JustDanceMod2020v2" --background "" -g "JustDanceMod"
                    With folder as base, copy pictures but remove background. Set launcher title "Just dance 2020" from PARAM.SFO. Customize game name to launch - beacuse game file/folder is named differently and default search by title_id will fail
                  
                  Using custom base
                  > {app} launcher -b "folder/with/stuff" -t "my title" -g "my game"
                    With folder as base, copy pictures. There is no PARAM.SFO, so set launcher title and game name to launch

                  Web command
                  > {app} launcher -t $'beep beep!\nwait\nbeep beep!' -l BEEPEREXAMPLE000 -c "/beep.ps3?2;/wait.ps3?1;/beep.ps3?2" -s ""
                    Specify multiline app title (bash syntax), label, custom Webman command, and exclude script file because we won't need it anyway

                Debugging:
                  * Check if Webman is OK, launcher relies on its web commands
                  * Launcher files are in /dev_hdd0/game/Wxxxxxxxx/USRDIR
                  * launch.txt has HTTP command for Webman, try it in browser manually
                  * script.txt is a Webman bat that is called from HTTP command. Try executing it yourself, inserting beep1/beep2 between lines to see where it fails
                  * "Update History" in XMB has launch and script contents for reference
                """;
    }

    private const string Desc = """
                                Generate customizable .pkg to launch specified game
                                """;

    public static readonly Option<string> Base = new("--base", "-b")
    {
        Description = "Directory, iso, HTTP or FTP address to guess title for launcher, game id to launch, and pictures. Required if -t or -g are not set. All other options override values parsed from base",
        HelpName = "dir_iso_url"
    };

    public static readonly Option<DirectoryInfo> Output = new("--output", "-o")
    {
        Description = "Directory to place generated .pkg",
        DefaultValueFactory = _ => new DirectoryInfo("out"),
        HelpName = "dir"
    };

    public static readonly Option<bool> Force = new("--force", "-f")
    {
        Description = "Overwrite existing files"
    };

    public static readonly Option<string> Title = new("--title", "-t")
    {
        Description = "App name displayed in XMB. Up to 127 characters, can use newline up to 2 times. Required if base is not set or does not provide a title. [default: read from -b]",
        HelpName = "TITLE"
    };

    public static readonly Option<string> TitleId = new("--id", "-i")
    {
        Description = $"Package id and installation folder. Must be unique 9-char string [A-Z0-9]. Example: {MetadataProvider.Prefix}12AB3C45. [default: generated as {MetadataProvider.Prefix}+HASH(title)]",
        HelpName = "TITLE_ID"
    };

    public static readonly Option<string> Label = new("--label", "-l")
    {
        Description = "Package label. Will be formatted into 16-char string of [A-Z0-9], padded with 0s. Example: MYGAMETITLE00000. [default: based on TITLE from -t or -b]",
        HelpName = "LABEL"
    };

    public static readonly Option<string> Command = new("--command", "-c")
    {
        Description = "Webman command to execute [default: execute script from -s]",
        HelpName = "CMD"
    };

    public static readonly Option<string> Script = new("--script", "-s")
    {
        Description = "Custom Webman script to execute. NONE to exclude file. Docs: https://github.com/aldostools/webMAN-MOD/wiki/Web-Commands [default: find game by id given in -b or -g, mount, wait and launch]",
        HelpName = "file"
    };

    public static readonly Option<string> Game = new("--game", "-g")
    {
        Description = "Path or filename of a game to find and launch. Full or patrial, for example TITLEID if your filenames have it [default: TITLE_ID, name of iso or folder from -b]",
        HelpName = "GAMENAME"
    };

    public static readonly Option<int?> Timeout = new("--timeout")
    {
        Description = "Script will wait several seconds after mounting a game before launching it. Increase if default does not cut it for you [default: 1]",
        HelpName = "seconds"
    };

    public static readonly Option<string> ParamHis = new("--param-his")
    {
        Description = "Text for update history in PARAM.HIS file. NONE to exclude file. [default: generated with parameters passed to app]",
        HelpName = "text"
    };

    public static readonly Option<string> Icon = new("--icon", "--icon0")
    {
        Description = "App icon (ICON0), must be 320x176 png. NONE to force default. [default: transparent]",
        HelpName = "png"
    };

    public static readonly Option<string> Video = new("--video", "--icon1")
    {
        Description = "App animated icon (ICON1), must be 320x176 pam video, embedded sound supported, up to 2.4MB with --sound combined. No validation. NONE to exclude file",
        HelpName = "pam"
    };

    public static readonly Option<string> Sound = new("--sound", "--snd0")
    {
        Description = "App sound (SND0), must be at3 sound, up to 2.4MB with --video combined, overrides sound from video file. No validation. NONE to exclude file",
        HelpName = "at3"
    };

    public static readonly Option<string> Background = new("--background", "--pic1")
    {
        Description = "App background (PIC1), must be 1920x1080 png. NONE to exclude file",
        HelpName = "png"
    };

    public static readonly Option<string> Overlay = new("--overlay", "--pic0")
    {
        Description = "App overlay (PIC0), must be 1000x560 png. NONE to exclude file",
        HelpName = "png"
    };

    public static readonly Option<string> OverlaySd = new("--overlay-sd", "--pic2")
    {
        Description = "App overlay for 4:3 display modes (PIC2), must be 310x250 png. NONE to exclude file",
        HelpName = "png"
    };

    public static readonly Option<string> ParamSfo = new("--param-sfo")
    {
        Description = "Custom file to use for launcher. NONE to exclude file. [default: generated]",
        HelpName = "PARAM.SFO"
    };

    public static readonly Option<bool> Preview = new("--preview", "-p")
    {
        Description = "Generate preview how icon, background and overlay pictures will look in XMB. Image is saved next to pkg"
    };
}
