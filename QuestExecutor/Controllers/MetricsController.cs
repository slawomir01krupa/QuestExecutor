using Microsoft.AspNetCore.Mvc;
using QuestExecutor.Core.Abstractions;

namespace QuestExecutor.Api.Controllers
{
    [Route("metrics")]
    public class MetricsController : Controller
    {
        private readonly IMetrics _metrics;

        public MetricsController(IMetrics metrics)
        {
            _metrics = metrics;
        }

        [HttpGet]
        public IActionResult Metrics()
        {
            return Ok(_metrics.ExportText());
        }
    }
}
