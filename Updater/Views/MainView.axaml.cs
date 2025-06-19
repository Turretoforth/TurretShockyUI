using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Updater.ViewModels;

namespace Updater.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        // If we are not in the designer, we can start the process
        if (!Design.IsDesignMode)
        {
            LaunchProcess();
        }
    }

    private void LaunchProcess()
    {
        // Spawn a new process to run the updater
        _ = Task.Run(async () =>
        {
            string _currentStep = "Pre-update";
            try
            {
                // Wait for the main application to close
                Process[] processes = Process.GetProcessesByName("TurretShocky");
                while (processes.Length > 0)
                {
                    processes = Process.GetProcessesByName("TurretShocky");
                    await Task.Delay(1000);
                }

                // Wait for an eventual other updater to close
                // But we only wait for a maximum of 4 seconds in case there's another program running with the same name
                Process[] updaterProcesses = Process.GetProcessesByName("Updater");
                Process currentProcess = Process.GetCurrentProcess();
                int nbAttempts = 0;
                while (updaterProcesses.Length > 0 && nbAttempts < 4)
                {
                    if (updaterProcesses.Length == 1 && updaterProcesses[0].Id == currentProcess.Id)
                    {
                        // If the only updater process is this one, we can break
                        break;
                    }
                    updaterProcesses = Process.GetProcessesByName("Updater");
                    await Task.Delay(1000);
                    nbAttempts++;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.UpdateStatusMessage = "Copying files";
                }, DispatcherPriority.MaxValue);

                // Version check + Version specific actions
                _currentStep = "Version check";
                string? version = null;
                string zipPath = Path.Combine(AppContext.BaseDirectory, "..", "TurretShocky_update.zip");
                if (File.Exists("..\\TurretShocky.exe"))
                {
                    version = FileVersionInfo.GetVersionInfo("..\\TurretShocky.exe").FileVersion ?? throw new InvalidOperationException("Could not retrieve the version of TurretShocky.exe");
                }
                else if (File.Exists("TurretShocky.exe"))
                {
                    version = FileVersionInfo.GetVersionInfo("TurretShocky.exe").FileVersion ?? throw new InvalidOperationException("Could not retrieve the version of TurretShocky.exe");
                }
                else
                {
                    throw new FileNotFoundException("TurretShocky.exe not found. Please ensure the updater is run in the correct directory.");
                }

                // Will be used later for update specific steps
                //Version currentVersion = new(version);
                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "TurretShocky.exe"))) // The Updater is in the same directory as TurretShocky.exe, that means we have to move the updater
                {
                    // Copy the updater to an Updater folder
                    _currentStep = "Copy updater";
                    string updaterPath = Path.Combine(AppContext.BaseDirectory, "Updater");
                    if (!Directory.Exists(updaterPath))
                    {
                        Directory.CreateDirectory(updaterPath);
                    }
                    // Extract the updater and all .dll files from the TurretShocky_update.zip to the Updater folder
                    zipPath = Path.Combine(AppContext.BaseDirectory, "TurretShocky_update.zip");
                    if (!File.Exists(zipPath))
                    {
                        throw new FileNotFoundException("TurretShocky_update.zip not found. Please ensure the updater is run in the correct directory.");
                    }

                    ZipArchive updaterZip = ZipFile.OpenRead("TurretShocky_update.zip");
                    foreach (ZipArchiveEntry entry in updaterZip.Entries)
                    {
                        // Skip directories
                        if (entry.FullName.EndsWith('/'))
                        {
                            continue;
                        }
                        // Only copy the updater and .dll files
                        if (entry.Name == "Updater.exe" || entry.Name.EndsWith(".dll"))
                        {
                            string destinationPath = Path.Combine(updaterPath, entry.Name);
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        (DataContext as MainViewModel)!.UpdateStatusMessage = "The updater will restart";
                    }, DispatcherPriority.MaxValue);
                    // Launch the updater from the Updater folder
                    ProcessStartInfo updaterStartInfo = new()
                    {
                        FileName = Path.Combine(updaterPath, "Updater.exe"),
                        UseShellExecute = true
                    };
                    Process.Start(updaterStartInfo);
                    // Close the current updater
                    Environment.Exit(0);
                }


                // Copy the files from the TurretShocky_update.zip
                _currentStep = "Copy";
                ZipArchive zip = ZipFile.OpenRead(zipPath);
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    // Don't overwrite the user config file if it happens to be in the zip
                    // Nor the updater since it's running
                    if (entry.Name == "prefs.json" || entry.Name == "Updater.exe")
                    {
                        continue;
                    }

                    // Skip directories, there shouldn't be any
                    if (entry.FullName.EndsWith('/'))
                    {
                        continue;
                    }

                    // Gets the full path to ensure that relative segments are removed.
                    string destinationPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", entry.FullName));
                    entry.ExtractToFile(destinationPath, true);
                }

                // Remove the old updater if it exists
                string oldUpdaterPath = Path.Combine(AppContext.BaseDirectory, "..", "Updater.exe");
                if (File.Exists(oldUpdaterPath))
                {
                    _currentStep = "Remove old updater";
                    File.Delete(oldUpdaterPath);
                }

                // Cleanup
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.UpdateStatusMessage = "Cleanup.";
                }, DispatcherPriority.MaxValue);
                _currentStep = "Cleanup";
                zip.Dispose();
                File.Delete(Path.Combine(AppContext.BaseDirectory, "..", "TurretShocky_update.zip"));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.UpdateStatusMessage = "Process finished, launching main app and closing.";
                }, DispatcherPriority.MaxValue);

                _currentStep = "Post-update";

                ProcessStartInfo startInfo = new()
                {
                    FileName = Path.Combine(AppContext.BaseDirectory, "..", "TurretShocky.exe"),
                    UseShellExecute = true
                };
                Process.Start(startInfo);

                await Task.Delay(3000); // Wait a bit to ensure the main app starts

                // Close the updater application
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Indicates that the updater failed
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.WarningMessage = $"Update failed (Step: {_currentStep}). See error below:";
                    (DataContext as MainViewModel)!.UpdateStatusMessage = ex.Message;
                }, DispatcherPriority.MaxValue);
            }
        });
    }
}
