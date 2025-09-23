namespace QuestExecutor.Core.Options
{
    public class ProxyOptions
    {
        public int MaxBodyBytes { get; set; } = 1_048_576;

        public List<string> AllowedHeaders { get; set; } = new();

        public int DefaultTimeoutMs { get; set; } = 30_000;

        public RetryOptions Retry { get; set; } = new();
    }
}
