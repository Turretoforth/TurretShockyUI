using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TurretShocky.Models;

namespace TurretShocky.Services
{
    public static class FileWatcherService
    {
        private static readonly List<FileWatcher> _fileWatchers = [];
        private static CancellationTokenSource? _cancellationTokenSource = null;

        public static void Destroy()
        {
            StopWatching();
            _fileWatchers.Clear();
        }

        public static void AddWatcher(FileWatcher fileWatcher)
        {
            ArgumentNullException.ThrowIfNull(fileWatcher);
            _fileWatchers.Add(fileWatcher);
        }

        public static void StartWatching()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            foreach (var watcher in _fileWatchers)
            {
                watcher.Watch(_cancellationTokenSource.Token);
            }
        }

        public static void StopWatching()
        {
            _cancellationTokenSource?.Cancel();
            foreach (FileWatcher watcher in _fileWatchers)
            {
                watcher.Stop();
            }
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

    }
    public class FileWatcher(string directoryPath, string filePattern, List<ShockTrigger> shockTriggers, Action<string, ShockTrigger> onMatchedLine, Action<string, bool> onLog)
    {
        public string DirectoryPath { get; } = directoryPath;
        public string FilePattern { get; } = filePattern;
        public List<ShockTrigger> ShockTriggers { get; } = shockTriggers;
        public Action<string, ShockTrigger> OnMatchedLine { get; } = onMatchedLine;
        public Action<string, bool> OnLog { get; } = onLog;

        private readonly Lock _lock = new();
        private readonly List<FileWatched> _currentlyWatchedFiles = [];
        private readonly List<Task> watchTasks = [];
        private Task? watcherTask = null;

        internal void Watch(CancellationToken cancellationToken)
        {
            // Launch the thread to watch files
            watcherTask = Task.Run(() =>
            {
                // Keep the thread alive
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Get the files in the directory that match the pattern
                    // Will grab any new files that match the pattern
                    List<FileWatched> filesInDir = FindFiles(DirectoryPath, FilePattern);
                    if (filesInDir.Count == 0)
                    {
                        OnLog($"No files found in {DirectoryPath} matching '{FilePattern}'", false);
                        Thread.Sleep(10000); // No files found, wait before checking again
                        continue;
                    }

                    foreach (FileWatched file in filesInDir)
                    {
                        // Check if the file is already being watched
                        lock (_lock)
                        {
                            if (!_currentlyWatchedFiles.Any(f => f.FilePath == file.FilePath))
                            {
                                _currentlyWatchedFiles.Add(file);
                            }
                            else
                                continue;
                        }
                        StartWatchFile(file, cancellationToken);
                    }
                    // We only need to check for new files once in a while
                    Thread.Sleep(10000);
                }
                OnLog($"Stopped watching directory: {DirectoryPath}", false);
            }, cancellationToken);
        }

        private void StartWatchFile(FileWatched file, CancellationToken cancellationToken)
        {
            // Start watching the file
            Task watchTask = Task.Run(() =>
            {
                try
                {
                    OnLog($"Started watching file: {file.FilePath}", false);
                    using FileStream fileStream = new(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using StreamReader streamReader = new(fileStream);

                    // Move the stream reader to the end of the file (to avoid reading old lines)
                    streamReader.BaseStream.Seek(0, SeekOrigin.End);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        string? line = streamReader.ReadLine();
                        if (line == null)
                        {
                            // No new line, wait for a bit
                            Thread.Sleep(1000);
                            continue;
                        }

                        line = line.Trim();

                        // Check if the line matches any of the triggers
                        foreach (var trigger in ShockTriggers)
                        {
                            bool isMatch = false;
                            if (trigger.TriggerMode == TriggerMode.Contains)
                            {
                                isMatch = line.Contains(trigger.TriggerText, StringComparison.InvariantCulture);
                            }
                            else if (trigger.TriggerMode == TriggerMode.StartsWith)
                            {
                                isMatch = line.StartsWith(trigger.TriggerText, StringComparison.InvariantCulture);
                            }
                            else if (trigger.TriggerMode == TriggerMode.EndsWith)
                            {
                                isMatch = line.EndsWith(trigger.TriggerText, StringComparison.InvariantCulture);
                            }
                            else if (trigger.TriggerMode == TriggerMode.Regex)
                            {
                                // Regex matching
                                try
                                {
                                    isMatch = System.Text.RegularExpressions.Regex.IsMatch(line, trigger.TriggerText);
                                }
                                catch (Exception ex)
                                {
                                    OnLog($"Error in regex for trigger ('{trigger.TriggerText}'): {ex.Message}", true);
                                    continue;
                                }
                            }
                            else
                            {
                                OnLog($"Unknown trigger mode: {trigger.TriggerMode}", true);
                                continue;
                            }

                            if (isMatch)
                            {
                                OnMatchedLine(line, trigger);
                            }
                        }
                    }
                    OnLog($"Stopped watching file: {file.FilePath}", false);
                }
                catch (Exception ex)
                {
                    // Remove the file from the list if it fails to read
                    lock (_lock)
                    {
                        _currentlyWatchedFiles.RemoveAll(f => f.FilePath == file.FilePath);
                    }
                    OnLog($"Error watching file: {file.FilePath} - {ex.Message}", true);
                }
            }, cancellationToken);
            watchTasks.Add(watchTask);
        }

        private static List<FileWatched> FindFiles(string directoryPath, string filePattern)
        {
            List<FileWatched> files = [];
            DirectoryInfo dir = new(directoryPath);
            foreach (FileInfo file in dir.GetFiles(filePattern))
            {
                files.Add(new(file.FullName));
            }
            return files;
        }

        internal void Stop()
        {
            watchTasks.Clear();
            watcherTask = null;
        }

        internal class FileWatched(string filePath)
        {
            public string FilePath { get; } = filePath;
        }
    }
}
