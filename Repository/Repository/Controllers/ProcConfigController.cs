using CommonModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Repository.Interfaces;

namespace Repository.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcConfigController : ControllerBase
    {
        private readonly IProcConfigService _procConfigService;

        public ProcConfigController(IProcConfigService procConfigService)
        {
            _procConfigService = procConfigService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProcConfig>> Get(string id)
        {
            var procConfig = await _procConfigService.GetProcConfig(id);
            if (procConfig == null)
            {
                return NotFound();
            }
            return Ok(procConfig);
        }

        [HttpPost]
        public async Task<ActionResult<ProcConfig>> Post([FromBody] Object jsonObject)
        {
            var procConfig = JsonConvert.DeserializeObject<ProcConfig>(jsonObject.ToString());
            var returnCode = await _procConfigService.AddProcConfig(procConfig);
            if (returnCode != 0)
            {
                return BadRequest(procConfig);
            }
            return Ok(procConfig);
        }
    }
}


