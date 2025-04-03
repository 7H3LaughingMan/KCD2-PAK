using System.Diagnostics.Metrics;
using System.IO.Compression;
using System.IO.Hashing;
using System.Threading.Tasks;
using KCD2.Mod;
using KCD2.PAK;
using Microsoft.Win32;
using Spectre.Console;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace KCD2;

class Program
{
    private static readonly NuGet.Versioning.SemanticVersion CurrentVersion = new(2, 0, 0);

    private static readonly VelopackLocator velopackLocator = VelopackLocator.GetDefault(null);

    static async Task Main(string[] args)
    {
        VelopackApp.Build()
            .SetLocator(velopackLocator)
            .WithFirstRun((v) => { Environment.Exit(0); })
            .WithAfterInstallFastCallback((v) =>
            {
                var applicationPath = Path.Combine(AppContext.BaseDirectory, "KCD2-PAK.exe");

                {
                    var shell = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\KCD2-PAK");
                    shell.SetValue("", "PAK Mods");
                    shell.SetValue("Icon", applicationPath);

                    var command = shell.CreateSubKey("command");
                    command.SetValue("", @$"""{applicationPath}"" ""%V""");
                }

                {
                    var shell = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\shell\KCD2-PAK");
                    shell.SetValue("", "PAK Mods");
                    shell.SetValue("Icon", applicationPath);

                    var command = shell.CreateSubKey("command");
                    command.SetValue("", @$"""{applicationPath}"" ""%V""");
                }
            })
            .WithBeforeUninstallFastCallback((v) =>
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\KCD2-PAK");
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\KCD2-PAK");
            })
            .Run();

        Console.WriteLine($"KCD2 PAK v{CurrentVersion}");
        Console.WriteLine();

        if (args.Length == 1)
            await ProcessFolder(args[0], true);
        else if (args.Length > 1)
            await ProcessFolders(args);

        if (!AppDir.File(".nopause").Exists)
        {
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey();
        }

        Update();
    }

    static async Task PakFolder(ProgressTask progressTask, RelativePath pakRelativePath, FileInfo pakFile, List<(FileInfo, RelativePath)> entryFiles)
    {
        var relativePath = pakRelativePath.ParentFolder;

        var pakFolder = pakFile.Directory?.FullName ?? "";
        var pakFileName = Path.GetFileNameWithoutExtension(pakFile.Name);
        var pakExtension = pakFile.Extension;
        var partNumber = 0;

        if (!Directory.Exists(pakFolder))
            Directory.CreateDirectory(pakFolder);

        progressTask.Description = $"{relativePath}{pakFileName}{pakExtension}";
        progressTask.MaxValue = entryFiles.Count;

        var pakStream = File.Open(Path.Combine(pakFolder, $"{pakFileName}{pakExtension}"), FileMode.Create);
        var pakArchive = new PakArchive(pakStream);

        foreach ((var entryFile, var entryPath) in entryFiles)
        {
            var entry = new PakArchiveEntry(entryPath);
            entry.LastWriteTime = entryFile.LastWriteTime;

            using var entryData = new MemoryStream();
            using var entryStream = entry.Open(entryData);
            using var entryFileStream = entryFile.Open(FileMode.Open);

            await entryFileStream.CopyToAsync(entryStream);

            await entryFileStream.DisposeAsync();
            await entryStream.DisposeAsync();

            if (!pakArchive.CheckEntry(entry))
            {
                pakArchive.Dispose();

                if (partNumber == 0)
                {
                    if (File.Exists(Path.Combine(pakFolder, $"{pakFileName}-part{partNumber}{pakExtension}")))
                        File.Delete(Path.Combine(pakFolder, $"{pakFileName}-part{partNumber}{pakExtension}"));

                    File.Move(Path.Combine(pakFolder, $"{pakFileName}{pakExtension}"), Path.Combine(pakFolder, $"{pakFileName}-part{partNumber}{pakExtension}"));
                }

                partNumber++;
                pakStream = File.Open(Path.Combine(pakFolder, $"{pakFileName}-part{partNumber}{pakExtension}"), FileMode.Create);
                pakArchive = new PakArchive(pakStream);
                progressTask.Description = $"{relativePath}{pakFileName}-part{partNumber}{pakExtension}";
            }

            pakArchive.AddEntry(entry, entryData);
            progressTask.Increment(1);
        }

        pakArchive.Dispose();
        progressTask.StopTask();
    }

    static DirectoryInfo AppDir => new(velopackLocator.RootAppDir ?? AppContext.BaseDirectory);

    static async Task ProcessFolder(string path, bool recursive = false)
    {
        if (TryGetFullPath(path, out var fullPath))
        {
            if (IsDirectory(fullPath))
            {
                if (File.Exists(Path.Combine(fullPath, "mod.manifest")))
                {
                    if (File.Exists(Path.Combine(fullPath, "kcd2-pak.ignore")))
                        return;

                    var modFolder = new ModFolder(fullPath);

                    if (modFolder.Valid)
                    {
                        Console.WriteLine($"Mod ID - {modFolder.ModId}");
                        Console.WriteLine($"Input - {modFolder.InputDirectory}");
                        Console.WriteLine($"Output - {modFolder.OutputDirectory}");

                        await AnsiConsole
                            .Progress()
                            .Columns(
                                new TaskDescriptionColumn() { Alignment = Justify.Left },
                                new ProgressBarColumn(),
                                new PercentageColumn(),
                                new RemainingTimeColumn(),
                                new SpinnerColumn()
                            )
                            .StartAsync(async ctx =>
                            {
                                var tasks = new List<Task>();

                                foreach ((var pakFile, var entryFiles) in modFolder.GetTasks())
                                {
                                    var taskDescription = pakFile.Name;
                                    var entries = entryFiles.ToList();

                                    if (entries.Count == 0)
                                        continue;

                                    tasks.Add(PakFolder(ctx.AddTask(taskDescription, true, entries.Count), new RelativePath(modFolder.OutputDirectory, pakFile), pakFile, entries));
                                }

                                await Task.WhenAll(tasks);
                            });
                    }
                }
                else if (recursive)
                    await ProcessFolders(Directory.GetDirectories(fullPath));
            }
        }
    }

    static async Task ProcessFolders(string[] paths)
    {
        foreach (var path in paths)
            await ProcessFolder(path);
    }

    static bool TryGetFullPath(string path, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
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

    static bool IsDirectory(string path)
    {
        var fileAttributes = File.GetAttributes(path);
        return fileAttributes.HasFlag(FileAttributes.Directory);
    }

    static void Update()
    {
        if (velopackLocator.AppId is null)
            return;

        var manager = new UpdateManager(new GithubSource("https://github.com/7H3LaughingMan/KCD2-PAK", null, false));

        var newVersion = manager.CheckForUpdates();
        if (newVersion is null)
            return;

        manager.DownloadUpdates(newVersion);
        manager.ApplyUpdatesAndExit(null);
    }
}
