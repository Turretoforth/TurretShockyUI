namespace TurretShockyUI.Models
{
    public class ApiConfigWindowResult()
    {
        public bool ShouldSave { get; set; } = false;
        public ApiPrefs? ApiPrefs { get; set; } = null;
    }
}
