using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using BC = BCrypt.Net.BCrypt;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly TelemetryDbContext _context;
    private readonly IMailService _mailService;

    public AuthController(IConfiguration config, TelemetryDbContext context, IMailService mailService)
    {
        _config = config;
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mailService = mailService;
    }

    [HttpPost("register")]
    [EndpointSummary("Registers a new user.")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> Register([FromBody] RegisterRequest req)
    {
        var normalizedEmail = req.Email.Trim().ToLower();
        var normalizedUsername = req.Username.Trim().ToLower();

        // Check if user exists
        var userExists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail || u.Username == normalizedUsername);

        if (userExists)
        {
            return Conflict(new OperationStatusResponse(
                false,
                "Username or Email is already in use."
            ));
        }

        var passwordHash = BC.HashPassword(req.Password);
        var emailVerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var newUser = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            IsEmailVerified = false
        };

        var newCredential = new UserCredential
        {
            PasswordHash = passwordHash,
            VerifyToken = emailVerificationToken,
            VerifyTokenExpiration = DateTime.UtcNow.AddHours(2)
        };

        newUser.UserCredential = newCredential;

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // SETUP EMAIL SERVICE TO EMAIL Verification token
        var mailSent = await _mailService.SendVerificationEmailAsync(newUser.Email, newUser.Username, emailVerificationToken);

        return CreatedAtAction(
            nameof(Register),
            new { id = newUser.Id },
            new RegistrationResponse
            {
                UserId = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Message = mailSent
                    ? "Registration successful! Please check your inbox to verify your email before logging in."
                    : "Registration successful, but we encountered an error sending the verification email. Please try resending the verification link."
            }
        );
    }

    [HttpPost("login")]
    [EndpointSummary("Logs user in by prodiving a jwt.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var normalizedEmail = req.Email.Trim().ToLower();
        const string invalidCredentialsMsg = "Invalid credentials provided.";

        // Check if user and credentials exist
        var user = await _context.Users
                    .Include(u => u.UserCredential)
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || user.UserCredential == null)
        {
            return Unauthorized(new OperationStatusResponse(
                false,
                invalidCredentialsMsg
            ));
        }

        // Check if password is valid
        var isPasswordValid = BC.Verify(req.Password, user.UserCredential.PasswordHash);
        if (!isPasswordValid)
        {
            return Unauthorized(new OperationStatusResponse(
                false,
                invalidCredentialsMsg
            ));
        }

        // Check if user is verified
        if (!user.IsEmailVerified)
        {
            return BadRequest(new OperationStatusResponse(
                false,
                "Account is unverified. Please check your inbox for the activation link."
            ));
        }

        // Validation fully passed => Generate mint token
        var token = GenerateJwtToken(user);
        var authResponse = new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        };

        return Ok(authResponse);
    }

    [HttpPost("verify")]
    [EndpointSummary("Verifies a user's email address using an activation token.")]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationStatusResponse>> Verify([FromBody] VerifyRequest req)
    {
        // check if a UserCredential exists with given token
        var normalizedEmail = req.Email.Trim().ToLower();
        var lookupToken = req.Token.Trim().ToUpper();
        var credential = await _context.UserCredentials
                    .Include(uc => uc.User)
                    .FirstOrDefaultAsync(uc => uc.VerifyToken == lookupToken);

        // Handle no credential or no user or email and token not matched
        if (credential == null || credential.User == null || credential.User.Email != normalizedEmail)
        {
            return BadRequest(new OperationStatusResponse(
                false,
                "The verification token or email address provided is invalid."
            ));
        }

        // Handle user email already verified
        if (credential.User.IsEmailVerified)
        {
            return Ok(new OperationStatusResponse(
                true,
                "Your email address has already been verified! You can proceed to log in."
            ));
        }

        // Handle expired token
        if (DateTime.UtcNow > credential.VerifyTokenExpiration)
        {
            return BadRequest(new OperationStatusResponse(
                false,
                "This verification token has expired. Please request a new activation link."
            ));
        }

        // Update data to reflect successful verification
        credential.User.IsEmailVerified = true;
        credential.VerifyToken = null;
        credential.VerifyTokenExpiration = null;

        await _context.SaveChangesAsync();
        return Ok(new OperationStatusResponse(
            true,
            "Your email address has been successfully verified! You can now log in to the application."
        ));

    }

    [HttpPost("resend-verification")]
    [EndpointSummary("Send a new verification link to the user.")]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OperationStatusResponse>> ResendVerification([FromBody] ResendVerifyRequest req)
    {
        var normalizedEmail = req.Email.Trim().ToLower();
        var user = await _context.Users
                .Include(u => u.UserCredential)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Handle user does not exist or already verified (Security: Prevent Email Enumeration)
        if (user == null || user.UserCredential == null || user.IsEmailVerified)
        {
            // Optionally add await Task.Delay(100) here to deter timing attacks
            return Ok(new OperationStatusResponse(
                true,
                "If an unverified account with that email address exists, a new verification link has been sent."
            ));
        }

        // Generate new token and send
        var emailVerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.UserCredential.VerifyToken = emailVerificationToken;
        user.UserCredential.VerifyTokenExpiration = DateTime.UtcNow.AddHours(2);
        await _context.SaveChangesAsync();

        // Email new verification link
        var mailSent = await _mailService.SendVerificationEmailAsync(user.Email, user.Username, emailVerificationToken);

        if (!mailSent)
        {
            return StatusCode(500, new OperationStatusResponse(
                false,
                "Failed to send the verification email. Please try again later."
            ));
        }

        return Ok(new OperationStatusResponse(
            true,
            "If an unverified account with that email address exists, a new verification link has been sent."
        ));
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT_SECRET_KEY"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["JWT_ISSUER"],
            audience: _config["JWT_AUDIENCE"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}