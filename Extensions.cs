using System.IO.Compression;

namespace KCD2_PAK;

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

    public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive zipArchive, FileInfo source, RelativePath entry) => zipArchive.CreateEntryFromFile(source.FullName, entry.ToString('/'));

    public static uint GetCompressedSize(this FileInfo fileInfo)
    {
        return 0;
    }
}
