using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TurretShocky.Models;
using TurretShocky.Services;
using TurretShocky.ViewModels;
using VRChatOSCLib;

namespace TurretShocky.Views
{
    public partial class MainTabView : UserControl
    {
        private readonly Lock lockCooldown = new();
        private bool inCooldown;
        private readonly ConcurrentQueue<ShockTrigger> shockQueue = new();
        public MainTabView()
        {
            InitializeComponent();
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

        private void AddLog(string message, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                (DataContext as MainWindowViewModel)?.AddLog(message, color);
            }, DispatcherPriority.MaxValue);
        }

        private void OnOscButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if((DataContext as MainWindowViewModel)!.IsOscEnabled)
                {
                    OSCService.Destroy();
                    AddLog($"Stopped listening to OSC", Colors.Orange);
                    FileWatcherService.Destroy();
                    (DataContext as MainWindowViewModel)!.IsOscEnabled = false;
                    OSCButtonLabel.Text = "Start listening to OSC";
                    OSCButton.Background = new SolidColorBrush(Colors.Green);
                    return;
                }

                if ((DataContext as MainWindowViewModel)!.Prefs.Api.ApiKey == string.Empty || (DataContext as MainWindowViewModel)!.Prefs.Api.Username == string.Empty)
                {
                    AddLog("Please configure the API key and username first", Colors.Red);
                    (DataContext as MainWindowViewModel)!.IsOscEnabled = false;
                    return;
                }
                if ((DataContext as MainWindowViewModel)!.Prefs.Shockers.Count == 0)
                {
                    AddLog("Please configure at least one shocker first", Colors.Red);
                    (DataContext as MainWindowViewModel)!.IsOscEnabled = false;
                    return;
                }

                OSCService.Initialize();
                OSCButtonLabel.Text = "Stop listening to OSC";
                OSCButton.Background = new SolidColorBrush(Colors.Red);
                (DataContext as MainWindowViewModel)!.IsOscEnabled = true;

