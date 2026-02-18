using EventManagement.Application.DTOs.EventDtos;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Services;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.UnitTests.Services
{
    public class EventServiceTests
    {
        private Mock<IEventRepository> _eventRepositoryMock;
        private Mock<IParticipantRepository> _participantRepositoryMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ILogger<EventService>> _loggerMock;
        private readonly EventService _service;
        // Standard xUnit Setup
        public EventServiceTests()
        {
            // Init the Mocks
            _eventRepositoryMock = new Mock<IEventRepository>();
            _participantRepositoryMock = new Mock<IParticipantRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<EventService>>();

            // Inject the .Object property of the mocks into real service
            _service = new EventService(
                _eventRepositoryMock.Object,
                _participantRepositoryMock.Object,
                _userRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task JoinEventAsync_ShouldThrowBadRequest_WhenEventIsFull()
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
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(myEvent);

            // Set up participant repo to say there are already 2 people
            _participantRepositoryMock.Setup(r => r.GetCountByEventIdAsync(eventId))
                .ReturnsAsync(2);

            // Act
            // Calling the service and checking for the exception
            Func<Task> action = () => _service.JoinEventAsync(eventId, userId);

            // Assert
            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Event is full");
        }

        [Fact]
        public async Task JoinEventAsync_ShouldThrowConflictException_WhenUserAlreadyJoined()
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
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(myEvent);

            _participantRepositoryMock.Setup(r => r.GetByEventAndUserAsync(eventId, userId))
                .ReturnsAsync(new Participant
                {
                    EventId = eventId,
                    UserId = userId
                });

            Func<Task> action = () => _service.JoinEventAsync(eventId, userId);

            await action.Should().ThrowAsync<ConflictException>()
               .WithMessage("User already joined this event");

            _participantRepositoryMock.Verify(r =>
                r.AddAsync(It.IsAny<Participant>()),
                Times.Never());
        }

        [Fact]
        public async Task CreateEventAsync_EnsureTheDateTimeKindUtc_NormalizationImplementedIsActuallyHappening()
        {
            // Arrange
            var organizerId = 13;
            var dto = new CreateEventDto
            {
                Name = "Test Event",
                EventDate = new DateTime(2026, 5, 20, 10, 0, 0), // Unspecified Kind
                Location = "Remote"
            };

            // We mock the User check first so the service doesn't throw NotFound
            _userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId))
                .ReturnsAsync(new User { Id = organizerId });

            // Act
            var resultId = await _service.CreateEventAsync(dto, organizerId);

            // Assert
            // This is where you prove the UTC fix is working
            _eventRepositoryMock.Verify(r => r.AddAsync(It.Is<Event>(e =>
                e.Name == dto.Name &&
                e.EventDate.Kind == DateTimeKind.Utc && // The Key check
                e.OrganizerId == organizerId
            )), Times.Once);

            _eventRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
