using CommonModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Repository.Interfaces;

namespace Repository.Controllers
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


        [HttpGet("{id}")]
        public async Task<ActionResult<UserRequest>> Get(string id)
        {
            var userRequest = await _userRequestService.GetUserRequest(id);
            if (userRequest == null)
            {
                return NotFound();
            }
            return Ok(userRequest);
        }

        [HttpPost]
        public async Task<ActionResult<UserRequest>> Post([FromBody] Object jsonObject)
        {
            var userRequest = JsonConvert.DeserializeObject<UserRequest>(jsonObject.ToString());
            var returnCode = await _userRequestService.AddUserRequest(userRequest);
            if (returnCode != 0)
            {
                return BadRequest(userRequest);
            }
            return Ok(userRequest);
        }
    }

}
