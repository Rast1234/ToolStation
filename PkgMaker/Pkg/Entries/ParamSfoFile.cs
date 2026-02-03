using PkgMaker.Pkg.Enums;

namespace PkgMaker.Pkg.Entries;

public class ParamSfoFile(string category, string path, Stream content, PkgFileFlags? flags = null)
    : PkgFile(path, content, flags)
{
    public string Category => category;
}