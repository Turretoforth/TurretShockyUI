namespace TurretShockyUI.Models
{
    public class ShockerConfigWindowResult
    {
        public bool ShouldSave { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public Shocker? Shocker { get; set; } = null;
    }
}
