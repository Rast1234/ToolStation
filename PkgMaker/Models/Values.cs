using PkgMaker.Sfo;

namespace PkgMaker.Models;

public class Values
{
    public string? Base { get; set; }
    public DirectoryInfo? Output { get; set; }
    public bool Force { get; set; }
    public bool Preview { get; set; }
    public string? Title { get; set; }
    public string? Game { get; set; }
    public string? TitleId { get; set; }
    public string? ContentId { get; set; }
    public string? Label { get; set; }
    public int Timeout { get; set; }
    public DateTimeOffset Now { get; set; }

    /// <summary>
    /// PARAM.SFO
    /// </summary>
    public ParamSfo? ParamSfo { get; set; }

    /// <summary>
    /// USRDIR/launch.txt
    /// </summary>
    public byte[]? Command { get; set; }

    /// <summary>
    /// USRDIR/script.txt
    /// </summary>
    public byte[]? Script { get; set; }

    /// <summary>
    /// ICON0.PNG 320*176 mandatory
    /// </summary>
    public byte[]? Icon { get; set; }

    /// <summary>
    /// PIC1.PNG 1920*1080
    /// </summary>
    public byte[]? Background { get; set; }

    /// <summary>
    /// PIC0.PNG 1000*560 for 16:9
    /// </summary>
    public byte[]? Overlay { get; set; }

    /// <summary>
    /// PIC2.PNG 310*250 for 4:3
    /// </summary>
    public byte[]? OverlaySd { get; set; }

    /// <summary>
    /// ICON1.PAM 320*176
    /// </summary>
    public byte[]? Video { get; set; }

    /// <summary>
    /// SND0.AT3
    /// </summary>
    public byte[]? Sound { get; set; }

    /// <summary>
    /// PARAM.HIS
    /// </summary>
    public byte[]? ParamHis { get; set; }

    public string ToInitString()
    {
        return $"""
                Initial parameters:
                    {nameof(Force)}=[{Force}], {nameof(Preview)}=[{Preview}], Script {nameof(Timeout)}=[{Timeout}]
                    {nameof(Base)}=[{Base}], {nameof(Output)}=[{Str(Output)}]
                """;
    }

    public string ToInfoString(string message)
    {
        return $"""
                {message}:
                    {nameof(Title)} (XMB app name)=[{Title}], {nameof(TitleId)} (install folder)=[{TitleId}], {nameof(Label)}=[{Label}]
                    {nameof(ContentId)} (package name)=[{ContentId}]
                    {nameof(Game)} partial filename for webman search=[{Game}]
                    {nameof(ParamSfo)}=[{Str(ParamSfo)}], {nameof(ParamHis)}=[{Str(ParamHis)}], {nameof(Command)}=[{Str(Command)}], {nameof(Script)}=[{Str(Script)}]
                    {nameof(Icon)}=[{Str(Icon)}], {nameof(Background)}=[{Str(Background)}], {nameof(Overlay)}=[{Str(Overlay)}]
                    {nameof(Video)}=[{Str(Video)}], {nameof(Sound)}=[{Str(Sound)}], {nameof(OverlaySd)}=[{Str(OverlaySd)}]
                """;
    }

    private string Str(byte[]? value)
    {
        return value is null
            ? "null"
            : $"{value.Length} bytes";
    }

    private string Str(ParamSfo? value)
    {
        return value is null
            ? "null"
            : $"{value.Data.Count} entries";
    }

    private string Str(FileSystemInfo? value)
    {
        return value is null
            ? "null"
            : value.FullName;
    }
}
