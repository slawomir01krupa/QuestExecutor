using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using QuestExecutor.Api.Validators;
using QuestExecutor.Core.Constants;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Tests.QuestExecutor.Api.Tests.Validators
{

    public class ExecutionRequestValidatorTests
    {
        private static ExecutionRequestValidator CreateValidator(ProxyOptions? proxyOptions = null)
        {
            var options = Options.Create(proxyOptions ?? new ProxyOptions
            {
                MaxBodyBytes = 1024 * 1024,
                AllowedHeaders = new List<string> { HeaderNames.TargetBase, HeaderNames.CorrelationId, HeaderNames.ExecutorType, "Content-Type" },
                DefaultTimeoutMs = 1000,
                Retry = new RetryOptions { MaxAttempts = 3, BaseDelayMs = 100, MaxDelayMs = 1000, JitterPct = 0.2 }
            });
            return new ExecutionRequestValidator(options);
        }

        private static ExecutionRequest CreateValidRequest()
        {
            return new ExecutionRequest()
            {
                ExecutorType = "http",
                CorrelationId = Guid.NewGuid().ToString(),
                Target = "https://example.com",
                Method = "GET",
                Path = "/api/test",
                Body = "",
                Query = new Dictionary<string, string>(),
                Headers = new Dictionary<string, string>
                {
                    { HeaderNames.TargetBase, "https://example.com" },
                    { HeaderNames.CorrelationId, Guid.NewGuid().ToString() },
                    { HeaderNames.ExecutorType, "http" },
                    { "Content-Type", "application/json" }
                }
            };
        }

        [Fact]
        public void ValidRequest_PassesValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();

            var result = validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void MissingRequiredHeader_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.Headers.Remove(HeaderNames.TargetBase);

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Headers);
        }

        [Fact]
        public void InvalidExecutorType_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.ExecutorType = "invalid";

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExecutorType);
        }

        [Fact]
        public void InvalidHttpMethod_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.Method = "INVALID";

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Method);
        }

        [Fact]
        public void PathNotStartingWithSlash_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.Path = "not/starting/with/slash";

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Path);
        }

        [Fact]
        public void BodyExceedsMaxBytes_FailsValidation()
        {
            var validator = CreateValidator(new ProxyOptions { MaxBodyBytes = 1, AllowedHeaders = new List<string> { HeaderNames.TargetBase, HeaderNames.CorrelationId, HeaderNames.ExecutorType }, DefaultTimeoutMs = 1000, Retry = new RetryOptions { MaxAttempts = 3, BaseDelayMs = 100, MaxDelayMs = 1000, JitterPct = 0.2 } });
            var request = CreateValidRequest();
            request.Body = "This is more than one byte.";

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Body);
        }

        [Fact]
        public void HeadersNotInAllowedList_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.Headers.Add("X-NotAllowed", "value");

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Headers);
        }

        [Fact]
        public void HttpExecutor_InvalidTargetUri_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.Target = "not-a-valid-uri";

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Target);
        }

        [Fact]
        public void PowerShellExecutor_InvalidBody_FailsValidation()
        {
            var validator = CreateValidator();
            var request = CreateValidRequest();
            request.ExecutorType = "powershell";
            request.Method = "POST";
            request.Body = "not-json";

            var result = validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Body);
        }
    }
}
