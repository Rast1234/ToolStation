using ToolStation.Ps3Formats.Pkg.Enums;

namespace ToolStation.Ps3Formats.Pkg.Entries;

public class ParamSfoFile
(
    string category,
    string path,
    Stream content,
    PkgFileFlags? flags = null
) : PkgFile(path, content, flags)
{
    public string Category => category;
}