                if ((DataContext as MainWindowViewModel)!.Prefs.App.WatchFiles)
                {
                    bool shouldQueue = (DataContext as MainWindowViewModel)!.Prefs.App.CooldownBehaviour == CooldownBehaviour.Queue;
                    foreach (var fileSetting in (DataContext as MainWindowViewModel)!.Prefs.App.FilesSettings.Where(f => f.IsEnabled))
                    {
                        FileWatcherService.AddWatcher(new FileWatcher(
                            fileSetting.DirectoryPath,
                            fileSetting.FilePattern,
                            [.. fileSetting.ShockTriggers],
                            (line, trigger) =>
                            {
                                // Trigger the shock or handle cooldown
                                Dispatcher.Invoke(() =>
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
                    FileWatcherService.StartWatching();
                }

                OSCService.StartOSC((e, m) =>
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

                AddLog($"Started listening to OSC", Colors.Green);
                SendSavedPrefs();

                Task.Run(async () =>
                {
                    Random random = new();
                    while (true)
                    {
                        // Send a random Idle face every 10 minutes
                        await Task.Delay(TimeSpan.FromMinutes(10));
                        OSCService.SendParameter("pishock/randomint", random.Next(1, 4));
                        // "Keep alive"
                        OSCService.SendParameter("pishock/codeon", true);
                    }
                });
            }
            catch (Exception ex)
            {
                (DataContext as MainWindowViewModel)!.IsOscEnabled = false;
                OSCButtonLabel.Text = "Start listening to OSC";
                OSCButton.Background = new SolidColorBrush(Colors.Green);
                AddLog($"Error: {ex.Message}", Colors.Red);
                AddLog($"Error details: {ex}", Colors.LightSalmon);
            }
        }

        private void SimulateTouch()
        {
            OSCService.SendParameter("pishock/TouchPoint_0", true);
            Thread.Sleep(1500); // Simulate a touch for long enough to trigger the shock
            OSCService.SendParameter("pishock/TouchPoint_0", false);
        }

        private void SendSavedPrefs()
        {
            OSCService.SendParameter("pishock/codeon", true);
            OSCService.SendParameter("pishock/onoroff", true);
            // Send cooldown off just in case it's stuck
            OSCService.SendParameter("pishock/cooldownbool", false);
            AddLog("Sent ON signal", Colors.LightYellow);

            OSCService.SendParameter("pishock/funtype", (int)Prefs.FunType);
            OSCService.SendParameter("pishock/minroll", Prefs.MinIntensity / 100f);
            OSCService.SendParameter("pishock/maxroll", Prefs.MaxIntensity / 100f);
            OSCService.SendParameter("pishock/cooldownset", Prefs.CooldownTime / 100f);
            OSCService.SendParameter("pishock/duration", Prefs.Duration / 10f);

            AddLog("Sent current preferences", Colors.LightYellow);
        }

        private void HandleOSCMessage(VRCMessage m)
        {
            bool hasExtraOscMessages = false;
            Dispatcher.Invoke(() =>
            {
                hasExtraOscMessages = Prefs.App.ShowExtraOscMessages;
            }, DispatcherPriority.MaxValue);

            // Changed mode
            if (m.Path.Equals("/funtype"))
            {
                Dispatcher.Invoke(() =>
                {
                    // Update the UI with the new fun type
                    Prefs.FunType = (FunType)m.GetValue<int>();
                    AddLog($"Changed type to {Prefs.FunType}", Colors.MediumPurple);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/minroll"))
            {
                Dispatcher.Invoke(() =>
                {
                    // Update the UI with the new min intensity
                    Prefs.MinIntensity = (int)Math.Ceiling(m.GetValue<float>() * 100);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/maxroll"))
            {
                Dispatcher.Invoke(() =>
                {
                    // Update the UI with the new max intensity
                    Prefs.MaxIntensity = (int)Math.Ceiling(m.GetValue<float>() * 100);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/cooldownset"))
            {
                Dispatcher.Invoke(() =>
                {
                    // Update the UI with the new cooldown time
                    Prefs.CooldownTime = (float)Math.Round(m.GetValue<float>() * 100, 1);
                }, DispatcherPriority.Render);
            }
            else if (m.Path.Equals("/duration"))
            {
                Dispatcher.Invoke(() =>
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
                ActionOnTrigger();
                // We ignore the false value from OSC, because it is sent back sometimes when we change the value
            }
            else if (m.Path.Equals("/TouchPoint_0"))
            {
                // Only received when there is a change in the value, so we can't rely on it for sending shocks
                // Can be fun for stats though
                if (m.GetValue<bool>())
                {
                    Dispatcher.Invoke(() =>
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
                    OSCService.SendParameter("pishock/codeon", true);
                }
            }
            else if (m.Path.Equals("/change"))
            {
                // The avatar is reloaded or changed, we need to send the ON signal again
                AddLog($"Avatar changed or reloaded, sending ON signal", Colors.LightYellow);
                OSCService.SendParameter("pishock/codeon", true);
                OSCService.SendParameter("pishock/onoroff", true);
                // Send cooldown off just in case it's stuck
                OSCService.SendParameter("pishock/cooldownbool", false);
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

        private void ActionOnTrigger()
        {
            FunType funType = FunType.Idle;
            int minIntensity = 0;
            int maxIntensity = 100;
            int duration = 1;
            List<Shocker> activatedDevices = [];
            int delayTrigger = 0;
            bool isRouletteMode = false;
            Dispatcher.Invoke(() =>
            {
                funType = Prefs.FunType;
                minIntensity = Prefs.MinIntensity;
                maxIntensity = Prefs.MaxIntensity;
                duration = Prefs.Duration;
                activatedDevices = [.. Prefs.Shockers.Where(s => s.IsEnabled)];
                delayTrigger = Prefs.App.DelayTrigger;
                isRouletteMode = Prefs.RouletteMode;
            }, DispatcherPriority.MaxValue);
            if (inCooldown && funType != FunType.Idle)
            {
                AddLog($"Detected trigger!", Colors.Firebrick);
                if (activatedDevices.Count == 0)
                {
                    AddLog($"No shockers enabled, ignoring trigger and resetting cooldown", Colors.Orange);
                    OSCService.SendParameter("pishock/cooldownbool", false);
                    return;
                }

                float cooldownTime = 0f;
                Dispatcher.Invoke(() =>
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
                    OSCService.SendParameter("pishock/cooldownbool", false);
                    AddLog("Cooldown finished", Colors.LightBlue);
                });

                // Then send the message to the PiShock
                // Generate a random intensity value between min and max
                Random rand = new();
                int randomIntensity = rand.Next(minIntensity, maxIntensity);
                OSCService.SendParameter("pishock/randomnum", randomIntensity / 100f);
                AddLog($"{funType} Time! Intensity: {randomIntensity}% for {duration:0.00}s", Colors.Yellow);

                if (delayTrigger > 0)
                {
                    // If delayTrigger is set, we wait before sending the shock
                    AddLog($"Delaying trigger by {delayTrigger} second(s)", Colors.LightGray);
                    Thread.Sleep(delayTrigger * 1000);
                }

                // Send the shock or vibration
                List<Shocker> selectedDevices = activatedDevices;
                if (isRouletteMode) // Roulette mode: select a random device
                {
                    selectedDevices = [.. selectedDevices.OrderBy(s => rand.Next()).Take(1)];
                    AddLog($"Roulette mode enabled, selected {selectedDevices[0].Name}", Colors.LightBlue);
                }

                DoShockersAction(funType, duration, randomIntensity, selectedDevices);

                Dispatcher.Invoke(() =>
                {
                    if (funType == FunType.Shock)
                    {
                        (DataContext as MainWindowViewModel)!.NbShocks += (uint)selectedDevices.Count;
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
                OSCService.SendParameter("pishock/cooldownbool", false);
            }
            else if (!inCooldown && funType != FunType.Idle && shockQueue.TryDequeue(out ShockTrigger trigger))
            {
                // React to any queued triggers
                AddLog($"Processing queued trigger: '{trigger.TriggerText}'. Simulating touch!", Colors.Firebrick);
                Thread.Sleep(1000); // Wait a bit before simulating the touch to be sure to trigger it
                SimulateTouch();
            }
        }

        private void DoShockersAction(FunType funType, int duration, int intensity, List<Shocker> selectedDevices)
        {
            List<ShockerAction> shockerActions = [];
            foreach (Shocker shocker in selectedDevices)
            {
                int calculatedDuration = duration;
                int calculatedIntensity = intensity;
                ShockerOverride? durationOverride = shocker.Overrides?.FirstOrDefault(o => o.OverrideType == ShockerOverrideType.Duration);
                ShockerOverride? intensityOverride = shocker.Overrides?.FirstOrDefault(o => o.OverrideType == ShockerOverrideType.Intensity);
                if (durationOverride != null
                    && ((durationOverride.OverrideMode == ShockerOverrideMode.Minimum && duration < durationOverride.OverrideValue)
                        || (durationOverride.OverrideMode == ShockerOverrideMode.Maximum && duration > durationOverride.OverrideValue)
                        || (durationOverride.OverrideMode == ShockerOverrideMode.Exactly)))
                {
                    calculatedDuration = durationOverride.OverrideValue;
                    AddLog($"Applying duration override for {shocker.Name}: {calculatedDuration}s", Colors.LightBlue);
                }

                if (intensityOverride != null
                    && ((intensityOverride.OverrideMode == ShockerOverrideMode.Minimum && intensity < intensityOverride.OverrideValue)
                        || (intensityOverride.OverrideMode == ShockerOverrideMode.Maximum && intensity > intensityOverride.OverrideValue)
                        || (intensityOverride.OverrideMode == ShockerOverrideMode.Exactly)))
                {
                    calculatedIntensity = intensityOverride.OverrideValue;
                    AddLog($"Applying intensity override for {shocker.Name}: {calculatedIntensity}", Colors.LightBlue);
                }
                shockerActions.Add(new ShockerAction
                {
                    Code = shocker.Code,
                    Type = shocker.Type,
                    FunType = funType,
                    Duration = calculatedDuration,
                    Intensity = calculatedIntensity
                });
            }

            if (shockerActions.Any(a => a.Type == ShockerType.PiShock))
            {
                AddLog($"Triggering {shockerActions.Count(s => s.Type == ShockerType.PiShock)} PiShock device(s)", Colors.Yellow);
                PiShockService.DoPiShockOperations(shockerActions.Where(a => a.Type == ShockerType.PiShock))
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

            if (shockerActions.Any(a => a.Type == ShockerType.OpenShock))
            {
                AddLog($"Triggering {shockerActions.Count(s => s.Type == ShockerType.OpenShock)} OpenShock device(s)", Colors.Yellow);
                OpenShockService.SendShockerCommand(shockerActions.Where(s => s.Type == ShockerType.OpenShock))
                .ContinueWith(r =>
                {
                    if (r.IsFaulted)
                    {
                        AddLog($"Error triggering OpenShock: {r.Exception?.Message}", Colors.Red);
                    }
                });
            }
        }
    }
}
