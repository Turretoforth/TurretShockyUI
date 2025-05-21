using System.Collections.Generic;

namespace TurretShocky.Models
{
    public class AppSettingsWindowResult
    {
        public bool WatchFiles { get; set; } = false;

        public CooldownBehaviour CooldownBehaviour { get; set; } = CooldownBehaviour.Ignore;

        public List<FileSettings> FilesSettings { get; set; } = [];
    }
}
