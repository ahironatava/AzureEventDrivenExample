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
            _logger.LogInformation($"HandleGridEvents called with jsonContent: {jsonContent}");

            List<string> validEventTypes = new List<string> { "UserRequestEvent" };

            (var validEvent, string errMsg) = GetValidEvent(jsonContent, validEventTypes);
            if (validEvent == null)
            {
                _logger.LogError(errMsg);
                return BadRequest(errMsg);
            }

            UserRequest userRequest = (UserRequest)Activator.CreateInstance(typeof(UserRequest), validEvent.Data.ToString());

            _logger.LogInformation($"UserRequest.RequestId = {userRequest.RequestId}");
            _logger.LogInformation($"UserRequest.UserName = {userRequest.UserName}");
            _logger.LogInformation($"UserRequest.Transction.TransactionType = {userRequest.UserTransaction.TransactionType}");
            _logger.LogInformation($"UserRequest.Transction.StockName = {userRequest.UserTransaction.StockName}");
            _logger.LogInformation($"UserRequest.Transction.Quantity = {userRequest.UserTransaction.Quantity}");

            if (!HasMandatoryContent(userRequest))
            {
                return BadRequest("Mandatory content is missing.");
            }

            int statusCode = await _procConfigBuilderService.CreateAndPublishProcConfigFile(userRequest);           
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

        private bool HasMandatoryContent(UserRequest userRequest)
        {
            if(string.IsNullOrWhiteSpace(userRequest.RequestId) || string.IsNullOrWhiteSpace(userRequest.UserName))
            {
                _logger.LogError("RequestId or UserName is missing.");
                return false;
            }

            Transaction transaction = userRequest.UserTransaction;

            if(string.IsNullOrWhiteSpace(transaction.TransactionType) || string.IsNullOrWhiteSpace(transaction.StockName) || transaction.Quantity < 1)
            {
                _logger.LogError("Transaction Type or StockName is missing, or StockQuantity is less than 1");
                return false;
            }

            return true;
        }
    }
}
