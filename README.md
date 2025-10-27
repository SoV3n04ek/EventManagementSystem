```markdown
# Event Management System

A full-stack event management application built with Angular frontend and ASP.NET Core Web API backend.

## Features

- ✅ **User Authentication** - JWT-based registration and login
- ✅ **Event Management** - Create, edit, delete events
- ✅ **Public Events** - Browse and join public events
- ✅ **Event Participation** - Join/leave events with capacity tracking
- ✅ **Calendar View** - Monthly/weekly view of user's events
- ✅ **Responsive Design** - Works on desktop and mobile

## Tech Stack

**Frontend:** Angular, TypeScript, Tailwind CSS  
**Backend:** ASP.NET Core Web API, C#  
**Database:** PostgreSQL with Entity Framework Core  
**Authentication:** JWT Tokens  
**Containerization:** Docker & Docker Compose  

## Quick Start

### Prerequisites
- Docker
- Docker Compose

### Installation & Launch

1. **Clone the repository**
   ```bash
   git clone git@github.com:SoV3n04ek/EventManagementSystem.git
   cd EventManagementSystem
   ```

2. **Launch the application**
   ```bash
   docker-compose up
   ```
   *Wait for all containers to start (this may take 1-2 minutes)*

3. **Access the application**
   - Frontend: http://localhost:4200
   - Backend API: http://localhost:5000  
   - Swagger Documentation: http://localhost:5000/swagger
   - Database: PostgreSQL on localhost:5432

## API Documentation

Full API documentation available at: http://localhost:5000/swagger

| Method | Endpoint | Description |
|--------|-----------|-------------|
| POST | `/auth/register` | User registration |
| POST | `/auth/login` | User login |
| GET | `/events` | Get public events |
| POST | `/events` | Create new event |
| GET | `/users/me/events` | Get user's events |

## Support

If you encounter any issues:
1. Ensure Docker Desktop is running
2. Check that ports 4200 and 5000 are available
3. Restart with `docker-compose down && docker-compose up`
```