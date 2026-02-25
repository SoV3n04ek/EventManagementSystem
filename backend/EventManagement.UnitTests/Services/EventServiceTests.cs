using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.UnitTests.Services;

public class EventServiceTests
{
    readonly Mock<IEventRepository> eventRepositoryMock;
    readonly Mock<IParticipantRepository> participantRepositoryMock;
    readonly Mock<IUserRepository> userRepositoryMock;
    readonly Mock<ILogger<EventService>> loggerMock;
    readonly EventService service;
    // Standard xUnit Setup
    public EventServiceTests()
    {
        // Init the Mocks
        eventRepositoryMock = new Mock<IEventRepository>();
        participantRepositoryMock = new Mock<IParticipantRepository>();
        userRepositoryMock = new Mock<IUserRepository>();
        loggerMock = new Mock<ILogger<EventService>>();

        // Inject the .Object property of the mocks into real service
        service = new EventService(
            eventRepositoryMock.Object,
            participantRepositoryMock.Object,
            userRepositoryMock.Object,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task JoinEventAsyncShouldThrowBadRequestWhenEventIsFull()
    {
        // Arrange
        int eventId = 1;
        int userId = 99;
        var myEvent = new Event
        {
            Id = eventId,
            Capacity = 2,
            EventDate = DateTime.Today.AddDays(1),
            Location = "Location",
            IsPublic = true,
        };

        // Set up repository to return the event
        _ = eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync(myEvent);

        // Set up participant repo to say there are already 2 people
        _ = participantRepositoryMock.Setup(r => r.GetCountByEventIdAsync(eventId))
            .ReturnsAsync(2);

        // Act
        // Calling the service and checking for the exception
        Func<Task> action = () => service.JoinEventAsync(eventId, userId);

        // Assert
        _ = await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Event is full");
    }

    [Fact]
    public async Task JoinEventAsyncShouldThrowConflictExceptionWhenUserAlreadyJoined()
    {
        // Arrange
        int eventId = 1;
        int userId = 99;
        var myEvent = new Event
        {
            Id = eventId,
            Capacity = 2,
            EventDate = DateTime.Today.AddDays(1),
            Location = "Location",
            IsPublic = true,
        };

        // Set up repository to return the event
        _ = eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync(myEvent);

        _ = participantRepositoryMock.Setup(r => r.GetByEventAndUserAsync(eventId, userId))
            .ReturnsAsync(new Participant
            {
                EventId = eventId,
                UserId = userId
            });

        Func<Task> action = () => service.JoinEventAsync(eventId, userId);

        _ = await action.Should().ThrowAsync<ConflictException>()
           .WithMessage("User already joined this event");

        participantRepositoryMock.Verify(r =>
            r.AddAsync(It.IsAny<Participant>()),
            Times.Never());
    }

    [Fact]
    public async Task CreateEventAsyncEnsureTheDateTimeKindUtcNormalizationImplementedIsActuallyHappening()
    {
        // Arrange
        int organizerId = 13;
        var dto = new CreateEventDto
        {
            Name = "Test Event",
            EventDate = new DateTime(2026, 5, 20, 10, 0, 0), // Unspecified Kind
            Location = "Remote"
        };

        // We mock the User check first so the service doesn't throw NotFound
        _ = userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId))
            .ReturnsAsync(new User { Id = organizerId });

        // Act
        int resultId = await service.CreateEventAsync(dto, organizerId);

        // Assert
        // This is where you prove the UTC fix is working
        eventRepositoryMock.Verify(r => r.AddAsync(It.Is<Event>(e =>
            e.Name == dto.Name &&
            e.EventDate.Kind == DateTimeKind.Utc && // The Key check
            e.OrganizerId == organizerId
        )), Times.Once);

        eventRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
