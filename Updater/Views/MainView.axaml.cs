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

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.UpdateStatusMessage = "Copying files";
                }, DispatcherPriority.MaxValue);

                // Copy the files from the TurretShocky_update.zip
                _currentStep = "Copy";
                ZipArchive zip = ZipFile.OpenRead("TurretShocky_update.zip");
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
                    string destinationPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, entry.FullName));
                    entry.ExtractToFile(destinationPath, true);
                }

                // Cleanup
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.UpdateStatusMessage = "Cleanup.";
                }, DispatcherPriority.MaxValue);
                _currentStep = "Cleanup";
                zip.Dispose();
                File.Delete("TurretShocky_update.zip");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    (DataContext as MainViewModel)!.UpdateStatusMessage = "Process finished, launching main app and closing.";
                }, DispatcherPriority.MaxValue);

                _currentStep = "Post-update";

                ProcessStartInfo startInfo = new()
                {
                    FileName = "TurretShocky.exe",
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
