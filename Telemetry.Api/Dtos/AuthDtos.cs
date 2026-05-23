using System.ComponentModel.DataAnnotations;

// INPUT CONTRACTS

public record RegisterRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [MaxLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public required string Password { get; init; }
}

public record LoginRequest
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [MaxLength(254)]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required.")]
    public required string Password { get; init; }
}


// OUTPUT CONTRACTS (Responses)

public record RegistrationResponse
{
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Message { get; init; }
}

public record AuthResponse
{
    public required string Token { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
}