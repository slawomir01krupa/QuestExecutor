using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QuestExecutor.Api.Orchestration;
using QuestExecutor.Api.Validators;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.Options;
using QuestExecutor.Executors.Http;
using QuestExecutor.Executors.Powershell;
using QuestExecutor.Observability.Logging;
using QuestExecutor.Resilience.Policies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient(nameof(HttpExecutor))
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddKeyedTransient<IExecutor>("http", (sp, key) =>
    ActivatorUtilities.CreateInstance<HttpExecutor>(sp));

builder.Services.AddKeyedTransient<IExecutor>("powershell", (sp, key) =>
    ActivatorUtilities.CreateInstance<PowershellExecutor>(sp));

builder.Services.Configure<ProxyOptions>(builder.Configuration.GetSection("ProxyOptions"));
builder.Services.AddSingleton<IExecutorRegistry, KeyedExecutorRegistry>();
builder.Services.AddScoped<IValidator<ExecutionRequest>, ExecutionRequestValidator>();
builder.Services.AddSingleton<IMetrics, InMemoryMetrics>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPolicyRunner, PollyPolicyRunner>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.FormatterName = "json");
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
