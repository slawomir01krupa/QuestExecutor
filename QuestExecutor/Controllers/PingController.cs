using Microsoft.AspNetCore.Mvc;

namespace QuestExecutor.Api.Controllers
{
    [Route("ping")]
    public class PingController : Controller
    {
        [HttpGet]
        public IActionResult Ping()
        {
            return Ok("ping");
        }
    }
}
