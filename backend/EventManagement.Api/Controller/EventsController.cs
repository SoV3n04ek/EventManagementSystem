using System.Security.Claims;
using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{
    readonly IEventService eventService = eventService;
    readonly ILogger<EventsController> logger = logger;

    /// <summary>
    /// Get all public events GET: api/events
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicEvents()
    {
        int? currentUserId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : null;
        var events = await eventService.GetPublicEventsAsync(currentUserId);
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
        int? currentUserId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : null;
        var ev = await eventService.GetEventByIdAsync(eventId, currentUserId);

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
        int userId = GetCurrentUserId();

        // set default date range if not provided
        var start = startDate ?? DateTime.UtcNow.Date;
        var end = endDate ?? start.AddMonths(1); // default one month range

        logger.LogInformation("Fetching calendar events for user {UserId} from {Start} to {End}", userId, start, end);

        var calendar = await eventService.GetUserCalendarAsync(userId, start, end, viewType);

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

        int organizerId = GetCurrentUserId();
        logger.LogInformation("Creating new event for organizer {OrganizerId}", organizerId);

        try
        {
            int eventId = await eventService.CreateEventAsync(dto, organizerId);

            await eventService.JoinEventAsync(eventId, organizerId);

            return Ok(new { id = eventId, message = "Event created successfully" });
        }
        catch (BadRequestException ex)
        {
            logger.LogWarning("Create event validation failed: {Message}", ex.Message);
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
            logger.LogWarning("Model validation failed:");
            foreach (var error in ModelState)
            {
                foreach (var err in error.Value.Errors)
                {
                    logger.LogWarning("Field {Field}, Error: {ErrorMessage}", error.Key, err.ErrorMessage);
                }
            }
            return BadRequest(ModelState);
        }

        int userId = GetCurrentUserId();

        logger.LogInformation("Updating event {EventId} by user {UserId}", eventId, userId);

        await eventService.UpdateEventAsync(eventId, dto, userId);

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
        int userId = GetCurrentUserId();
        logger.LogInformation("Deleting event with id {EventId} by user with userId {UserId}", eventId, userId);

        await eventService.DeleteEventAsync(eventId, userId);

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
        int userId = GetCurrentUserId();
        logger.LogInformation("User with userId {UserId} joining event with eventId {EventId}", userId, eventId);

        await eventService.JoinEventAsync(eventId, userId);

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
        int userId = GetCurrentUserId();
        logger.LogInformation("User {UserId} leaving event {EventId}", userId, eventId);

        await eventService.LeaveEventAsync(eventId, userId);

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
        int userId = GetCurrentUserId();
        logger.LogInformation("Fetching events for user {UserId}", userId);

        var events = await eventService.GetUserEventsAsync(userId);

        return Ok(events);
    }

    // 

    /// <summary>
    /// Helper method to get current user ID from JWT token
    /// </summary>
    int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)
            ? throw new UnauthorizedAccessException("User ID not found in token")
            : userId;
    }
}
