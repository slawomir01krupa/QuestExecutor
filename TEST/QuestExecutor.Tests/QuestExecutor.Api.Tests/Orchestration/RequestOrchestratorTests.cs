using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using QuestExecutor.Api.Orchestration;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.Options;
using System;
using System.Threading.Tasks;
using Xunit;

namespace QuestExecutor.Tests.QuestExecutor.Api.Tests.Orchestration
{
    public class RequestOrchestratorTests
    {
        private readonly Mock<IValidator<ExecutionRequest>> _validatorMock;
        private readonly Mock<IExecutorRegistry> _registryMock;
        private readonly Mock<ILogger<RequestOrchestrator>> _loggerMock;
        private readonly Mock<IMetrics> _metricsMock;
        private readonly Mock<IPolicyRunner> _policyRunnerMock;
        private readonly IOptions<ProxyOptions> _proxyOptions;
        private readonly RequestOrchestrator _orchestrator;

        public RequestOrchestratorTests()
        {
            _validatorMock = new Mock<IValidator<ExecutionRequest>>();
            _registryMock = new Mock<IExecutorRegistry>();
            _loggerMock = new Mock<ILogger<RequestOrchestrator>>();
            _metricsMock = new Mock<IMetrics>();
            _policyRunnerMock = new Mock<IPolicyRunner>();
            _proxyOptions = Options.Create(new ProxyOptions
            {
                MaxBodyBytes = 1024 * 1024,
                AllowedHeaders = new List<string> { "X-Target-Base", "X-Correlation-Id", "X-Executor-Type" },
                DefaultTimeoutMs = 1000,
                Retry = new RetryOptions { MaxAttempts = 1, BaseDelayMs = 10, MaxDelayMs = 100, JitterPct = 0.1 }
            });

            _orchestrator = new RequestOrchestrator(
                _validatorMock.Object,
                _registryMock.Object,
                _loggerMock.Object,
                _metricsMock.Object,
                _policyRunnerMock.Object,
                _proxyOptions
            );
        }

        private static ExecutionRequest CreateValidRequest()
        {
            return new ExecutionRequest
            {
                RequestId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid().ToString(),
                ExecutorType = "http",
                Target = "https://example.com",
                Method = "GET",
                Path = "/api/test",
                Body = "",
                Query = new Dictionary<string, string>(),
                Headers = new Dictionary<string, string>
                {
                    { "X-Target-Base", "https://example.com" },
                    { "X-Correlation-Id", Guid.NewGuid().ToString() },
                    { "X-Executor-Type", "http" }
                }
            };
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_ReturnsSuccessEnvelope()
        {
            // Arrange
            var request = CreateValidRequest();
            _validatorMock.Setup(v => v.Validate(request))
                .Returns(new ValidationResult());

            var executorMock = new Mock<IExecutor>();
            executorMock.SetupGet(e => e.ExecutorType).Returns("http");
            executorMock.Setup(e => e.ExecuteAsync(request))
                .ReturnsAsync(new ExecutorOutcome { Success = true, Payload = "result" });

            _registryMock.Setup(r => r.Resolve("http")).Returns(executorMock.Object);

            _policyRunnerMock.Setup(p => p.ExecuteAsync(
                It.IsAny<Func<Task<ExecutorOutcome>>>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync((Func<Task<ExecutorOutcome>> attempt, int maxAttempts, TimeSpan timeout) =>
                {
                    var outcome = attempt().Result;
                    return (outcome, new List<AttemptSummary>());
                });

            // Act
            var envelope = await _orchestrator.HandleAsync(request);

            // Assert
            Assert.NotNull(envelope);
            Assert.Equal("Success", envelope.Status);
            Assert.Empty(envelope.Errors);
            Assert.Equal("http", envelope.ExecutorType);
            Assert.Equal("result", envelope.Result);
        }

        [Fact]
        public async Task HandleAsync_InvalidRequest_ReturnsFailedEnvelope()
        {
            // Arrange
            var request = CreateValidRequest();
            var validationResult = new ValidationResult(new[] { new ValidationFailure("Field", "Error") });
            _validatorMock.Setup(v => v.Validate(request)).Returns(validationResult);

            // Act
            var envelope = await _orchestrator.HandleAsync(request);

            // Assert
            Assert.NotNull(envelope);
            Assert.Equal("Failed", envelope.Status);
            Assert.Contains("Error", envelope.Errors);
        }

        [Fact]
        public async Task HandleAsync_UnknownExecutorType_ReturnsFailedEnvelope()
        {
            // Arrange
            var request = CreateValidRequest();
            _validatorMock.Setup(v => v.Validate(request)).Returns(new ValidationResult());
            _registryMock.Setup(r => r.Resolve(It.IsAny<string>())).Returns((IExecutor)null);

            // Act
            var envelope = await _orchestrator.HandleAsync(request);

            // Assert
            Assert.NotNull(envelope);
            Assert.Equal("Failed", envelope.Status);
            Assert.Contains("No executor found", envelope.Errors[0]);
        }

        [Fact]
        public async Task HandleAsync_ExecutorFails_ReturnsFailedEnvelope()
        {
            // Arrange
            var request = CreateValidRequest();
            _validatorMock.Setup(v => v.Validate(request)).Returns(new ValidationResult());

            var executorMock = new Mock<IExecutor>();
            executorMock.SetupGet(e => e.ExecutorType).Returns("http");
            executorMock.Setup(e => e.ExecuteAsync(request))
                .ReturnsAsync(new ExecutorOutcome { Success = false, Error = "Executor error" });

            _registryMock.Setup(r => r.Resolve("http")).Returns(executorMock.Object);

            _policyRunnerMock.Setup(p => p.ExecuteAsync(
                It.IsAny<Func<Task<ExecutorOutcome>>>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync((Func<Task<ExecutorOutcome>> attempt, int maxAttempts, TimeSpan timeout) =>
                {
                    var outcome = attempt().Result;
                    return (outcome, new List<AttemptSummary>());
                });

            // Act
            var envelope = await _orchestrator.HandleAsync(request);

            // Assert
            Assert.NotNull(envelope);
            Assert.Equal("Failed", envelope.Status);
            Assert.Contains("Executor error", envelope.Errors);
        }
    }
}
