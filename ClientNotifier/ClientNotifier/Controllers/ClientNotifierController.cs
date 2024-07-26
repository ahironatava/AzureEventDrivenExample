using ClientNotifier.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebHookAbstraction;

namespace ClientNotifier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientNotifierController : WebHookController
    {
        private readonly IClientNotifierService _clientNotifierService;
        private readonly ILogger<ClientNotifierController> _logger;

        public ClientNotifierController(IClientNotifierService clientNotifierService, ILogger<ClientNotifierController> logger)
        {
            _clientNotifierService = clientNotifierService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            return await PostOrchestration(Request.Body);
        }

        public override async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            List<string> validEventTypes = new List<string> { "ProcessingCompleteEvent" };

            (var validEvent, string errMsg) = GetValidEvent(jsonContent, validEventTypes);
            if(validEvent == null)
            {
                _logger.LogError(errMsg);
                return BadRequest(errMsg);
            }
            
            bool success = await _clientNotifierService.NotifyClient(validEvent);
            if (success)
            {
                _logger.LogInformation("NotifyClient successful");
                return Ok();
            }
            else
            {
                _logger.LogInformation("NotifyClient system error");
                return StatusCode(500);
            }
        }
    }
}
