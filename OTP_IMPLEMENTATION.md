# Email OTP Verification Implementation

## Overview
This document describes the implementation of email-based one-time password (OTP) verification for user registration in the Trading System.

## Changes Made

### Backend (API)

#### 1. **New Entity: OtpEntity** (`src/TradingSystem.Api/Data/Entities/OtpEntity.cs`)
- Stores OTP records with email, code, expiry, and verification status
- Fields:
  - `Id`: Primary key
  - `Email`: User's email address
  - `Code`: 6-digit OTP code
  - `CreatedAt`: Timestamp when OTP was generated
  - `ExpiresAt`: Timestamp when OTP expires (default 10 minutes)
  - `IsVerified`: Flag indicating if OTP was verified
  - `UserId`: Optional foreign key to UserEntity
  - `User`: Navigation property to UserEntity

#### 2. **Updated AppDbContext** (`src/TradingSystem.Api/Data/AppDbContext.cs`)
- Added `DbSet<OtpEntity> Otps` property
- Added OTP entity mapping with:
  - Table name: `otps`
  - Unique index on `(Email, Code)`
  - Index on `Email` for quick lookups
  - Automatic timestamp tracking

#### 3. **New Service: IOtpService** (`src/TradingSystem.Api/Services/IOtpService.cs`)
Interface methods:
- `SendOtpAsync(email)`: Generate and send OTP to email
- `VerifyOtpAsync(email, code)`: Verify OTP code
- `IsEmailVerifiedAsync(email)`: Check if email was verified
- `CleanupExpiredOtpsAsync()`: Remove expired OTPs

#### 4. **OTP Service Implementation** (`src/TradingSystem.Api/Services/OtpService.cs`)
- Generates random 6-digit codes
- Sends emails via SMTP (Mailtrap configured by default)
- Stores OTP in PostgreSQL with configurable expiry (default 10 minutes)
- Automatically deletes expired unverified OTPs before creating new ones
- For dev/testing: Logs OTP code to console if email credentials not configured
- Beautiful HTML email template

**Key Features:**
- Prevents duplicate OTPs for same email
- Validates OTP hasn't expired
- Marks OTP as verified after successful validation
- Thread-safe database operations

#### 5. **New API Models** (`src/TradingSystem.Api/Models/AuthModels.cs`)
```csharp
public class SendOtpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

public class SendOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class VerifyOtpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; }
    
    [Required, RegularExpression(@"^\d{6}$")]
    public string Code { get; set; }
}

public class VerifyOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
```

#### 6. **Updated AuthController** (`src/TradingSystem.Api/Controllers/AuthController.cs`)
New endpoints:
- `POST /auth/send-otp` - Send OTP to email (public, no auth required)
- `POST /auth/verify-otp` - Verify OTP code (public, no auth required)

**Modified endpoint:**
- `POST /auth/register` - Now requires email to be verified with OTP before registration succeeds

#### 7. **Configuration** (`src/TradingSystem.Api/appsettings.json`)
Added configuration sections:
```json
"Email": {
  "SmtpServer": "smtp.mailtrap.io",
  "SmtpPort": 587,
  "SenderEmail": "noreply@tredingsystem.com",
  "SenderName": "TredingSystem",
  "Username": "",
  "Password": ""
},
"Otp": {
  "ExpiryMinutes": 10,
  "CodeLength": 6
}
```

