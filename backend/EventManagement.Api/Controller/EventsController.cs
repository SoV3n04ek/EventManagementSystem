using EventManagement.Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;

        public EventsController(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        // Get: api/events
        [HttpGet]
        public async Task<IActionResult> GetPublicEvents()
        {
            var events = await _eventRepository.GetPublicEventsAsync();
            return Ok(events);
        }

        // Get: api/events/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found " });
            }

            return Ok(ev);
        }
    }
}
