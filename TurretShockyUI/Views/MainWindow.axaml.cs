using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TurretShocky.Models;
using TurretShocky.Services;
using TurretShocky.ViewModels;
using VRChatOSCLib;

namespace TurretShocky.Views
{
    public partial class MainWindow : Window
    {
        private readonly UpdateService updateService = new("Turretoforth", "TurretShockyUI", "TurretShocky.zip");
        public MainWindow()
        {
            InitializeComponent();
            Preferences.Initialize();
            OSCService.Initialize();

            if (!Design.IsDesignMode)
            {
                StartUpdateCheckLoop();
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            // Initialize shock services
            OpenShockService.Initialize(Prefs.Api.OpenShockBaseApi, Prefs.Api.OpenShockApiToken);
            PiShockService.Initialize(Prefs.Api.ApiKey, Prefs.Api.Username);

            base.OnOpened(e);
        }

        private void StartUpdateCheckLoop()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (updateService != null && await updateService.CheckForUpdates())
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                (DataContext as MainWindowViewModel)!.HasUpdateAvailable = true;
                                (DataContext as MainWindowViewModel)!.UpdateVersion = updateService.LatestStableVersion;
                            }, DispatcherPriority.MaxValue);
                        }
                        else
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                (DataContext as MainWindowViewModel)!.HasUpdateAvailable = false;
                            }, DispatcherPriority.MaxValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error checking for update: {ex.Message}", Colors.Red);
                    }
                    await Task.Delay(TimeSpan.FromMinutes(30)); // Check every 30 minutes
                }
            });
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            OSCService.Destroy();
            base.OnClosing(e);
        }


        private void AddLog(string message, Color color)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                (DataContext as MainWindowViewModel)?.AddLog(message, color);
            }, DispatcherPriority.MaxValue);
        }

        ShockyPrefs Prefs
        {
            get
            {
                return (DataContext as MainWindowViewModel)!.Prefs;
            }
        }

        private void OnUpdateClickBtn(object? sender, RoutedEventArgs e)
        {
            if ((DataContext as MainWindowViewModel)!.HasUpdateAvailable)
            {
                // Open the update changelog window
                UpdateChangelogWindow updateChangelogWindow = new(updateService);
                updateChangelogWindow.ShowDialog<UpdateChangelogWindowResult>(this)
                    .ContinueWith(t =>
                    {
                        if (t.Result != null && t.Result.ShouldUpdate)
                        {
                            InitiateUpdateClickBtn();
                        }
                        else
                        {
                            AddLog("Update cancelled", Colors.LightGray);
                        }
                    });
            }
        }

        private void InitiateUpdateClickBtn()
        {
            bool hasUpdate = false;
            string updateToDownload = string.Empty;
            Dispatcher.UIThread.Invoke(() =>
            {
                hasUpdate = (DataContext as MainWindowViewModel)!.HasUpdateAvailable;
                updateToDownload = (DataContext as MainWindowViewModel)!.UpdateVersion ?? "latest";
            }, DispatcherPriority.MaxValue);

            if (hasUpdate)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        AddLog("Downloading update " + updateToDownload + "...", Colors.Green);
                        await updateService.DownloadUpdateToCurrentFolder("TurretShocky_update.zip");
                        AddLog("Downloaded update! Extracting updater...", Colors.Green);
                        // Extract the updater
                        using ZipArchive zip = ZipFile.OpenRead("TurretShocky_update.zip");
                        bool foundUpdater = false;
                        bool hasUpdaterFolder = System.IO.Directory.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "Updater"));
                        string updaterPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Updater.exe");
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            if (hasUpdaterFolder && entry.Name.EndsWith(".dll"))
                            {
                                // The updater needs the .dll files that could not be in the Updater folder for some reason
                                entry.ExtractToFile(System.IO.Path.Combine(AppContext.BaseDirectory, "Updater", entry.Name), true);
                            }
                            else if (entry.Name == "Updater.exe")
                            {
                                foundUpdater = true;

                                // If the Updater folder exists, extract to it, otherwise extract to the current directory
                                // (The updater will create the folder if it doesn't exist)
                                if (hasUpdaterFolder)
                                {
                                    updaterPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Updater", entry.Name);
                                }

                                entry.ExtractToFile(updaterPath, true);
                            }
                        }
                        if (foundUpdater)
                        {
                            AddLog($"Extracted updater! The application will update in a few seconds.", Colors.Green);
                            await Task.Delay(3000); // Wait 3 seconds before applying the update

                            // Start the updater
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = updaterPath,
                                UseShellExecute = true
                            });

                            // Close the main application
                            Environment.Exit(0);
                        }
                        else
                        {
                            AddLog("Updater not found in the downloaded archive. Please install it manually or check the Github for more information.", Colors.Yellow);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error downloading update: {ex.Message}", Colors.Red);
                    }
                });
            }
        }
    }
}