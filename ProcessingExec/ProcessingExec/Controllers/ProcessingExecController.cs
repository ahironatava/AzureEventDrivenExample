using CommonModels;
using Microsoft.AspNetCore.Mvc;
using ProcessingExec.Interfaces;
using WebHookAbstraction;

namespace ProcessingExec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            _logger.LogInformation($"HandleGridEvents called with jsonContent: {jsonContent}");

            List<string> validEventTypes = new List<string> { "ProcConfigCreatedEvent" };

            (var validEvent, string errMsg) = GetValidEvent(jsonContent, validEventTypes);
            if (validEvent == null)
            {
                _logger.LogError(errMsg);
                return BadRequest(errMsg);
            }

            string requestId = validEvent.Subject;
            _logger.LogInformation($"\nrequestId parsed as: {requestId}\n");

            int statusCode = await _processingService.ApplyConfiguredProcessing(requestId);
            if (statusCode == 200)
            {
                _logger.LogInformation("ApplyConfiguredProcessing successful\n");
                return Ok();
            }
            else
            {
                _logger.LogInformation("ApplyConfiguredProcessing system error\n");
                return StatusCode(statusCode);
            }
        }
    }
}
