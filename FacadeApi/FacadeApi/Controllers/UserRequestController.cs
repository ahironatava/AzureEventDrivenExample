using CommonModels;
using FacadeApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FacadeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserRequestController : ControllerBase
    {
        private readonly IUserRequestService _userRequestService;
        public UserRequestController(IUserRequestService userRequestService)
        {
            _userRequestService = userRequestService;
        }

        [HttpPost]
        public async Task Post([FromBody] Transaction transaction)
        {
            if (!IsValidTransaction(transaction))
            {
                Response.StatusCode = 400;
                return;
            }

            string userName = GetUserIdFromToken();
            if(string.IsNullOrWhiteSpace(userName))
            {
                Response.StatusCode = 401;
                return;
            }

            (bool serviceSuccess, string recordId) = await _userRequestService.ProcessRequest(transaction, userName);
            Response.StatusCode = serviceSuccess ? 202 : 500;
            if(serviceSuccess)
            {
                Response.Headers.Append("Location", $"api/Facade/{recordId}");
            }

            return;
        }

        private bool IsValidTransaction(Transaction transaction)
        {
            return !string.IsNullOrEmpty(transaction.StockName) &&
                   !string.IsNullOrEmpty(transaction.TransactionType) &&
                   (transaction.TransactionType.ToLower() == "buy" || transaction.TransactionType.ToLower() == "sell") &&
                   transaction.Quantity > 0;
        }

        private string GetUserIdFromToken()
        {
            // This is a fake implementation
            return "fake name";
        }
    }
}
