using System.Diagnostics;
using Microsoft.Win32;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace KCD2_PAK;

class Program
{
    private static readonly NuGet.Versioning.SemanticVersion CurrentVersion = new(2, 0, 0);

    private static readonly VelopackLocator velopackLocator = VelopackLocator.GetDefault(null);

    static void Main(string[] args)
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
            ProcessFolder(args[0], true);
        else if (args.Length > 1)
            ProcessFolders(args);

        if (!AppDir.File(".nopause").Exists)
        {
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey();
        }

        Update();
    }

    static DirectoryInfo AppDir => new(velopackLocator.RootAppDir ?? AppContext.BaseDirectory);

    static void ProcessFolder(string path, bool recursive = false)
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
                        modFolder.PakData();
                        modFolder.PakLevels();
                        modFolder.PakLocalization();
                    }
                }
                else if (recursive)
                    ProcessFolders(Directory.GetDirectories(fullPath));
            }
        }
    }

    static void ProcessFolders(string[] paths)
    {
        foreach (var path in paths)
            ProcessFolder(path);
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
