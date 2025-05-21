using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TurretShocky.Services
{
    // TODO: Implement fetching the latest version from the server and downloading the update
    // (https://docs.github.com/en/repositories/releasing-projects-on-github/linking-to-releases)
    // Or use api https://docs.github.com/en/rest/releases/releases?apiVersion=2022-11-28#get-the-latest-release
    public class UpdateService(string baseUpdateUrl)
    {
        public string LatestStableVersion { get; private set; } = string.Empty;
        public string LatestPreReleaseVersion { get; private set; } = string.Empty;
        public async Task<bool> CheckForUpdates()
        {
            // Simulate checking for updates
            await Task.Delay(1000);

            string latestVersion = "1.0.1"; // This would be fetched from the server in a real scenario

            // Compare with the current version
            string currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            Version latest;
            // Handle pre-release versions
            if (latestVersion.ToString().Contains('-'))
            {
                LatestPreReleaseVersion = latestVersion;
                latest = new Version(latestVersion.Split('-')[0]);
            }
            else
            {
                latest = new(latestVersion);
            }
            Version current = new(currentVersion);
            if (latest > current)
            {
                LatestStableVersion = latest.ToString();
                return true; // Update available
            }
            else
            {
                return false; // No update available
            }
        }
    }
}
