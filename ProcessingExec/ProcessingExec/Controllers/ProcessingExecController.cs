using CommonModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessingExec.Interfaces;
using System.Text;
using WebHookAbstraction;

namespace ProcessingExec.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcessingExecController : WebHookController
    {
        private readonly IProcessingService _processingService;
        private readonly ILogger<ProcessingExecController> _logger;

        public ProcessingExecController(IProcessingService processingService, ILogger<ProcessingExecController> logger)
        {
            _processingService = processingService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            return await PostOrchestration(Request.Body);
        }

        public override async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            List<string> validEventTypes = new List<string> { "ProcConfigCreatedEvent" };

            (var validEvent, string errMsg) = GetValidEvent(jsonContent, validEventTypes);
            if (validEvent == null)
            {
                _logger.LogError(errMsg);
                return BadRequest(errMsg);
            }

            var success = await _processingService.ApplyConfiguredProcessing(validEvent);
            if (success)
            {
                _logger.LogInformation("ApplyConfiguredProcessing successful");
                return Ok();
            }
            else
            {
                _logger.LogInformation("ApplyConfiguredProcessing system error");
                return StatusCode(500);
            }
        }
    }
}
