using CommonModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace WebHookAbstraction
{
    public abstract class WebHookController : ControllerBase
    {
        public bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
            "SubscriptionValidation";

        public bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
            "Notification";

        public async Task<IActionResult> PostOrchestration(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
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

        public abstract Task<IActionResult> HandleGridEvents(string jsonContent);

        public (GridEvent<dynamic>?, string) GetValidEvent(string jsonContent, List<string> validEventTypes)
        {
            var gridEvent = GetEvent(jsonContent);
            if (gridEvent == null)
            {
                return (null, "No event found in the request.");
            }

            var eventType = gridEvent.EventType;
            if (!validEventTypes.Contains(eventType))
            {
                return (null, $"Invalid event type: {eventType}");
            }

            return (gridEvent, string.Empty);
        }

        private GridEvent<dynamic>? GetEvent(string jsonContent)
        {
            var events = JArray.Parse(jsonContent);
            if (events == null || events.Count == 0)
            {
                return null;
            }

            return events.First.ToObject<GridEvent<dynamic>>();
        }

    }
}
