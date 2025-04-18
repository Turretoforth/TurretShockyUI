namespace TurretShockyUI.Models
{
    public class ApiConfigWindowResult()
    {
        public bool ShouldSave { get; set; } = false;
        public ApiSettings? ApiPrefs { get; set; } = null;
    }
}
