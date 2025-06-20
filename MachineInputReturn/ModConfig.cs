namespace MachineInputReturn
{
    public enum RetrievalTimeOption
    {
        Anytime,
        Below50Percent,
        Below30Percent
    }

    public class ModConfig
    {
        public bool Enabled { get; set; } = true;
        public RetrievalTimeOption RetrievalTime { get; set; } = RetrievalTimeOption.Anytime;
    }
}