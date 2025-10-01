using Microsoft.Extensions.Logging;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.enums;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
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
                var paramDict = parameters?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")
                                ?? new Dictionary<string, string>();

                _log.LogInformation("Establishing SSH session for PowerShell command {command}", command);

                string target = req.Target;
                string username = node["username"]?.GetValue<string>() ?? "your-username";
                string password = node["password"]?.GetValue<string>() ?? "your-password";

                string? portStr = node["port"]?.GetValue<string>();
                int port = 22;
                if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out int parsedPort))
                {
                    port = parsedPort;
                }

                string psCommand = MapCommand(command);
                if (paramDict.Count > 0)
                {
                    var paramString = string.Join(" ", paramDict.Select(p => $"-{p.Key} \"{p.Value}\""));
                    psCommand = $"{psCommand} {paramString}";
                }

                string output = string.Empty;
                string error = string.Empty;

                using (var client = new SshClient(target, port, username, password))
                {
                    client.Connect();
                    using (var cmd = client.CreateCommand($"powershell -NoProfile -NonInteractive -Command \"{psCommand}\""))
                    {
                        var asyncResult = await Task.Run(() => cmd.BeginExecute());
                        output = cmd.EndExecute(asyncResult);

                        if (!string.IsNullOrWhiteSpace(cmd.Error))
                        {
                            error = cmd.Error;
                            _log.LogError("PowerShell SSH error: {error}", error);
                            return new ExecutorOutcome
                            {
                                Success = false,
                                Error = error
                            };
                        }
                    }
                    client.Disconnect();
                }

                var result = new PowershellExecutorResult
                {
                    Command = command,
                    Parameters = paramDict,
                    Output = output
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
                _log.LogError(ex, "PowerShell SSH execution error");
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

        private static string MapCommand(string command)
        {
            return command.ToLowerInvariant() switch
            {
                "list-mailboxes" => "Get-Mailbox",
                "list-users" => "Get-LocalUser",
                _ => throw new InvalidOperationException("Command not allowlisted")
            };
        }
    }
}