**Note:** Email credentials are empty by default. For production:
1. Sign up at [Mailtrap.io](https://mailtrap.io/) (free tier: 500 emails/day)
2. Get SMTP credentials
3. Set `Email:Username` and `Email:Password` environment variables on Render

#### 8. **Service Registration** (`src/TradingSystem.Api/Program.cs`)
Added to dependency injection:
```csharp
builder.Services.AddScoped<IOtpService, OtpService>();
```

### Frontend (Blazor WASM)

#### 1. **Updated AuthService** (`src/TradingSystem.Web/Services/AuthService.cs`)

Added interface methods:
```csharp
Task<SendOtpResponse> SendOtpAsync(string email);
Task<VerifyOtpResponse> VerifyOtpAsync(string email, string code);
```

Implementations:
- `SendOtpAsync`: Calls `POST /auth/send-otp` endpoint
- `VerifyOtpAsync`: Calls `POST /auth/verify-otp` endpoint
- Both handle connection errors gracefully

#### 2. **New Models** (`src/TradingSystem.Web/Models/Models.cs`)
```csharp
public class SendOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class VerifyOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
```

#### 3. **Enhanced Register Page** (`src/TradingSystem.Web/Pages/Register.razor`)

**Three-step registration flow:**

**Step 1: Email Verification**
- User enters email address
- Clicks "Send OTP" button
- API generates and sends 6-digit code to email

**Step 2: OTP Verification**
- User enters 6-digit code from email
- Code is validated against database
- Expiry is checked (10 minutes default)
- On success, user proceeds to Step 3

**Step 3: Account Details**
- User enters username and password
- Email field is pre-filled and read-only
- Registration proceeds only after OTP verification

**UI Features:**
- Clear step indicators in the form
- Back buttons between steps
- Loading spinners during API calls
- Detailed error messages for each step
- Form auto-clears on success

## Database Changes

### New Table: `otps`
```sql
CREATE TABLE otps (
  id SERIAL PRIMARY KEY,
  email VARCHAR(100) NOT NULL,
  code VARCHAR(6) NOT NULL,
  created_at TIMESTAMP NOT NULL,
  expires_at TIMESTAMP NOT NULL,
  is_verified BOOLEAN NOT NULL DEFAULT false,
  user_id INTEGER,
  UNIQUE(email, code),
  INDEX(email)
);
```

This table is automatically created on API startup via EF Core migrations.

## Environment Configuration

### Development (No Real Email)
1. Leave `Email:Username` and `Email:Password` empty in appsettings.json
2. OTP codes will be logged to console:
   ```
   WARN Email credentials not configured. OTP code for user@example.com: 123456
   ```
3. Use logged code to complete registration flow

### Production (Render/Netlify)

#### Step 1: Set up Email Service
Option A: Mailtrap (recommended for free tier)
1. Sign up at https://mailtrap.io/
2. Create new inbox
3. Copy SMTP credentials
4. Get API token

Option B: SendGrid (free tier)
1. Sign up at https://sendgrid.com/
2. Create new API key
3. Use API for sending

#### Step 2: Set Environment Variables on Render

In Render dashboard for TradingSystem API:
1. Go to Settings → Environment
2. Add variables:
   ```
   Email__Username=your_smtp_username
   Email__Password=your_smtp_password
   Email__SmtpServer=smtp.mailtrap.io  (or other)
   Email__SmtpPort=587
   Email__SenderEmail=noreply@yourdomain.com
   ```

3. Restart the service

## Usage Flow

### For New Users

1. Navigate to `/register`
2. Enter email address
3. Click "📧 Send OTP"
4. Check email inbox (or console logs if dev mode)
5. Enter 6-digit code in next step
6. Click "✅ Verify OTP"
7. Enter username and password
8. Click "📝 Complete Sign Up"
9. Redirects to login page
10. Login with username/password

### API Endpoints

#### Send OTP
```
POST /auth/send-otp
Content-Type: application/json

{
  "email": "user@example.com"
}

Response 200:
{
  "success": true,
  "message": "OTP sent to your email",
  "expiresAt": "2024-01-15T10:15:00Z"
}

Response 400:
{
  "success": false,
  "error": "Email is already registered"
}
```

#### Verify OTP
```
POST /auth/verify-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "code": "123456"
}

Response 200:
{
  "success": true,
  "message": "Email verified successfully"
}

Response 400:
{
  "success": false,
  "error": "OTP has expired"
}
```

#### Register (After OTP Verification)
```
POST /auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}

Response 200:
{
  "success": true,
  "message": "User registered successfully"
}

Response 400:
{
  "success": false,
  "error": "Email must be verified with OTP before registration"
}
```

## Security Considerations

1. **OTP Expiry**: 10 minutes (configurable)
2. **OTP Length**: 6 digits (random, non-sequential)
3. **Email Validation**: RFC-compliant email validation
4. **Duplicate Prevention**: Only one valid OTP per email at a time
5. **Cleanup**: Expired OTPs are automatically deleted before new ones created
6. **Rate Limiting**: Consider adding rate limiting for `/auth/send-otp` to prevent spam
7. **HTTPS**: All email/OTP data transmitted over HTTPS in production
8. **Database**: Passwords hashed with bcrypt, OTP codes stored in plaintext (acceptable for time-limited codes)

## Future Improvements

1. **Rate Limiting**: Add cooldown between OTP requests (e.g., 1 request per 30 seconds)
2. **SMS OTP**: Support SMS delivery via Twilio
3. **OTP History**: Track OTP attempt history for security audit
4. **Resend Limit**: Limit number of OTP resends (e.g., max 3 per email per day)
5. **Admin Panel**: View/manage OTP records in admin dashboard
6. **Customizable Email**: Allow admin to customize OTP email template
7. **Two-Factor Auth**: Extend OTP system for 2FA on login

## Testing

### Manual Testing Steps

1. **Happy Path (Dev Mode)**:
   ```
   1. Go to /register
   2. Enter test@example.com
   3. Check console for OTP code
   4. Enter OTP in form
   5. Create account with username/password
   6. Login works
   ```

2. **Expired OTP**:
   ```
   1. Send OTP
   2. Wait 11+ minutes
   3. Try to verify → Error: "OTP has expired"
   ```

3. **Wrong Code**:
   ```
   1. Send OTP to email
   2. Enter wrong 6-digit code
   3. Try to verify → Error: "Invalid OTP code"
   ```

4. **Duplicate Email**:
   ```
   1. Register user1@test.com successfully
   2. Try to send OTP to user1@test.com again
   3. Get error: "Email is already registered"
   ```

5. **Skip OTP and Register**:
   ```
   1. Try to access /register
   2. Try to register without going through OTP
   3. Error: "Email must be verified with OTP before registration"
   ```

### Automated Testing (Future)

Create `AuthControllerTests.cs`:
- Test SendOtp generates 6-digit code
- Test VerifyOtp with valid code
- Test VerifyOtp with expired code
- Test Register without OTP verification
- Test duplicate email handling

## Deployment Checklist

Before deploying to production:

- [ ] Set up Mailtrap or SendGrid account
- [ ] Add `Email__Username` environment variable on Render
- [ ] Add `Email__Password` environment variable on Render
- [ ] Test email delivery with real email
- [ ] Update `/register` to latest code
- [ ] Test full registration flow in production
- [ ] Monitor logs for OTP delivery issues
- [ ] Set up email alerts for failed sends

## Troubleshooting

### OTP Code Not Received
1. Check email credentials in appsettings/environment variables
2. Check email spam folder
3. Check Mailtrap/SendGrid inbox for bounces
4. Verify SMTP server address and port
5. Check logs for SMTP errors

### "Email is already registered" on first attempt
1. Check if email was already used for a different account
2. Use a different email address
3. Contact admin if accidental duplicate exists

### OTP Code Expired Before Verification
1. OTP expires after 10 minutes (configurable)
2. Click "← Back" to return to Step 1
3. Send a new OTP code
4. Enter new code before 10 minutes elapse

## Files Summary

| File | Purpose |
|------|---------|
| `OtpEntity.cs` | OTP database entity |
| `AppDbContext.cs` | OTP table mapping |
| `IOtpService.cs` | OTP service interface |
| `OtpService.cs` | OTP service implementation |
| `AuthModels.cs` | OTP request/response DTOs (API) |
| `AuthController.cs` | OTP endpoints (/auth/send-otp, /auth/verify-otp) |
| `Program.cs` | Service registration (IOtpService) |
| `appsettings.json` | Email and OTP configuration |
| `AuthService.cs` | OTP methods (frontend) |
| `Models.cs` | OTP request/response DTOs (frontend) |
| `Register.razor` | Three-step registration UI |

## Questions & Support

For issues or questions about the OTP implementation:
1. Check this documentation
2. Review the code comments
3. Check logs for detailed error information
4. Verify email configuration on Render environment
