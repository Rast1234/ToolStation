using System.Numerics;

namespace ToolStation.Ps3Formats.Utils;

public static class Extensions
{
    public static T PaddingTo16Length<T>(this T value)
        where T : IBinaryInteger<T>
    {
        var x = T.One << 4;
        return (x - value % x) % x;
    }
}
