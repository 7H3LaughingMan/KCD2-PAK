using System.Xml.Linq;
using System.Xml.XPath;

namespace KCD2.Mod;

public class ModFolder
{
    public ModFolder(DirectoryInfo directoryInfo)
    {
        InputDirectory = directoryInfo;

        var lnkFile = directoryInfo.File("kcd2-pak.lnk");

        if (lnkFile.Exists)
        {
            var lnk = Lnk.Lnk.LoadFile(lnkFile.FullName);
            OutputDirectory = new DirectoryInfo(lnk.LocalPath);
        }
        else
        {
            OutputDirectory = directoryInfo;
        }

        ModId = GetModId(directoryInfo);
    }

    public ModFolder(string path) : this(new DirectoryInfo(path)) { }

    public DirectoryInfo InputDirectory { get; private set; }

    public DirectoryInfo OutputDirectory { get; private set; }

    public string? ModId { get; private set; }

    public bool Valid => ModId is not null;

    private static string? GetModId(DirectoryInfo directoryInfo)
    {
        if (!directoryInfo.Exists)
            return null;

        var manifest = directoryInfo.File("mod.manifest");

        if (!manifest.Exists)
            return null;

        try
        {
            var document = XDocument.Load(manifest.FullName);
            return document.XPathSelectElement("/kcd_mod/info/modid")?.Value;
        }
        catch (Exception) { }

        return null;
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

    public IList<(FileInfo, IEnumerable<(FileInfo, RelativePath)>)> GetTasks()
    {
        var tasks = new List<(FileInfo, IEnumerable<(FileInfo, RelativePath)>)>();

        var dataFolder = InputDirectory.Subfolder("Data");

        if (dataFolder.Exists)
            tasks.Add((OutputDirectory.Subfolder("Data").File($"{ModId}.pak"), GetFiles(dataFolder, x => !x.IsWithinFolder(Levels))));

        var localizations = InputDirectory.Subfolder("Localization");

        if (localizations.Exists)
            foreach (var localization in localizations.EnumerateDirectories())
                tasks.Add((OutputDirectory.Subfolder("Localization").File($"{localization.Name}.pak"), GetFiles(localization)));

        var levels = InputDirectory.Subfolder("Data", "Levels");

        if (levels.Exists)
            foreach (var level in levels.EnumerateDirectories())
                tasks.Add((OutputDirectory.Subfolder("Data", "Levels", level.Name).File($"{ModId}.pak"), GetFiles(level)));

        return tasks;
    }
}
