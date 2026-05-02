using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly IPhoneOtpService _phoneOtpService;
    private readonly IActivityTrackingService _activityTracker;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IOtpService otpService, IPhoneOtpService phoneOtpService, IActivityTrackingService activityTracker, ILogger<AuthController> logger)
    {
        _authService = authService;
        _otpService = otpService;
        _phoneOtpService = phoneOtpService;
        _activityTracker = activityTracker;
        _logger = logger;
    }

    /// <summary>
    /// Check if a username exists
    /// </summary>
    [HttpGet("check-username/{username}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> CheckUsername(string username)
    {
        var exists = await _authService.UsernameExistsAsync(username);
        return Ok(new { exists });
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("POST /auth/login for user: {Username}", request.Username);

        if (!ModelState.IsValid)
        {
            return BadRequest(new LoginResponse
            {
                Success = false,
                Error = "Invalid request. Please provide username and password."
            });
        }

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            await _activityTracker.TrackAsync(new ActivityEvent
            {
                EventType = "Login",
                Username = request.Username,
                IsSuccess = false,
                Details = result.Error ?? "Invalid credentials"
            });
            return Unauthorized(result);
        }

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "Login",
            Username = request.Username,
            IsSuccess = true
        });

        return Ok(result);
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), 200)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(ApiResponse<UserInfo>.Fail("Not authenticated"));

        var user = await _authService.GetUserAsync(username);
        if (user == null)
            return NotFound(ApiResponse<UserInfo>.Fail("User not found"));

        return Ok(ApiResponse<UserInfo>.Ok(user));
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), 200)]
    [ProducesResponseType(typeof(RegisterResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("POST /auth/register for user: {Username}", request.Username);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new RegisterResponse
            {
                Success = false,
                Error = string.Join("; ", errors)
            });
        }

        // Check if email has been verified via OTP
        var isEmailVerified = await _otpService.IsEmailVerifiedAsync(request.Email);
        if (!isEmailVerified)
        {
            return BadRequest(new RegisterResponse
            {
                Success = false,
                Error = "Email must be verified with OTP before registration"
            });
        }

        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            await _activityTracker.TrackAsync(new ActivityEvent
            {
                EventType = "Signup",
                Username = request.Username,
                Email = request.Email,
                IsSuccess = false,
                Details = result.Error ?? "Registration failed"
            });
            return BadRequest(result);
        }

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "Signup",
            Username = request.Username,
            Email = request.Email,
            IsSuccess = true
        });

        return Ok(result);
    }

    /// <summary>
    /// Refresh JWT token (requires a valid or recently expired token)
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("POST /auth/refresh for user: {Username}", request.Username);

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new LoginResponse { Success = false, Error = "Username is required" });
        }

        var result = await _authService.RefreshTokenAsync(request.Username);

        if (!result.Success)
        {
            await _activityTracker.TrackAsync(new ActivityEvent
            {
                EventType = "TokenRefresh",
                Username = request.Username,
                IsSuccess = false,
                Details = result.Error
            });
            return Unauthorized(result);
        }

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "TokenRefresh",
            Username = request.Username,
            IsSuccess = true
        });

        return Ok(result);
    }

    /// <summary>
    /// Send OTP to email for verification
    /// </summary>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SendOtpResponse), 200)]
    [ProducesResponseType(typeof(SendOtpResponse), 400)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        _logger.LogInformation("POST /auth/send-otp for email: {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            return BadRequest(new SendOtpResponse
            {
                Success = false,
                Error = "Invalid email address"
            });
        }

        var result = await _otpService.SendOtpAsync(request.Email);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "OtpSent",
            Email = request.Email,
            IsSuccess = result.Success,
            Details = result.Success ? null : result.Error
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Verify OTP code sent to email
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyOtpResponse), 200)]
    [ProducesResponseType(typeof(VerifyOtpResponse), 400)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        _logger.LogInformation("POST /auth/verify-otp for email: {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new VerifyOtpResponse
            {
                Success = false,
                Error = string.Join("; ", errors)
            });
        }

        var result = await _otpService.VerifyOtpAsync(request.Email, request.Code);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "OtpVerified",
            Email = request.Email,
            IsSuccess = result.Success,
            Details = result.Success ? null : result.Error
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ChangePasswordResponse), 200)]
    [ProducesResponseType(typeof(ChangePasswordResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new ChangePasswordResponse { Success = false, Error = "Not authenticated" });

        _logger.LogInformation("POST /auth/change-password for user: {Username}", username);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new ChangePasswordResponse { Success = false, Error = string.Join("; ", errors) });
        }

        var result = await _authService.ChangePasswordAsync(username, request);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "PasswordChange",
            Username = username,
            IsSuccess = result.Success,
            Details = result.Success ? null : result.Error
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Send OTP to phone number via Fast2SMS (Indian numbers)
    /// </summary>
    [HttpPost("send-phone-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendPhoneOtp([FromBody] PhoneOtpRequest request)
    {
        _logger.LogInformation("POST /auth/send-phone-otp for phone: {CountryCode}***", request.CountryCode);

        if (!ModelState.IsValid)
            return BadRequest(new SendOtpResponse { Success = false, Error = "Invalid phone number" });

        var result = await _phoneOtpService.SendPhoneOtpAsync(request.PhoneNumber, request.CountryCode);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = result.Success ? "PhoneOtpSent" : "PhoneOtpFailed",
            Details = result.Success ? $"Phone OTP sent to {request.CountryCode}***" : result.Error,
            IsSuccess = result.Success
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Verify phone OTP code
    /// </summary>
    [HttpPost("verify-phone-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPhoneOtp([FromBody] PhoneOtpVerifyRequest request)
    {
        _logger.LogInformation("POST /auth/verify-phone-otp for phone: {CountryCode}***", request.CountryCode);

        if (!ModelState.IsValid)
            return BadRequest(new VerifyOtpResponse { Success = false, Error = "Invalid request" });

        var result = await _phoneOtpService.VerifyPhoneOtpAsync(request.PhoneNumber, request.CountryCode, request.Code);

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = result.Success ? "PhoneVerified" : "PhoneVerifyFailed",
            Details = result.Success ? "Phone verified" : result.Error,
            IsSuccess = result.Success
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}

