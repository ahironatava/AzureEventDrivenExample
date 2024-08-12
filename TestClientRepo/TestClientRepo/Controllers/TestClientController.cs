using CommonModels;
using Microsoft.AspNetCore.Mvc;
using TestClientRepo.Interfaces;

namespace TestClientRepo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestClientController : ControllerBase
    {
        private readonly ITestClientService _testClientService;

        public TestClientController(ITestClientService testClientService)
        {
            _testClientService = testClientService;
        }

        // User Request
        [HttpGet]
        [Route("UserRequestDictionary")]
        public async Task<IActionResult> GetUserRequestDictionary()
        {
            var userRequestDictionary = await _testClientService.GetUserRequestDictionary();
            return Ok(userRequestDictionary);
        }

        [HttpGet]
        [Route("UserRequest/{requestId}")]
        public async Task<IActionResult> GetUserRequest(string requestId)
        {
            var userRequest = await _testClientService.GetUserRequest(requestId);
            if (userRequest == null)
            {
                return NotFound();
            }
            return Ok(userRequest);
        }

        [HttpPost]
        [Route("UserRequest")]
        public async Task<IActionResult> AddUserRequest([FromBody] UserRequest userRequest)
        {
            await _testClientService.AddUserRequest(userRequest);
            return Ok();
        }

        // Proc Config
        [HttpGet]
        [Route("ProcConfigDictionary")]
        public async Task<IActionResult> GetProcConfigDictionary()
        {
            var procConfigDictionary = await _testClientService.GetProcConfigDictionary();
            return Ok(procConfigDictionary);
        }

        [HttpGet]
        [Route("ProcConfig/{requestId}")]
        public async Task<IActionResult> GetProcConfig(string requestId)
        {
            var procConfig = await _testClientService.GetProcConfig(requestId);
            if (procConfig == null)
            {
                return NotFound();
            }
            return Ok(procConfig);
        }

        [HttpPost]
        [Route("ProcConfig")]
        public async Task<IActionResult> AddProcConfig([FromBody] ProcConfig procConfig)
        {
            await _testClientService.AddProcConfig(procConfig);
            return Ok();
        }

        // Processing Results 
        [HttpGet]
        [Route("ProcessingResultsDictionary")]
        public async Task<IActionResult> GetProcessingResultsDictionary()
        {
            var processingResultsDictionary = await _testClientService.GetProcessingResultsDictionary();
            return Ok(processingResultsDictionary);
        }

        [HttpGet]
        [Route("ProcessingResults/{requestId}")]
        public async Task<IActionResult> GetProcessingResults(string requestId)
        {
            var processingResults = await _testClientService.GetProcessingResults(requestId);
            if (processingResults == null)
            {
                return NotFound();
            }
            return Ok(processingResults);
        }

        [HttpPost]
        [Route("ProcessingResults")]
        public async Task<IActionResult> AddProcessingResults([FromBody] ProcessingResults processingResults)
        {
            await _testClientService.AddProcessingResults(processingResults);
            return Ok();
        }
    }
}
