using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Data
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly EventManagementDbContext _context;

        public DatabaseSeeder(EventManagementDbContext context) =>
            _context = context;

        public async Task SeedAsync()
        {
            await SeedUsersAsync();
            await _context.SaveChangesAsync();
            await SeedEventsAsync();
            await _context.SaveChangesAsync();
            await SeedParticipantsAsync();
            await _context.SaveChangesAsync();
            await ResetSequences();
        }

        private async Task SeedParticipantsAsync()
        {
            if (await _context.Participants.AnyAsync()) return;

            var participants = new List<Participant>();

            var events = await _context.Events.ToListAsync();

            foreach (Event eve in events) {
                participants.Add(new Participant
                {
                    EventId = eve.Id,
                    UserId = eve.OrganizerId
                });
            }
            await _context.Participants.AddRangeAsync(participants);
        }

        private async Task SeedUsersAsync()
        {
            var tableExists = await _context.Database.ExecuteSqlRawAsync(@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'users'
                )");

            if (tableExists == 0)
            {
                return; 
            }

            if (await _context.Users.AnyAsync())
            {
                return;
            }

            var users = new List<User>
            {
                new User { Name = "John Doe", Email = "john@email.com", PasswordHash = "temp1" },
                new User { Name = "Jane Smith", Email = "jane.smith@email.com", PasswordHash = "temp2" },
                new User { Name = "Mike Johnson", Email = "mike.johnson@email.com", PasswordHash = "temp3" },
                new User { Name = "Sarah Wilson", Email = "sarah.wilson@email.com", PasswordHash = "temp4" },
                new User { Name = "David Brown", Email = "david.brown@email.com", PasswordHash = "temp5" },
                new User { Name = "Lisa Davis", Email = "lisa.davis@email.com", PasswordHash = "temp6" },
                new User { Name = "Robert Miller", Email = "robert.miller@email.com", PasswordHash = "temp7" },
                new User { Name = "Emily Taylor", Email = "emily.taylor@email.com", PasswordHash = "temp8" },
                new User { Name = "Thomas Anderson", Email = "thomas.anderson@email.com", PasswordHash = "temp9" },
                new User { Name = "Jennifer Martinez", Email = "jennifer.martinez@email.com", PasswordHash = "temp10" },
                new User { Name = "Christopher Lee", Email = "christopher.lee@email.com", PasswordHash = "temp11" },
                new User { Name = "Amanda White", Email = "amanda.white@email.com", PasswordHash = "temp12" },
                new User { Name = "Kevin Harris", Email = "kevin.harris@email.com", PasswordHash = "temp13" },
                new User { Name = "Michelle Clark", Email = "michelle.clark@email.com", PasswordHash = "temp14" },
                new User { Name = "Jason Lewis", Email = "jason.lewis@email.com", PasswordHash = "temp15" },
                new User { Name = "Stephanie Walker", Email = "stephanie.walker@email.com", PasswordHash = "temp16" },
                new User { Name = "Brian Hall", Email = "brian.hall@email.com", PasswordHash = "temp17" },
                new User { Name = "Rebecca Allen", Email = "rebecca.allen@email.com", PasswordHash = "temp18" },
                new User { Name = "Daniel Young", Email = "daniel.young@email.com", PasswordHash = "temp19" },
                new User { Name = "Nicole King", Email = "nicole.king@email.com", PasswordHash = "temp20" }
            };


            await _context.Users.AddRangeAsync(users);
        }

        private async Task SeedEventsAsync()
        {
            if (await _context.Events.AnyAsync()) { return; }

            var events = new List<Event>
            {
                new Event
                {
                    Name = "Tech Conference 2024",
                    Description = "Annual technology conference featuring latest innovations",
                    EventDate = DateTime.UtcNow.AddDays(5),
                    Location = "Convention Center",
                    Capacity = 300,
                    OrganizerId = 1,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Summer Music Festival",
                    Description = "Outdoor music festival with multiple stages",
                    EventDate = DateTime.UtcNow.AddDays(15),
                    Location = "Central Park",
                    Capacity = 5000,
                    OrganizerId = 2,
                    IsPublic = true
                },
                new Event
                {
                    Name = ".NET Developer Workshop",
                    Description = "Hands-on workshop for .NET developers",
                    EventDate = DateTime.UtcNow.AddDays(3),
                    Location = "Tech Hub",
                    Capacity = 50,
                    OrganizerId = 3,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Startup Pitch Competition",
                    Description = "Early-stage startups pitch to investors",
                    EventDate = DateTime.UtcNow.AddDays(25),
                    Location = "Innovation Center",
                    Capacity = 200,
                    OrganizerId = 4,
                    IsPublic = true
                },
                new Event
                {
                    Name = "AI & Machine Learning Summit",
                    Description = "Conference on artificial intelligence trends",
                    EventDate = DateTime.UtcNow.AddDays(40),
                    Location = "Science Museum",
                    Capacity = 400,
                    OrganizerId = 5,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Yoga & Wellness Retreat",
                    Description = "Weekend wellness and meditation retreat",
                    EventDate = DateTime.UtcNow.AddDays(12),
                    Location = "Mountain Resort",
                    Capacity = 30,
                    OrganizerId = 6,
                    IsPublic = false
                },
                new Event
                {
                    Name = "Blockchain Fundamentals",
                    Description = "Introduction to blockchain technology",
                    EventDate = DateTime.UtcNow.AddDays(8),
                    Location = "Business School",
                    Capacity = 100,
                    OrganizerId = 7,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Food & Wine Tasting",
                    Description = "Gourmet food and wine pairing event",
                    EventDate = DateTime.UtcNow.AddDays(18),
                    Location = "Vineyard Estate",
                    Capacity = 80,
                    OrganizerId = 8,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Mobile App Development",
                    Description = "Workshop on cross-platform mobile development",
                    EventDate = DateTime.UtcNow.AddDays(22),
                    Location = "Developer Space",
                    Capacity = 60,
                    OrganizerId = 9,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Photography Masterclass",
                    Description = "Advanced photography techniques workshop",
                    EventDate = DateTime.UtcNow.AddDays(30),
                    Location = "Art Gallery",
                    Capacity = 25,
                    OrganizerId = 10,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Cybersecurity Conference",
                    Description = "Latest trends in cybersecurity and threats",
                    EventDate = DateTime.UtcNow.AddDays(35),
                    Location = "Security Institute",
                    Capacity = 250,
                    OrganizerId = 11,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Book Club Meeting",
                    Description = "Monthly book discussion group",
                    EventDate = DateTime.UtcNow.AddDays(7),
                    Location = "Public Library",
                    Capacity = 20,
                    OrganizerId = 12,
                    IsPublic = false
                },
                new Event
                {
                    Name = "Hackathon 2024",
                    Description = "48-hour coding competition",
                    EventDate = DateTime.UtcNow.AddDays(45),
                    Location = "University Campus",
                    Capacity = 150,
                    OrganizerId = 13,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Digital Marketing Workshop",
                    Description = "Strategies for online marketing success",
                    EventDate = DateTime.UtcNow.AddDays(20),
                    Location = "Marketing Agency",
                    Capacity = 75,
                    OrganizerId = 14,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Charity Gala Dinner",
                    Description = "Fundraising event for local charity",
                    EventDate = DateTime.UtcNow.AddDays(28),
                    Location = "Grand Hotel",
                    Capacity = 120,
                    OrganizerId = 15,
                    IsPublic = true
                },
                new Event
                {
                    Name = "UI/UX Design Conference",
                    Description = "User interface and experience design trends",
                    EventDate = DateTime.UtcNow.AddDays(33),
                    Location = "Design Studio",
                    Capacity = 180,
                    OrganizerId = 16,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Running Marathon",
                    Description = "Annual city marathon race",
                    EventDate = DateTime.UtcNow.AddDays(50),
                    Location = "City Center",
                    Capacity = 1000,
                    OrganizerId = 17,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Cooking Class: Italian Cuisine",
                    Description = "Learn to cook authentic Italian dishes",
                    EventDate = DateTime.UtcNow.AddDays(14),
                    Location = "Culinary School",
                    Capacity = 15,
                    OrganizerId = 18,
                    IsPublic = true
                },
                new Event
                {
                    Name = "VR/AR Development Workshop",
                    Description = "Creating virtual and augmented reality experiences",
                    EventDate = DateTime.UtcNow.AddDays(27),
                    Location = "Tech Lab",
                    Capacity = 40,
                    OrganizerId = 19,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Investor Networking Event",
                    Description = "Connect with angel investors and VCs",
                    EventDate = DateTime.UtcNow.AddDays(10),
                    Location = "Business Club",
                    Capacity = 90,
                    OrganizerId = 20,
                    IsPublic = false
                },
                new Event
                {
                    Name = "Python Data Science Bootcamp",
                    Description = "Intensive data science training with Python",
                    EventDate = DateTime.UtcNow.AddDays(37),
                    Location = "Data Institute",
                    Capacity = 70,
                    OrganizerId = 1,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Art Exhibition Opening",
                    Description = "Contemporary art exhibition opening night",
                    EventDate = DateTime.UtcNow.AddDays(6),
                    Location = "Modern Art Museum",
                    Capacity = 200,
                    OrganizerId = 2,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Leadership Summit",
                    Description = "Executive leadership development conference",
                    EventDate = DateTime.UtcNow.AddDays(42),
                    Location = "Conference Center",
                    Capacity = 300,
                    OrganizerId = 3,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Game Development Workshop",
                    Description = "Create your first video game",
                    EventDate = DateTime.UtcNow.AddDays(17),
                    Location = "Game Studio",
                    Capacity = 35,
                    OrganizerId = 4,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Sustainable Living Expo",
                    Description = "Eco-friendly products and practices",
                    EventDate = DateTime.UtcNow.AddDays(55),
                    Location = "Exhibition Hall",
                    Capacity = 800,
                    OrganizerId = 5,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Public Speaking Masterclass",
                    Description = "Overcome fear and master public speaking",
                    EventDate = DateTime.UtcNow.AddDays(23),
                    Location = "Communication Center",
                    Capacity = 25,
                    OrganizerId = 6,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Cloud Computing Conference",
                    Description = "Latest in cloud technologies and migration",
                    EventDate = DateTime.UtcNow.AddDays(48),
                    Location = "Tech Park",
                    Capacity = 350,
                    OrganizerId = 7,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Wine Making Workshop",
                    Description = "Learn the art of wine making",
                    EventDate = DateTime.UtcNow.AddDays(32),
                    Location = "Winery",
                    Capacity = 20,
                    OrganizerId = 8,
                    IsPublic = false
                },
                new Event
                {
                    Name = "Financial Planning Seminar",
                    Description = "Personal finance and investment strategies",
                    EventDate = DateTime.UtcNow.AddDays(19),
                    Location = "Financial District",
                    Capacity = 60,
                    OrganizerId = 9,
                    IsPublic = true
                },
                new Event
                {
                    Name = "Robotics Competition",
                    Description = "High school robotics team competition",
                    EventDate = DateTime.UtcNow.AddDays(60),
                    Location = "Science Center",
                    Capacity = 500,
                    OrganizerId = 10,
                    IsPublic = true
                }
            };

            await _context.Events.AddRangeAsync(events);
        }

        public async Task ResetSequences()
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT setval('\"events_Id_seq\"', (SELECT MAX(\"Id\") FROM events))");
            await _context.Database.ExecuteSqlRawAsync("SELECT setval('\"users_Id_seq\"', (SELECT MAX(\"Id\") FROM users))");
        }
    }
}
