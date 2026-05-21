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
        private PiShockService? piShockService;
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
            // Initialize OpenShockService
            string openshockApiToken = (DataContext as MainWindowViewModel)!.Prefs.Api.OpenShockApiToken;
            string openshockBaseApi = (DataContext as MainWindowViewModel)!.Prefs.Api.OpenShockBaseApi;
            OpenShockService.Initialize(openshockBaseApi, openshockApiToken);

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


        private void OnConfigureApiBtnClick(object? sender, RoutedEventArgs e)
        {
            // Open the Api configuration window
            var apiConfigWindow = new ApiConfigWindow
            {
                DataContext = (DataContext as MainWindowViewModel)!.Prefs.Api
            };
            apiConfigWindow.ShowDialog<ApiConfigWindowResult>(this)
                .ContinueWith(t =>
                {
                    // Check if the user clicked the save button
                    if (t.Result != null && t.Result.ShouldSave)
                    {
                        // Save the preferences
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            (DataContext as MainWindowViewModel)!.Prefs.Api = t.Result.ApiPrefs ?? new();
                            // Reinitialize the OpenShockService with the new (potential) API settings
                            OpenShockService.Initialize(
                                (DataContext as MainWindowViewModel)!.Prefs.Api.OpenShockBaseApi,
                                (DataContext as MainWindowViewModel)!.Prefs.Api.OpenShockApiToken
                            );
                        });
                    }
                }
            );
        }

        private void ShockerEnableClick(object? sender, RoutedEventArgs e)
        {
            // Get the name of the shocker to enable/disable
            var shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
            Shocker? selectedShocker = shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as CheckBox)!.Name);
            if (selectedShocker != null)
            {
                // Toggle the enabled state of the shocker
                selectedShocker.IsEnabled = (sender as CheckBox)!.IsChecked ?? false;
                // Update the DataContext
                (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
            }
        }

        private void ShockerSelectClick(object? sender, RoutedEventArgs e)
        {
            // Get the name of the shocker to enable/disable
            var shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
            Shocker? selectedShocker = shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as CheckBox)!.Name);
            if (selectedShocker != null)
            {
                // Toggle the selected state of the shocker
                selectedShocker.IsSelected = (sender as CheckBox)!.IsChecked ?? false;
                // Update the DataContext
                (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
            }
        }

        private void DeleteShockerBtn(object? sender, RoutedEventArgs e)
        {
            // Get the name of the shocker to delete
            var shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
            Shocker? selectedShocker = shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as Button)!.Name);
            if (selectedShocker != null)
            {
                // Remove the shocker from the list
                shockers.Remove(selectedShocker);
                // Update the DataContext
                (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
            }
        }

        private void OnEditOrCreateShockerBtnClick(object? sender, RoutedEventArgs e)
        {
            // Check if the button clicked is the "New" button
            bool isNew = (sender as Button)!.Name == "NewShockerBtn";
            Shocker? selectedShocker = null;
            if (!isNew)
            {
                selectedShocker = (DataContext as MainWindowViewModel)!.Prefs.Shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as Button)!.Name);
            }

            // Open the Shocker configuration window
            var shockerConfigWindow = new ShockerConfigWindow(isNew, selectedShocker)
            {
                DataContext = (DataContext as MainWindowViewModel)!.Prefs.Shockers
            };
            shockerConfigWindow.ShowDialog<ShockerConfigWindowResult>(this)
                .ContinueWith(t =>
                {
                    // Check if there is something to save
                    if (t.Result != null && t.Result.ShouldSave && t.Result.Shocker != null)
                    {
                        // Save the shocker
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            System.Collections.ObjectModel.ObservableCollection<Shocker> shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
                            if (t.Result.IsNew)
                            {
                                shockers.Add(t.Result.Shocker);
                            }
                            else if (selectedShocker != null)
                            {
                                shockers.Remove(selectedShocker);
                                shockers.Add(t.Result.Shocker);
                            }
                            (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
                        });
                    }
                }
            );
        }
        private void OnTestShockerBtnClick(object? sender, RoutedEventArgs e)
        {
            // Get the shocker to test
            Shocker? selectedShocker = (DataContext as MainWindowViewModel)!.Prefs.Shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as Button)!.Name);
            if (selectedShocker == null)
            {
                AddLog("Couldn't find shocker to test? (Report this)", Colors.Red);
                return;
            }
            AddLog($"Triggering a test [1 second - 80%] vibration on {selectedShocker.Name}", Colors.Yellow);

            try
            {
                if (selectedShocker.Type == ShockerType.PiShock)
                {
                    piShockService ??= new PiShockService(Prefs.Api.ApiKey, Prefs.Api.Username);
                    piShockService.DoPiShockOperations(FunType.Vibration, 1, 80, [selectedShocker.Code])
                        .ContinueWith(r =>
                        {
                            foreach (var shocker in r.Result)
                            {
                                if (!shocker.Value.Success)
                                {
                                    AddLog($"Error triggering PiShock {shocker.Key}: {shocker.Value.Message}", Colors.Red);
                                }
                            }
                        });
                }
                else if (selectedShocker.Type == ShockerType.OpenShock)
                {
                    OpenShockService.SendShockerCommand([selectedShocker.Code], FunType.Vibration, 80, 1000)
                        .ContinueWith(r =>
                        {
                            if (r.IsFaulted)
                            {
                                AddLog($"Error triggering OpenShock: {r.Exception?.Message}", Colors.Red);
                            }
                        });
                }
                else
                {
                    AddLog($"Unknown shocker type: {selectedShocker.Type}", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error testing shocker: {ex.Message}", Colors.Red);
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