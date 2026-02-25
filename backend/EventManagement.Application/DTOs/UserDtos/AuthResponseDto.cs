using System.Text.Json.Serialization;

namespace EventManagement.Application.DTOs.UserDtos;

public class AuthResponseDto
{
    /// <summary>
    /// JWT token — only used server-side to set the HttpOnly cookie.
    /// Never serialized to JSON responses (prevents XSS token theft).
    /// </summary>
    [JsonIgnore]
    public string Token { get; set; } = string.Empty;

    public UserDto User { get; set; } = null!;
}
