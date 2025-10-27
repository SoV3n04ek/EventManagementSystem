using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        [AllowAnonymous]
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
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventById(int eventId)
        {
            var ev = await _eventService.GetEventByIdAsync(eventId);

            return Ok(ev);
        }

        /// <summary>
        /// Get user's events in calendar format
        /// </summary>
        [HttpGet("user/me/calendar")]
        [Authorize]
        [ProducesResponseType(typeof(CalendarViewDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyCalendarEvents(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string viewType = "month")
        {
            var userId = GetCurrentUserId();

            // set default date range if not provided
            var start = startDate ?? DateTime.UtcNow.Date;
            var end = endDate ?? start.AddMonths(1); // default one month range

            _logger.LogInformation($"Fetching calendar events for user {userId} from {start} to {end}");

            var calendar = await _eventService.GetUserCalendarAsync(userId, start, end, viewType);

            return Ok(calendar);
        }

        /// <summary>
        /// Create a new event
        /// </summary>
        /// <param name="dto">Event creation data</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        k => k.Key,
                        v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            var organizerId = GetCurrentUserId();
            _logger.LogInformation($"Creating new event for organizer {organizerId}");

            try
            {
                var eventId = await _eventService.CreateEventAsync(dto, organizerId);

                await _eventService.JoinEventAsync(eventId, organizerId);

                return Ok(new { id = eventId, message = "Event created successfully" });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning($"Create event validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="dto">Event update data</param>
        [HttpPatch("{eventId:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEvent(
            int eventId, 
            [FromBody] UpdateEventDto dto)
        {
            // todo: FOR DEBUG ONLY
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed:");
                foreach(var error in ModelState)
                {
                    foreach(var err in error.Value.Errors)
                    {
                        _logger.LogWarning($"Field {error.Key}, Error: {err.ErrorMessage}");
                    }
                }
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();

            _logger.LogInformation($"Updating event {eventId} by user {userId}");

            await _eventService.UpdateEventAsync(eventId, dto, userId);

            return Ok(new { message = "Event updated successfully" });
        }

        /// <summary>
        /// Delete an event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        [HttpDelete("{eventId:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation($"Deleting event with id {eventId} by user with userId {userId}");

            await _eventService.DeleteEventAsync(eventId, userId);

            return Ok(new { message = "Event deleted successfully" });
        }

        /// <summary>
        /// Join an event as participant
        /// </summary>
        /// <param name="eventId">Event ID</param>
        [HttpPost("{eventId:int}/join")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> JoinEvent(int eventId)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User with userId {userId} joining event with eventId {eventId}");

            await _eventService.JoinEventAsync(eventId, userId);

            return Ok(new { message = "Successfully joined the event " });
        }

        /// <summary>
        /// Leave an event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        [HttpPost("{eventId:int}/leave")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LeaveEvent(int eventId)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User {userId} leaving event {eventId}");

            await _eventService.LeaveEventAsync(eventId, userId);

            return Ok(new { message = "Successfully left the event" });
        }

        /// <summary>
        /// Get current user's events (organized + participating)
        /// </summary>
        [HttpGet("user/me")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<EventListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation($"Fetching events for user {userId}");

            var events = await _eventService.GetUserEventsAsync(userId);

            return Ok(events);
        }

        // 

        /// <summary>
        /// Helper method to get current user ID from JWT token
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }
    }
}
