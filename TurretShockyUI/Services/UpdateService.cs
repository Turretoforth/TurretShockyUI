using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TurretShocky.Services
{
    public interface IUpdateService
    {
        string LatestStableVersion { get; }
        string LatestStableVersionUrl { get; }
        Task<bool> CheckForUpdates();
        Task DownloadUpdateToCurrentFolder(string targetFileName);
    }

    public class UpdateService : IUpdateService
    {
        private readonly HttpClient httpClient = new();
        private readonly string owner;
        private readonly string repo;
        private readonly string expectedZipName;
        private readonly string _currentVer;
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        public string LatestStableVersion { get; private set; } = string.Empty;
        public string LatestStableVersionUrl { get; private set; } = string.Empty;

        public UpdateService(string owner, string repo, string expectedZipName)
        {
            this.owner = owner;
            this.repo = repo;
            this.expectedZipName = expectedZipName;
            _currentVer = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TurretShocky/" + _currentVer);
        }

        public async Task<bool> CheckForUpdates()
        {
            GithubReleaseInfo? latestRelease = await GetLatestRelease() ?? throw new InvalidOperationException($"Latest release not found");
            string latestVersion = latestRelease.Tag_Name.Replace("v", ""); // Remove the 'v' prefix from the version string

            // Handle '*' in expectedZipName as a wildcard
            GithubAsset? githubAsset = null;
            if (expectedZipName.Contains('*'))
            {
                Regex regex = new(expectedZipName.Replace(".", "\\.").Replace("*", ".*"), RegexOptions.IgnoreCase);
                githubAsset = latestRelease.Assets.FirstOrDefault(a => regex.IsMatch(a.Name));
            }
            else
            {
                githubAsset = latestRelease.Assets.FirstOrDefault(a => a.Name.Equals(expectedZipName, StringComparison.OrdinalIgnoreCase));
            }

            if (githubAsset == default)
            {
                throw new InvalidOperationException($"No asset matching '{expectedZipName}' found in the latest release.");
            }

            // Update latest version information
            LatestStableVersionUrl = githubAsset.Browser_Download_Url;
            LatestStableVersion = latestVersion;

            // Compare with the current version
            Version latest;
            latest = new(latestVersion);
            Version current = new(_currentVer);
            return latest > current; // Return true if an update is available
        }

        private async Task<GithubReleaseInfo?> GetLatestRelease()
        {
            // The endpoint gets the latest non-prerelease release
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            GithubReleaseInfo? releaseInfo = null;
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                releaseInfo = JsonSerializer.Deserialize<GithubReleaseInfo>(jsonResponse, jsonSerializerOptions);
            }

            return releaseInfo;
        }

        public async Task DownloadUpdateToCurrentFolder(string targetFileName)
        {
            // Download the latest release
            HttpResponseMessage assetResponse = await httpClient.GetAsync(LatestStableVersionUrl);
            if (assetResponse.IsSuccessStatusCode)
            {
                byte[] fileBytes = await assetResponse.Content.ReadAsByteArrayAsync();
                string filePath = Path.Combine(Environment.CurrentDirectory, targetFileName);
                await File.WriteAllBytesAsync(filePath, fileBytes);
            }
            else
            {
                throw new HttpRequestException($"Failed to download latest release: {assetResponse.StatusCode} - {await assetResponse.Content.ReadAsStringAsync()}");
            }
        }
    }

    public class GithubReleaseInfo
    {
        public string Url { get; set; } = string.Empty;
        public string Assets_Url { get; set; } = string.Empty;
        public string Upload_Url { get; set; } = string.Empty;
        public string Html_Url { get; set; } = string.Empty;
        public long Id { get; set; } = 0;
        public GithubAuthor Author { get; set; } = new();
        public string Node_Id { get; set; } = string.Empty;
        public string Tag_Name { get; set; } = string.Empty;
        public string Target_Commitish { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Draft { get; set; } = false;
        public bool Prerelease { get; set; } = false;
        public DateTime Created_At { get; set; } = DateTime.MinValue;
        public DateTime Published_At { get; set; } = DateTime.MinValue;
        public List<GithubAsset> Assets { get; set; } = new();
        public string Tarball_Url { get; set; } = string.Empty;
        public string Zipball_Url { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public GithubReactions Reactions { get; set; } = new();
        public int Mentions_Count { get; set; } = 0;
    }

    public class GithubReactions
    {
        public string Url { get; set; } = string.Empty;
        public int Total_Count { get; set; } = 0;
        [JsonPropertyName("+1")]
        public int Plus_1 { get; set; } = 0;
        [JsonPropertyName("-1")]
        public int Minus_1 { get; set; } = 0;
        public int Laugh { get; set; } = 0;
        public int Confused { get; set; } = 0;
        public int Heart { get; set; } = 0;
        public int Hooray { get; set; } = 0;
        public int Rocket { get; set; } = 0;
        public int Eyes { get; set; } = 0;
    }

    public class GithubAsset
    {
        public string Url { get; set; } = string.Empty;
        long Id { get; set; } = 0;
        public string Node_Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Label { get; set; } = null;
        public GithubAuthor Uploader { get; set; } = new();
        public string Content_Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public long Size { get; set; } = 0;
        public long Download_Count { get; set; } = 0;
        public DateTime Created_At { get; set; } = DateTime.MinValue;
        public DateTime Updated_At { get; set; } = DateTime.MinValue;
        public string Browser_Download_Url { get; set; } = string.Empty;
    }

    public class GithubAuthor
    {
        public string Login { get; set; } = string.Empty;
        public long Id { get; set; } = 0;
        public string Node_Id { get; set; } = string.Empty;
        public string Avatar_Url { get; set; } = string.Empty;
        public string Gravatar_Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Html_Url { get; set; } = string.Empty;
        public string Followers_Url { get; set; } = string.Empty;
        public string Following_Url { get; set; } = string.Empty;
        public string Gists_Url { get; set; } = string.Empty;
        public string Starred_Url { get; set; } = string.Empty;
        public string Subscriptions_Url { get; set; } = string.Empty;
        public string Organizations_Url { get; set; } = string.Empty;
        public string Repos_Url { get; set; } = string.Empty;
        public string Events_Url { get; set; } = string.Empty;
        public string Received_Events_Url { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string User_View_Type { get; set; } = string.Empty;
        public bool Site_Admin { get; set; } = false;
    }
}
