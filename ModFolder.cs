using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;

namespace KCD2_PAK;

public class ModFolder
{
    private DirectoryInfo _inputDirectory;

    private DirectoryInfo _outputDirectory;

    private string? _modId;

    private ILogger _logger = new ConsoleLogger();

    public ModFolder(DirectoryInfo directoryInfo)
    {
        _inputDirectory = directoryInfo;

        var lnkFile = directoryInfo.File("kcd2-pak.lnk");

        if (lnkFile.Exists)
        {
            var lnk = Lnk.Lnk.LoadFile(lnkFile.FullName);
            _outputDirectory = new DirectoryInfo(lnk.LocalPath);
        }
        else
        {
            _outputDirectory = directoryInfo;
        }

        var logFile = directoryInfo.File("kcd2-pak.log");

        if (logFile.Exists)
            _logger = new FileLogger(logFile.FullName);

        _modId = GetModId(directoryInfo);
    }

    public ModFolder(string path) : this(new DirectoryInfo(path)) { }

    public bool Valid => _modId is not null;

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

    private static IEnumerable<(FileInfo, RelativePath)> GetFiles(DirectoryInfo directoryInfo, Predicate<RelativePath>? predicate = null)
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

    public void PakData()
    {
        var inputFolder = _inputDirectory.Subfolder("Data");
        var outputFolder = _outputDirectory.Subfolder("Data");

        if (!inputFolder.Exists)
            return;

        if (!outputFolder.Exists)
            outputFolder.Create();

        var outputPak = outputFolder.File($"{_modId}.pak");

        var relativeOutputPak = new RelativePath(_inputDirectory, outputPak);
        var relativeInputFolder = new RelativePath(_inputDirectory, inputFolder);

        using var pakFile = File.Open(outputPak.FullName, FileMode.Create);
        using var pakArchive = new ZipArchive(pakFile, ZipArchiveMode.Create);
        _logger.LogInformation($"Creating {relativeOutputPak}");

        foreach ((FileInfo fileInfo, RelativePath relativePath) in GetFiles(inputFolder, x => !x.IsWithinFolder(Levels)))
        {
            _logger.LogInformation($"Adding {relativeInputFolder + relativePath}");
            pakArchive.CreateEntryFromFile(fileInfo, relativePath);
        }

        _logger.LogInformation("");
    }

    public void PakLocalization()
    {
        var inputFolder = _inputDirectory.Subfolder("Localization");
        var outputFolder = _outputDirectory.Subfolder("Localization");

        if (!inputFolder.Exists)
            return;

        if (!outputFolder.Exists)
            outputFolder.Create();

        foreach (var inputLocalization in inputFolder.EnumerateDirectories())
        {
            var outputPak = outputFolder.File($"{inputLocalization.Name}.pak");

            var relativeOutputPak = new RelativePath(_inputDirectory, outputPak);
            var relativeInputLocalization = new RelativePath(_inputDirectory, inputLocalization);

            using var pakFile = File.Open(outputPak.FullName, FileMode.Create);
            using var pakArchive = new ZipArchive(pakFile, ZipArchiveMode.Create);
            _logger.LogInformation($"Creating {relativeOutputPak}");

            foreach ((FileInfo fileInfo, RelativePath relativePath) in GetFiles(inputLocalization))
            {
                _logger.LogInformation($"Adding {relativeInputLocalization + relativePath}");
                pakArchive.CreateEntryFromFile(fileInfo.FullName, relativePath.ToString('/'));
            }

            _logger.LogInformation("");
        }
    }

    public void PakLevels()
    {
        var inputFolder = _inputDirectory.Subfolder("Data", "Levels");
        var outputFolder = _outputDirectory.Subfolder("Data", "Levels");

        if (!inputFolder.Exists)
            return;

        if (!outputFolder.Exists)
            outputFolder.Create();

        foreach (var inputLevel in inputFolder.EnumerateDirectories())
        {
            var outputLevel = outputFolder.Subfolder(inputLevel.Name);

            if (!outputLevel.Exists)
                outputLevel.Create();

            var outputPak = outputLevel.File($"{_modId}.pak");

            var relativeOutputPak = new RelativePath(_inputDirectory, outputPak);
            var relativeInputLevel = new RelativePath(_inputDirectory, inputLevel);

            using var pakFile = File.Open(outputPak.FullName, FileMode.Create);
            using var pakArchive = new ZipArchive(pakFile, ZipArchiveMode.Create);
            _logger.LogInformation($"Creating {relativeOutputPak}");

            foreach ((FileInfo fileInfo, RelativePath relativePath) in GetFiles(inputLevel))
            {
                _logger.LogInformation($"Adding {relativeInputLevel + relativePath}");
                pakArchive.CreateEntryFromFile(fileInfo.FullName, relativePath.ToString('/'));
            }

            _logger.LogInformation("");
        }
    }
}
