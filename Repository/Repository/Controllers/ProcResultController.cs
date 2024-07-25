using CommonModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Repository.Interfaces;

namespace Repository.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcResultController : ControllerBase
    {
        private readonly IProcResultService _procResultService;

        public ProcResultController(IProcResultService procResultService)
        {
            _procResultService = procResultService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProcessingResults>> Get(string id)
        {
            var processingResults = await _procResultService.GetProcessingResults(id);
            if (processingResults == null)
            {
                return NotFound();
            }
            return Ok(processingResults);
        }

        [HttpPost]
        public async Task<ActionResult<ProcessingResults>> Post([FromBody] object jsonObject)
        {
            var processingResults = JsonConvert.DeserializeObject<ProcessingResults>(jsonObject.ToString());
            var returnCode = await _procResultService.AddProcessingResults(processingResults);
            if (returnCode != 0)
            {
                return BadRequest(processingResults);
            }
            return Ok(processingResults);
        }
    }
}


