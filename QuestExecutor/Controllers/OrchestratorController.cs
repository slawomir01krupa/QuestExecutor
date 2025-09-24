using Microsoft.AspNetCore.Mvc;
using QuestExecutor.Api.Models;
using QuestExecutor.Api.Orchestration;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Constants;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.Extensions;

namespace QuestExecutor.Api.Controllers
{
    [Route("api/{**path}")]
    [ApiController]
    public class OrchestratorController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRequestOrchestrator _requestOrchestrator;

        public OrchestratorController(IHttpContextAccessor httpContextAccessor, IRequestOrchestrator requestOrchestrator)
        {
            _httpContextAccessor = httpContextAccessor;
            _requestOrchestrator = requestOrchestrator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string path)
        {
            var executionRequest = PopulateExecutionRequest(path);
            var result = await _requestOrchestrator.HandleAsync(executionRequest);
            
            if (result.Errors.Any()) 
            {                 
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post(string path, [FromBody] ExecutionWebRequest body)
        {
            var executionRequest = PopulateExecutionRequest(path, body);
            var result = await _requestOrchestrator.HandleAsync(executionRequest);

            if (result.Errors.Any())
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Put(string path, [FromBody] ExecutionWebRequest body)
        {
            var executionRequest = PopulateExecutionRequest(path, body);
            var result = await _requestOrchestrator.HandleAsync(executionRequest);

            if (result.Errors.Any())
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(string path, [FromBody] ExecutionWebRequest body)
        {
            var executionRequest = PopulateExecutionRequest(path, body);
            var result = await _requestOrchestrator.HandleAsync(executionRequest);

            if (result.Errors.Any())
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string path)
        {
            var executionRequest = PopulateExecutionRequest(path);
            var result = await _requestOrchestrator.HandleAsync(executionRequest);

            if (result.Errors.Any())
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        private ExecutionRequest PopulateExecutionRequest(string path, ExecutionWebRequest? webRequest = null)
        {
            var result = new ExecutionRequest();
            var request = _httpContextAccessor.HttpContext?.Request;

            result.Path = $"/{path}";
            result.Query = request?.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            result.Headers = request?.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            result.Target = request?.Headers.GetHeaderValue(HeaderNames.TargetBase) ?? string.Empty;
            result.CorrelationId = request?.Headers.GetHeaderValue(HeaderNames.CorrelationId) ?? string.Empty;
            result.ExecutorType = request?.Headers.GetHeaderValue(HeaderNames.ExecutorType) ?? string.Empty;
            result.Method = request?.Method ?? string.Empty;
            result.Body = webRequest?.Body ?? string.Empty;    

            return result;
        }
    }
}
