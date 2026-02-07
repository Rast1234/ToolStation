using System.Reflection;

namespace PkgMaker.Utils;

public static class Resources
{
    public static byte[] GetEmbedded(string name)
    {
        var a = Assembly.GetEntryAssembly();
        var id = $"{a!.GetName().Name}.Files.{name}";
        using var ms = new MemoryStream();
        a.GetManifestResourceStream(id)!.CopyTo(ms);
        return ms.ToArray();
    }
}
