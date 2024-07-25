using CommonModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcConfigBuilder.Interfaces;
using System.Text;

namespace ProcConfigBuilder.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcConfigBuilderController : ControllerBase
    {
        private readonly IProcConfigBuilderService _procConfigBuilderService;
        private readonly ILogger<ProcConfigBuilderController> _logger;

        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
            "SubscriptionValidation";

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "Notification";

        public ProcConfigBuilderController(IProcConfigBuilderService procConfigBuilderService, ILogger<ProcConfigBuilderController> logger)
        {
            _procConfigBuilderService = procConfigBuilderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();

                if (EventTypeSubcriptionValidation)
                {
                    return HandleValidation(jsonContent);
                }
                else if (EventTypeNotification)
                {
                    if (IsCloudEvent(jsonContent))
                    {
                        return await HandleCloudEvent(jsonContent);
                    }

                    return await HandleGridEvents(jsonContent);
                }

                _logger.LogError($"Unexpected event type: {HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault()}");
                return BadRequest();
            }
        }

        private static JsonResult HandleValidation(string jsonContent)
        {
            var gridEvent =
                JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                    .First();

            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private static bool IsCloudEvent(string jsonContent)
        {
            // Cloud events are sent one at a time, while Grid events
            // are sent in an array. As a result, the JObject.Parse will 
            // fail for Grid events. 
            try
            {
                // Attempt to read one JSON object. 
                var eventData = JObject.Parse(jsonContent);

                // Check for the spec version property.
                var version = eventData["specversion"].Value<string>();
                if (!string.IsNullOrEmpty(version)) return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        private async Task<IActionResult> HandleCloudEvent(string jsonContent)
        {
            var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);
            var eventData = JObject.Parse(jsonContent);

            // Not implemented in this example.

            return Ok();
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            var events = JArray.Parse(jsonContent);
            var gridEvent = events?.First?.ToObject<GridEvent<dynamic>>();

            if ((events.Count == 0)
                || (gridEvent == null)
                || !IsAcceptableEventType(gridEvent))
            {
                _logger.LogError("Invalid event data.");
                return BadRequest();
            }

            bool success = await _procConfigBuilderService.CreateAndPublishProcConfigFile(events);
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

        private bool IsAcceptableEventType(GridEvent<dynamic> gridEvent)
        {
            var type = gridEvent.EventType;
            if (type == "UserRequestEvent")
            {
                _logger.LogError($"Unacceptable event type: {type}");
                return false;
            }
            return true;
        }

    }
}
