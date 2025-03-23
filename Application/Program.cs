using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KCD2_PAK;

public class Program
{
    private static readonly NuGet.Versioning.SemanticVersion ApplicationVersion = new(1, 2, 0);

    public static void Main(string[] args)
    {
        Console.WriteLine($"KCD2 PAK v{ApplicationVersion}");
        Console.WriteLine();

        CheckVersion();

        foreach (var arg in args)
        {
            if (TryGetFullPath(arg, out var fullPath))
            {
                if (IsDirectory(fullPath))
                {
                    var directory = new DirectoryInfo(fullPath);
                    var modId = GetModId(directory.FullName);

                    Console.WriteLine($"Folder: {directory}");
                    if (modId is not null)
                    {
                        Console.WriteLine($"Mod ID: {modId}");
                        Console.WriteLine();

                        PakData(directory, modId);
                        PakLocalization(directory);
                        PakLevels(directory);
                    }
                    else
                    {
                        Console.WriteLine("Missing Mod ID!");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine($"{arg} is not a folder!");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine($"Invalid Folder: {arg}");
                Console.WriteLine();
            }
        }

        Console.Write("Press any key to continue . . . ");
        Console.ReadKey();
    }

    public static void CheckVersion()
    {
        var latestVersion = GetLatestVersion();
        if (latestVersion is null)
            return;

        if (latestVersion > ApplicationVersion)
        {
            Console.WriteLine($"KCD2 PAK v{latestVersion} is now available!");
            Console.WriteLine("You can download the latest version using the below links.");
            Console.WriteLine("https://github.com/7H3LaughingMan/KCD2-PAK/releases/latest");
            Console.WriteLine("https://www.nexusmods.com/kingdomcomedeliverance2/mods/1482");
            Console.WriteLine();
        }
    }

    public static NuGet.Versioning.SemanticVersion? GetLatestVersion()
    {
        var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("KCD2_PAK"));
        var latestRelease = github.Repository.Release.GetLatest("7H3LaughingMan", "KCD2-PAK").WaitAndUnwrapException();

        if (latestRelease is not null)
            return NuGet.Versioning.SemanticVersion.Parse(latestRelease.TagName[1..]);

        return null;
    }

    public static string? GetModId(string path)
    {
        if (!Directory.Exists(path))
            return null;

        if (!File.Exists(Path.Combine(path, "mod.manifest")))
            return null;

        try
        {
            var document = XDocument.Load(Path.Combine(path, "mod.manifest"));
            return document.XPathSelectElement("/kcd_mod/info/modid")?.Value;
        }
        catch (Exception) { }

        return null;
    }

    public static bool TryGetFullPath(string path, out string fullPath)
    {
        fullPath = string.Empty;

        if (String.IsNullOrWhiteSpace(path))
            return false;

        bool status = false;

        try
        {
            fullPath = Path.GetFullPath(path);
            status = true;
        }
        catch (Exception) { }

        return status;
    }

    public static bool IsDirectory(string path)
    {
        var fileAttributes = File.GetAttributes(path);
        return fileAttributes.HasFlag(FileAttributes.Directory);
    }

    public static IEnumerable<(FileInfo, RelativePath)> GetFiles(DirectoryInfo directoryInfo, Predicate<RelativePath>? predicate = null)
    {
        foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            RelativePath relativePath = new(Path.GetRelativePath(directoryInfo.FullName, fileInfo.FullName));

            if (fileInfo.Extension.Equals(".pak"))
                continue;

            if (predicate is not null)
                if (!predicate(relativePath))
                    continue;

            yield return (fileInfo, relativePath);
        }
    }

    private static readonly RelativePath Levels = new("Levels/");

    public static void PakData(DirectoryInfo directoryInfo, string modId)
    {
        DirectoryInfo dataDirectory = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "Data"));

        if (!dataDirectory.Exists)
            return;

        var pakPath = Path.Combine(dataDirectory.FullName, $"{modId}.pak");
        var relativePakPath = new RelativePath(Path.GetRelativePath(directoryInfo.FullName, pakPath));
        var relativeDataPath = new RelativePath(Path.GetRelativePath(directoryInfo.FullName, dataDirectory.FullName));

        using FileStream zipFile = File.Open(pakPath, FileMode.Create);
        using ZipArchive zipArchive = new(zipFile, ZipArchiveMode.Create);
        Console.WriteLine($"Creating {relativePakPath}");

        foreach ((FileInfo fileInfo, RelativePath relativePath) in GetFiles(dataDirectory, x => !x.IsWithinFolder(Levels)))
        {
            Console.WriteLine($"Adding {relativeDataPath + relativePath}");
            ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(relativePath.ToString('/'));
            using FileStream fileStream = fileInfo.OpenRead();
            using Stream zipArchiveStream = zipArchiveEntry.Open();
            fileStream.CopyTo(zipArchiveStream);
        }

        Console.WriteLine();
    }

    public static void PakLocalization(DirectoryInfo directoryInfo)
    {
        DirectoryInfo localizationDirectory = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "Localization"));

        if (!localizationDirectory.Exists)
            return;

        foreach (DirectoryInfo localization in localizationDirectory.EnumerateDirectories())
        {
            var pakPath = Path.Combine(localizationDirectory.FullName, $"{localization.Name}.pak");
            var relativePakPath = new RelativePath(Path.GetRelativePath(directoryInfo.FullName, pakPath));
            var relativeLocalizationPath = new RelativePath(Path.GetRelativePath(directoryInfo.FullName, localization.FullName));

            using FileStream zipFile = File.Open(pakPath, FileMode.Create);
            using ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create);
            Console.WriteLine($"Creating {relativePakPath}");

            foreach ((FileInfo fileInfo, RelativePath relativePath) in GetFiles(localization))
            {
                Console.WriteLine($"Adding {relativeLocalizationPath + relativePath}");
                ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(relativePath.ToString('/'));
                using FileStream fileStream = fileInfo.OpenRead();
                using Stream zipArchiveStream = zipArchiveEntry.Open();
                fileStream.CopyTo(zipArchiveStream);
            }

            Console.WriteLine();
        }
    }

    public static void PakLevels(DirectoryInfo directoryInfo)
    {
        DirectoryInfo levelsDirectory = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "Data", "Levels"));

        if (!levelsDirectory.Exists)
            return;

        foreach (DirectoryInfo level in levelsDirectory.EnumerateDirectories())
        {
            var pakPath = Path.Combine(levelsDirectory.FullName, $"{level.Name}.pak");
            var relativePakPath = new RelativePath(Path.GetRelativePath(directoryInfo.FullName, pakPath));
            var relativeLevelPath = new RelativePath(Path.GetRelativePath(directoryInfo.FullName, level.FullName));

            using FileStream zipFile = File.Open(pakPath, FileMode.Create);
            using ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create);
            Console.WriteLine($"Creating {relativePakPath}");

            foreach ((FileInfo fileInfo, RelativePath relativePath) in GetFiles(level))
            {
                Console.WriteLine($"Adding {relativeLevelPath + relativePath}");
                ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(relativePath.ToString('/'));
                using FileStream fileStream = fileInfo.OpenRead();
                using Stream zipArchiveStream = zipArchiveEntry.Open();
                fileStream.CopyTo(zipArchiveStream);
            }

            Console.WriteLine();
        }
    }
}