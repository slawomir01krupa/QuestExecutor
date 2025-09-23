using Microsoft.Extensions.Logging;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace QuestExecutor.Executors.Powershell
{
    public class PowershellExecutor : IExecutor
    {
        private readonly ILogger<PowershellExecutor> _log;

        public string ExecutorType => "powershell";

        public PowershellExecutor(ILogger<PowershellExecutor> log)
        {
            _log = log;
        }

        public async Task<ExecutorOutcome> ExecuteAsync(ExecutionRequest req)
        {
            if (!req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = ErrorCode.InvalidSchema.ToString()
                };
            }

            if (string.IsNullOrWhiteSpace(req.Body) || !IsValidJson(req.Body))
            {
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = ErrorCode.InvalidSchema.ToString()
                };
            }

            try
            {
                var node = JsonNode.Parse(req.Body)!.AsObject();
                var command = node["command"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(command) || !IsAllowlisted(command))
                {
                    return new ExecutorOutcome
                    {
                        Success = false,
                        Error = ErrorCode.CommandNotAllowlisted.ToString()
                    };
                }

                var parameters = node["parameters"] as JsonObject;

                _log.LogInformation("Simulating PowerShell command {command}", command);

                await Task.Delay(50);

                var result = new PowershellExecutorResult
                {
                    Command = command,
                    Parameters = parameters?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")
                                            ?? new Dictionary<string, string>(),
                    Output = $"Executed command {command} successfully (simulated)."
                };

                return new ExecutorOutcome
                {
                    Success = true,
                    Error = null,
                    Payload = result
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "PowerShell execution error");
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = ErrorCode.Unknown.ToString()
                };
            }
        }

        private static bool IsValidJson(string body)
        {
            try { JsonNode.Parse(body); return true; }
            catch { return false; }
        }

        private static bool IsAllowlisted(string command)
        {

            var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
             {
                 "list-mailboxes",
                 "list-users"
             };

            return allow.Contains(command);
        }
    }
}
