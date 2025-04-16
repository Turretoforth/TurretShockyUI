using System.Collections.Generic;

namespace TurretShockyUI.Models
{
    public class AppSettingsWindowResult
    {
        public bool WatchFiles { get; set; } = false;

        public List<FilesSettings> FilesSettings { get; set; } = [];
    }
}
