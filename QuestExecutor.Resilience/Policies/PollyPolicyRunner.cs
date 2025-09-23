using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Resilience.Policies
{
    public class PollyPolicyRunner : IPolicyRunner
    {
        private readonly IClock _clock;
        private readonly RetryOptions _retry; // from your ProxyOptions.Retry

        public PollyPolicyRunner(IClock clock, IOptions<ProxyOptions> proxy)
        {
            _clock = clock;
            _retry = proxy.Value.Retry;
        }

        public async Task<(ExecutorOutcome outcome, List<AttemptSummary> attempts)> ExecuteAsync(Func<Task<ExecutorOutcome>> attempt, int maxAttempts, TimeSpan perAttemptTimeout)
        {
            var attempts = new List<AttemptSummary>();
            maxAttempts = Math.Max(1, maxAttempts);

            var retryPolicy = Policy
                .Handle<TimeoutRejectedException>()
                .OrResult<ExecutorOutcome>(o => !o.Success)
                .WaitAndRetryAsync(
                    retryCount: maxAttempts - 1,
                    sleepDurationProvider: n => ComputeBackoff(n),
                    onRetryAsync: (_, __, ___, ____) => Task.CompletedTask);

            var timeoutPolicy = Policy.TimeoutAsync<ExecutorOutcome>(perAttemptTimeout, TimeoutStrategy.Pessimistic);

            var policy = Policy.WrapAsync(timeoutPolicy, retryPolicy);
            async Task<ExecutorOutcome> TimedAttempt()
            {
                var started = _clock.UtcNow;
                try
                {
                    var res = await attempt();
                    attempts.Add(new AttemptSummary
                    {
                        Number = attempts.Count + 1,
                        StartedAtUtc = started,
                        EndedAtUtc = _clock.UtcNow,
                        Outcome = res.Success ? "Success" : "Failure",
                        Error = res.Error,
                    });
                    return res;
                }
                catch (TimeoutRejectedException)
                {
                    attempts.Add(new AttemptSummary
                    {
                        Number = attempts.Count + 1,
                        StartedAtUtc = started,
                        EndedAtUtc = _clock.UtcNow,
                        Outcome = "Timeout",
                        Error = nameof(TimeoutRejectedException)
                    });
                    throw;
                }
                catch (Exception ex)
                {
                    attempts.Add(new AttemptSummary
                    {
                        Number = attempts.Count + 1,
                        StartedAtUtc = started,
                        EndedAtUtc = _clock.UtcNow,
                        Outcome = "Failure",
                        Error = ex.Message
                    });
                    throw;
                }
            }

            ExecutorOutcome final;
            try
            {
                final = await policy.ExecuteAsync(() => TimedAttempt());
            }
            catch (TimeoutRejectedException)
            {
                final = new ExecutorOutcome { Success = false,  Error = nameof(TimeoutRejectedException) };
            }
            catch(Exception ex)
            {
                final = new ExecutorOutcome { Success = false,  Error = ex.Message };
            }

            return (final, attempts);
        }

        private TimeSpan ComputeBackoff(int attemptNumber)
        {
            var baseMs = _retry.BaseDelayMs;
            var maxMs = _retry.MaxDelayMs;
            var jitter = _retry.JitterPct;

            var exp = Math.Min(maxMs, baseMs * Math.Pow(2, Math.Max(0, attemptNumber - 1)));
            var delta = exp * jitter;
            var rnd = Random.Shared.NextDouble() * 2 - 1;
            var ms = Math.Max(0, exp + delta * rnd);
            return TimeSpan.FromMilliseconds(ms);
        }
    }
}
