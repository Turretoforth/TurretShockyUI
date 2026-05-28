namespace TurretShocky.Models
{
    public class ShockerAction
    {
        public required string Code { get; set; }
        public required ShockerType Type { get; set; }
        public required FunType FunType { get; set; }
        public required int Duration { get; set; }
        public required int Intensity { get; set; }
    }
}
