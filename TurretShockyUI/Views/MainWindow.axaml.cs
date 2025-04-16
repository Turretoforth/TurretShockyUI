using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurretShockyUI.Models;
using TurretShockyUI.ViewModels;
using VRChatOSCLib;

namespace TurretShockyUI.Views
{
    public partial class MainWindow : Window
    {
        readonly VRChatOSC osc = new();
        private bool inCooldown;
        private readonly object lockObj = new();
        private PiShockService piShockService = null;
        public MainWindow()
        {
            InitializeComponent();
            Preferences.Initialize();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            osc.Dispose();
            base.OnClosing(e);
        }

        private void Button_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                (DataContext as MainWindowViewModel)!.IsOscButtonEnabled = false;
                osc.Connect();
                osc.OnMessage = ((e, m) =>
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

        VrcPrefs Prefs
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
                lock (lockObj)
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
                        lock (lockObj)
                        {
                            inCooldown = true;
                        }
                        Task.Delay((int)Math.Round(cooldownTime * 1000)).Wait();
                        lock (lockObj)
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
                    piShockService.DoPiShockOperations(funType, duration, randomIntensity, [.. activatedDevices.Select(s => s.Code)]).ContinueWith(r =>
                    {
                        foreach (var shocker in r.Result)
                        {
                            if (!shocker.Value.Success)
                            {
                                AddLog($"Error triggering shocker {shocker.Key}: {shocker.Value.Message}", Colors.Red);
                            }
                        }
                    });
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
                    lock (lockObj)
                    {
                        inCooldown = false;
                    }
                    osc.SendParameter("pishock/cooldownbool", false);
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
                AddLog($"Received: Path={m.Path} Type={m.Type} Address={m.Address} IsParameter={m.IsParameter} Value={m.GetValue()}", Colors.LightGreen);
            }
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
    }
}