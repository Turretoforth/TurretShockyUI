using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TurretShocky.Models;

namespace TurretShocky.Services
{
    public class FileWatcherService
    {
        private readonly List<FileWatcher> _fileWatchers = [];
        public FileWatcherService() { }
        public void AddWatcher(FileWatcher fileWatcher)
        {
            ArgumentNullException.ThrowIfNull(fileWatcher);
            _fileWatchers.Add(fileWatcher);
        }

        public void StartWatching()
        {
            foreach (var watcher in _fileWatchers)
            {
                watcher.Watch();
            }
        }

        public class FileWatcher(string directoryPath, string filePattern, List<ShockTrigger> shockTriggers, Action<string, ShockTrigger> onMatchedLine, Action<string, bool> onLog)
        {
            public string DirectoryPath { get; } = directoryPath;
            public string FilePattern { get; } = filePattern;
            public List<ShockTrigger> ShockTriggers { get; } = shockTriggers;
            public Action<string, ShockTrigger> OnMatchedLine { get; } = onMatchedLine;
            public Action<string, bool> OnLog { get; } = onLog;

            private readonly object _lock = new();
            private readonly List<FileWatched> _currentlyWatchedFiles = [];

            internal void Watch()
            {
                // Launch the thread to watch files
                Task.Run(() =>
                {
                    // Keep the thread alive
                    while (true)
                    {
                        // Get the files in the directory that match the pattern
                        // Will grab any new files that match the pattern
                        List<FileWatched> filesInDir = FindFiles(DirectoryPath, FilePattern);
                        if (filesInDir.Count == 0)
                        {
                            OnLog($"No files found in {DirectoryPath} matching '{FilePattern}'", false);
                            Thread.Sleep(30000); // No files found, wait before checking again
                            continue;
                        }

                        foreach (var file in filesInDir)
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

                            // Start watching the file
                            Task.Run(() =>
                            {
                                try
                                {
                                    OnLog($"Started watching file: {file.FilePath}", false);
                                    using var fileStream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                    using var streamReader = new StreamReader(fileStream);

                                    // Move the stream reader to the end of the file (to avoid reading old lines)
                                    streamReader.BaseStream.Seek(0, SeekOrigin.End);

                                    while (true)
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
                            });
                        }
                        // We only need to check for new files once in a while
                        Thread.Sleep(30000);
                    }
                });
            }

            private static List<FileWatched> FindFiles(string directoryPath, string filePattern)
            {
                var files = new List<FileWatched>();
                DirectoryInfo dir = new(directoryPath);
                foreach (var file in dir.GetFiles(filePattern))
                {
                    files.Add(new(file.FullName));
                }
                return files;
            }

            internal class FileWatched(string filePath)
            {
                public string FilePath { get; } = filePath;
            }
        }
    }
}
