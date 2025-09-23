namespace QuestExecutor.Core.Options
{
    public class RetryOptions
    {
        public int MaxAttempts { get; set; } = 3;

        public int BaseDelayMs { get; set; } = 100;

        public int MaxDelayMs { get; set; } = 2000;

        public double JitterPct { get; set; } = 0.2;
    }
}
