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
        readonly VRChatOSC osc = new();
        private bool inCooldown;
        private readonly object lockCooldown = new();
        private PiShockService? piShockService;
        private FileWatcherService? fileWatcherService;
        private readonly UpdateService updateService = new("Turretoforth", "TurretShockyUI", "TurretShocky.zip");
        private readonly ConcurrentQueue<ShockTrigger> shockQueue = new();
        public MainWindow()
        {
            InitializeComponent();
            Preferences.Initialize();

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
            osc.Dispose();
            base.OnClosing(e);
        }

        private void OnOscButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                (DataContext as MainWindowViewModel)!.IsOscButtonEnabled = false;
                if ((DataContext as MainWindowViewModel)!.Prefs.Api.ApiKey == string.Empty || (DataContext as MainWindowViewModel)!.Prefs.Api.Username == string.Empty)
                {
                    AddLog("Please configure the API key and username first", Colors.Red);
                    (DataContext as MainWindowViewModel)!.IsOscButtonEnabled = true;
                    return;
                }
                if ((DataContext as MainWindowViewModel)!.Prefs.Shockers.Count == 0)
                {
                    AddLog("Please configure at least one shocker first", Colors.Red);
                    (DataContext as MainWindowViewModel)!.IsOscButtonEnabled = true;
                    return;
                }
                if ((DataContext as MainWindowViewModel)!.Prefs.App.WatchFiles)
                {
                    fileWatcherService = new FileWatcherService();
                    bool shouldQueue = (DataContext as MainWindowViewModel)!.Prefs.App.CooldownBehaviour == CooldownBehaviour.Queue;
                    foreach (var fileSetting in (DataContext as MainWindowViewModel)!.Prefs.App.FilesSettings.Where(f => f.IsEnabled))
                    {
                        fileWatcherService.AddWatcher(new FileWatcherService.FileWatcher(
                            fileSetting.DirectoryPath,
                            fileSetting.FilePattern,
                            [.. fileSetting.ShockTriggers],
                            (line, trigger) =>
                            {
                                // Trigger the shock or handle cooldown
                                Dispatcher.UIThread.Invoke(() =>
                                {
                                    bool cooldown = false;
                                    lock (lockCooldown)
                                    {
                                        cooldown = inCooldown;
                                    }

                                    if (cooldown && !shouldQueue)
                                    {
                                        AddLog($"File watcher triggered, but in cooldown. Ignoring.", Colors.Orange);
                                    }
                                    else if (cooldown && shouldQueue)
                                    {
                                        AddLog($"File watcher triggered, but in cooldown. Queuing.", Colors.Orange);
                                        shockQueue.Enqueue(trigger);
                                    }
                                    else
                                    {
                                        AddLog($"Triggered by file watcher: '{trigger.TriggerText}'. Simulating touch!", Colors.Firebrick);
                                        SimulateTouch();
                                    }
                                }, DispatcherPriority.MaxValue);
                            },
                            (message, isError) =>
                            {
                                if (isError)
                                {
                                    AddLog($"File watcher error: {message}", Colors.Red);
                                }
                                else
                                {
                                    AddLog($"File watcher: {message}", Colors.LightBlue);
                                }
                            }
                        ));
                    }
                    fileWatcherService.StartWatching();
                }

                osc.Connect();
                osc.OnMessage += ((e, m) =>
                {
                    try
                    {
                        HandleOSCMessage(m);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error: {ex.Message}", Colors.Red);
                        AddLog($"Error details: {ex}", Colors.LightSalmon);
                    }
                });

                osc.Listen();
                AddLog($"Started listening to OSC", Colors.Green);
                SendSavedPrefs(osc);

                Task.Run(async () =>
                {
                    Random random = new();
                    while (true)
                    {
                        // Send a random Idle face every 10 minutes
                        await Task.Delay(TimeSpan.FromMinutes(10));
                        osc.SendParameter("pishock/randomint", random.Next(1, 4));
                        // "Keep alive"
                        osc.SendParameter("pishock/codeon", true);
                    }
                });
            }
            catch (Exception ex)
            {
                (DataContext as MainWindowViewModel)!.IsOscButtonEnabled = true;
                AddLog($"Error: {ex.Message}", Colors.Red);
                AddLog($"Error details: {ex}", Colors.LightSalmon);
            }
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


        readonly string[] ignoredPaths = [
            "/randomint", "/randomnum", "/onoroff", // Asset stuff
            "/AFK","/AngularY","/Grounded","/Seated","/TrackingType","/Upright","/VelocityX",
            "/VelocityY","/VelocityZ","/AvatarVersion","/GestureLeft","/GestureLeftWeight",
            "/GestureRight","/GestureRightWeight","/VRCEmote","/Viseme","/VRCFaceBlendH","/ScaleFactorInverse",
            "/EyeHeightAsPercent","/EyeHeightAsMeters", "/VelocityMagnitude", "/IsOnFriendsList", "/ScaleModified",
            "/ScaleFactor","/InStation","/Earmuffs","/MuteSelf","/IsLocal","/VRMode","/Voice"// VRChat stuff
            ];
        private void HandleOSCMessage(VRCMessage m)
        {
            bool hasExtraOscMessages = false;
            Dispatcher.UIThread.Invoke(() =>
            {
                hasExtraOscMessages = Prefs.App.ShowExtraOscMessages;
            }, DispatcherPriority.MaxValue);

            // Changed mode
            if (m.Path.Equals("/funtype"))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // Update the UI with the new fun type
                    Prefs.FunType = (FunType)m.GetValue<int>();
                    AddLog($"Changed type to {Prefs.FunType}", Colors.MediumPurple);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/minroll"))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // Update the UI with the new min intensity
                    Prefs.MinIntensity = (int)Math.Ceiling(m.GetValue<float>() * 100);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/maxroll"))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // Update the UI with the new max intensity
                    Prefs.MaxIntensity = (int)Math.Ceiling(m.GetValue<float>() * 100);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/cooldownset"))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // Update the UI with the new cooldown time
                    Prefs.CooldownTime = (float)Math.Round(m.GetValue<float>() * 100, 1);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/duration"))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    // Update the UI with the new duration
                    Prefs.Duration = Math.Clamp((int)Math.Round(m.GetValue<float>() * 10), 1, 15);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/cooldownbool"))
            {
                // Cooldown from OSC is switched to true when the spinning animation is started so it indicates we have to shock
                lock (lockCooldown)
                {
                    inCooldown = m.GetValue<bool>();
                }
                FunType funType = FunType.Idle;
                int minIntensity = 0;
                int maxIntensity = 100;
                int duration = 1;
                List<Shocker> activatedDevices = [];
                Dispatcher.UIThread.Invoke(() =>
                {
                    funType = Prefs.FunType;
                    minIntensity = Prefs.MinIntensity;
                    maxIntensity = Prefs.MaxIntensity;
                    duration = Prefs.Duration;
                    activatedDevices = [.. Prefs.Shockers.Where(s => s.IsEnabled)];
                    hasExtraOscMessages = Prefs.App.ShowExtraOscMessages;
                }, DispatcherPriority.MaxValue);
                if (inCooldown && funType != FunType.Idle)
                {
                    AddLog($"Detected trigger!", Colors.Firebrick);
                    if (activatedDevices.Count == 0)
                    {
                        AddLog($"No shockers enabled, ignoring trigger and resetting cooldown", Colors.Orange);
                        osc.SendParameter("pishock/cooldownbool", false);
                        return;
                    }

                    float cooldownTime = 0f;
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        cooldownTime = Prefs.CooldownTime;
                        (DataContext as MainWindowViewModel)!.TimesTriggered++;
                    }, DispatcherPriority.MaxValue);

                    // Start the cooldown timer first
                    AddLog($"Cooldown started for {cooldownTime:0.00}s", Colors.LightGreen);
                    Task.Run(() =>
                    {
                        lock (lockCooldown)
                        {
                            inCooldown = true;
                        }
                        Task.Delay((int)Math.Round(cooldownTime * 1000)).Wait();
                        lock (lockCooldown)
                        {
                            inCooldown = false;
                        }
                        osc.SendParameter("pishock/cooldownbool", false);
                        AddLog("Cooldown finished", Colors.LightBlue);
                    });

                    // Then send the message to the PiShock
                    // Generate a random intensity value between min and max
                    Random rand = new();
                    int randomIntensity = rand.Next(minIntensity, maxIntensity);
                    osc.SendParameter("pishock/randomnum", randomIntensity / 100f);
                    AddLog($"{funType} Time! Intensity: {randomIntensity}% for {duration:0.00}s", Colors.Yellow);

                    // Send the shock or vibration
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        piShockService ??= new PiShockService(Prefs.Api.ApiKey, Prefs.Api.Username);
                    });
                    if (activatedDevices.Any(s => s.Type == ShockerType.PiShock))
                    {
                        AddLog($"Triggering {activatedDevices.Count(s => s.Type == ShockerType.PiShock)} PiShock device(s)", Colors.Yellow);
                        piShockService!.DoPiShockOperations(funType, duration, randomIntensity, [.. activatedDevices.Where(s => s.Type == ShockerType.PiShock).Select(s => s.Code)])
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
                    if (activatedDevices.Any(s => s.Type == ShockerType.OpenShock))
                    {
                        AddLog($"Triggering {activatedDevices.Count(s => s.Type == ShockerType.OpenShock)} OpenShock device(s)", Colors.Yellow);
                        OpenShockService.SendShockerCommand([.. activatedDevices.Where(s => s.Type == ShockerType.OpenShock).Select(s => s.Code)],
                            funType, randomIntensity, duration * 1000) // For OpenShock, duration is in milliseconds
                        .ContinueWith(r =>
                        {
                            if (r.IsFaulted)
                            {
                                AddLog($"Error triggering OpenShock: {r.Exception?.Message}", Colors.Red);
                            }
                        });
                    }

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (funType == FunType.Shock)
                        {
                            (DataContext as MainWindowViewModel)!.NbShocks += (uint)activatedDevices.Count;
                        }
                        if ((DataContext as MainWindowViewModel)!.MaxIntensity < randomIntensity)
                        {
                            (DataContext as MainWindowViewModel)!.MaxIntensity = (uint)randomIntensity;
                        }
                    }, DispatcherPriority.Render);
                }
                else if (inCooldown && funType == FunType.Idle)
                {
                    // Should not happen, but just in case, we reset the cooldown
                    AddLog($"Trigger ignored, currently in Idle mode. Resetting cooldown.", Colors.Yellow);
                    lock (lockCooldown)
                    {
                        inCooldown = false;
                    }
                    osc.SendParameter("pishock/cooldownbool", false);
                }
                else if (!inCooldown && funType != FunType.Idle && shockQueue.TryDequeue(out ShockTrigger trigger))
                {
                    // React to any queued triggers
                    AddLog($"Processing queued trigger: '{trigger.TriggerText}'. Simulating touch!", Colors.Firebrick);
                    Thread.Sleep(1000); // Wait a bit before simulating the touch to be sure to trigger it
                    SimulateTouch();
                }
                // We ignore the false value from OSC, because it is sent back sometimes when we change the value
            }
            else if (m.Path.Equals("/TouchPoint_0"))
            {
                // Only received when there is a change in the value, so we can't rely on it for sending shocks
                // Can be fun for stats though
                if (m.GetValue<bool>())
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        (DataContext as MainWindowViewModel)!.NbTouches++;
                    }, DispatcherPriority.Render);
                }
            }
            else if (m.Path.Equals("/codeon"))
            {
                if (!m.GetValue<bool>())
                {
                    AddLog($"Received code OFF signal, reminding avatar we're alive!", Colors.LightYellow);
                    osc.SendParameter("pishock/codeon", true);
                }
            }
            else if (m.Path.Equals("/change"))
            {
                // The avatar is reloaded or changed, we need to send the ON signal again
                AddLog($"Avatar changed or reloaded, sending ON signal", Colors.LightYellow);
                osc.SendParameter("pishock/codeon", true);
                osc.SendParameter("pishock/onoroff", true);
                // Send cooldown off just in case it's stuck
                osc.SendParameter("pishock/cooldownbool", false);
            }
            else if (ignoredPaths.Any(p => m.Path.Equals(p)))
            {
                // Ignore the message
            }
            else
            {
                if (hasExtraOscMessages)
                {
                    AddLog($"Received: Path={m.Path} Type={m.Type} Address={m.Address} IsParameter={m.IsParameter} Value={m.GetValue()}", Colors.LightGreen);
                }
            }
        }

        private void SimulateTouch()
        {
            osc.SendParameter("pishock/TouchPoint_0", true);
            Thread.Sleep(1500); // Simulate a touch for long enough to trigger the shock
            osc.SendParameter("pishock/TouchPoint_0", false);
        }

        private void SendSavedPrefs(VRChatOSC osc)
        {
            osc.SendParameter("pishock/codeon", true);
            osc.SendParameter("pishock/onoroff", true);
            // Send cooldown off just in case it's stuck
            osc.SendParameter("pishock/cooldownbool", false);
            AddLog("Sent ON signal", Colors.LightYellow);

            osc.SendParameter("pishock/funtype", (int)Prefs.FunType);
            osc.SendParameter("pishock/minroll", Prefs.MinIntensity / 100f);
            osc.SendParameter("pishock/maxroll", Prefs.MaxIntensity / 100f);
            osc.SendParameter("pishock/cooldownset", Prefs.CooldownTime / 100f);
            osc.SendParameter("pishock/duration", Prefs.Duration / 10f);

            AddLog("Sent current preferences", Colors.LightYellow);
        }

        private void OnAppSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            // Open the App settings window
            var appSettingsWindow = new AppSettingsWindow
            {
                DataContext = (DataContext as MainWindowViewModel)!.Prefs.App
            };
            appSettingsWindow.ShowDialog<AppSettingsWindowResult>(this)
                .ContinueWith(t =>
                {
                    // We should always have a result, but just in case
                    if (t.Result != null)
                    {
                        // Save the preferences
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            AppSettings appSettings = (DataContext as MainWindowViewModel)!.Prefs.App;

                            appSettings.WatchFiles = t.Result.WatchFiles;
                            appSettings.CooldownBehaviour = t.Result.CooldownBehaviour;
                            appSettings.FilesSettings = [.. t.Result.FilesSettings];

                            (DataContext as MainWindowViewModel)!.Prefs.App = appSettings;
                        });
                    }
                }
            );
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

        private void InitiateUpdateClickBtn(object? sender, RoutedEventArgs e)
        {
            if ((DataContext as MainWindowViewModel)!.HasUpdateAvailable)
            {
                string updateToDownload = (DataContext as MainWindowViewModel)!.UpdateVersion ?? "latest";
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
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            if (entry.Name == "Updater.exe")
                            {
                                foundUpdater = true;
                                string targetPath = System.IO.Path.Combine(AppContext.BaseDirectory, entry.Name);
                                entry.ExtractToFile(targetPath, true);
                                AddLog($"Extracted updater! The application will update in a few seconds.", Colors.Green);
                                await Task.Delay(3000); // Wait 3 seconds before applying the update
                                // Start the updater
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "Updater.exe",
                                    UseShellExecute = true
                                });

                                // Close the main application
                                Environment.Exit(0);
                            }
                        }
                        if (!foundUpdater)
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