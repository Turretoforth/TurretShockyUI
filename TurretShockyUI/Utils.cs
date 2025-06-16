using System.Reflection;

namespace TurretShocky
{
    public static class Utils
    {
        public static string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        }
    }
}
