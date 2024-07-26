using Microsoft.AspNetCore.Mvc;
using ProcConfigBuilder.Interfaces;
using WebHookAbstraction;

namespace ProcConfigBuilder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcConfigBuilderController : WebHookController
    {
        private readonly IProcConfigBuilderService _procConfigBuilderService;
        private readonly ILogger<ProcConfigBuilderController> _logger;

        public ProcConfigBuilderController(IProcConfigBuilderService procConfigBuilderService, ILogger<ProcConfigBuilderController> logger)
        {
            _procConfigBuilderService = procConfigBuilderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            return await PostOrchestration(Request.Body);
        }

        public override async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            List<string> validEventTypes = new List<string> { "UserRequestEvent" };

            (var validEvent, string errMsg) = GetValidEvent(jsonContent, validEventTypes);
            if (validEvent == null)
            {
                _logger.LogError(errMsg);
                return BadRequest(errMsg);
            }

            bool success = await _procConfigBuilderService.CreateAndPublishProcConfigFile(validEvent);           
            if(success)
            {
                _logger.LogInformation("CreateAndPublishProcConfigFile succeeded.");
                return Ok();
            }
            else 
            {
                _logger.LogError("CreateAndPublishProcConfigFile system error.");
                return StatusCode(500);
            }   
        }
    }
}
