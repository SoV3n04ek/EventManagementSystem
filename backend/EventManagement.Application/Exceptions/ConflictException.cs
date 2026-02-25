namespace EventManagement.Application.Exceptions;

public class ConflictException(string message) : Exception(message)
{
}
