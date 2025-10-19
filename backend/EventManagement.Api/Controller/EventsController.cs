using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        /// <summary>
        /// Get all public events GET: api/events
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicEvents()
        {
            var events = await _eventService.GetPublicEventsAsync();
            return Ok(events);
        }

        /// <summary>
        /// Get event by ID // GET: api/events/5
        /// </summary>
        /// <param name="eventId">Event ID</param>
        [HttpGet("{eventId:int}")]
        [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventById(int eventId)
        {
            var ev = await _eventService.GetEventByIdAsync(eventId);

            return Ok(ev);
        }

        /// <summary>
        /// Create a new event
        /// </summary>
        /// <param name="dto">Event creation data</param>
        /// <param name="organizerId">Organizer user ID (from auth)</param>
        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateEvent(
            [FromBody] CreateEventDto dto, 
            [FromHeader] int organizerId)
        {
            _logger.LogInformation($"Creating new event for organizer {organizerId}");

            var eventId = await _eventService.CreateEventAsync(dto, organizerId);

            return Ok(new { id = eventId, message = "Event created successfully" });
        }

        /// <summary>
        /// Update an existing event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="dto">Event update data</param>
        /// <param name="userId">Current user ID (from auth)</param>
        [HttpPatch("{eventId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEvent(
            int eventId, 
            [FromBody] UpdateEventDto dto,
            [FromHeader] int userId)
        {
            _logger.LogInformation($"Updating event {eventId} by user {userId}");

            await _eventService.UpdateEventAsync(eventId, dto, userId);

            return Ok(new { message = "Event updated successfully" });
        }

        /// <summary>
        /// Delete an event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="userId">Current user ID (from auth)</param>
        [HttpDelete("{eventId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteEvent(
            int eventId, 
            [FromHeader] int userId)
        {
            _logger.LogInformation($"Deleting event with id {eventId} by user with userId {userId}");

            await _eventService.DeleteEventAsync(eventId, userId);

            return Ok(new { message = "Event deleted successfully" });
        }

        /// <summary>
        /// Join an event as participant
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="userId">User ID wanting to join</param>
        [HttpPost("{eventId:int}/join")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> JoinEvent(
            int eventId,
            [FromHeader] int userId)
        {
            _logger.LogInformation($"User with userId {userId} joining event with eventId {eventId}");

            await _eventService.JoinEventAsync(eventId, userId);

            return Ok(new { message = "Successfully joined the event " });
        }

        /// <summary>
        /// Leave an event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="userId">User ID wanting to leave</param>
        [HttpPost("{eventId:int}/leave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LeaveEvent(
            int eventId,
            [FromHeader] int userId)
        {
            _logger.LogInformation($"User {userId} leaving event {eventId}");

            await _eventService.LeaveEventAsync(eventId, userId);

            return Ok(new { message = "Successfully left the event" });
        }

        /// <summary>
        /// Get user's events (organized + participating)
        /// </summary>
        /// <param name="userId">User ID</param>
        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(typeof(IEnumerable<EventListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserEvents(int userId)
        {
            _logger.LogInformation($"Fetching events for user {userId}");

            var events = await _eventService.GetUserEventsAsync(userId);

            return Ok(events);
        }
    }
}
