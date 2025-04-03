using System.IO.Compression;
using KCD2;

namespace KCD2;

public static class Extensions
{
    public static DirectoryInfo Subfolder(this DirectoryInfo directoryInfo, params string[] paths)
    {
        return new DirectoryInfo(Path.Combine([directoryInfo.FullName, .. paths]));
    }

    public static FileInfo File(this DirectoryInfo directoryInfo, params string[] paths)
    {
        return new FileInfo(Path.Combine([directoryInfo.FullName, .. paths]));
    }
}
