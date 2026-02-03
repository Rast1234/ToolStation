using PkgMaker.Models.Pkg.Enums;

namespace PkgMaker.Models.Pkg.Entries;

public class ParamSfoFile(string category, string path, Stream content, PkgFileFlags? flags = null)
    : PkgFile(path, content, flags)
{
    public string Category => category;
}