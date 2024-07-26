using CommonModels;
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

            if (!HasMandatoryContent(validEvent))
            {
                return BadRequest("Mandatory content is missing.");
            }

            int statusCode = await _procConfigBuilderService.CreateAndPublishProcConfigFile(validEvent);           
            if(statusCode == 200)
            {
                _logger.LogInformation("CreateAndPublishProcConfigFile succeeded.");
                return Ok();
            }
            else 
            {
                _logger.LogError($"CreateAndPublishProcConfigFile system error {statusCode}");
                return StatusCode(statusCode);
            }   
        }

        private bool HasMandatoryContent(GridEvent<dynamic> gridEvent)
        {
            var userRequest = gridEvent.Data.UserRequest;
            var requestId = userRequest?.RequestId;
            var userName = userRequest?.UserName;
            if(string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(userName))
            {
                _logger.LogError("RequestId or UserName is missing.");
                return false;
            }

            var userTransaction = gridEvent.Data.UserRequest?.UserTransaction;
            var transactionType = userTransaction?.TransactionType;
            var stockName = userTransaction?.StockName;
            var stockQuantity = userTransaction?.StockQuantity;
            if(string.IsNullOrWhiteSpace(transactionType) || string.IsNullOrWhiteSpace(stockName) || stockQuantity < 1)
            {
                _logger.LogError("Transaction Type or StockName is missing, or StockQuantity is less than 1");
                return false;
            }

            return true;
        }
    }
}
