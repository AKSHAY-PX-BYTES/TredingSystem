# Email OTP System Architecture

## 🏗️ System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        NETLIFY (Frontend)                           │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                  Register.razor (Blazor)                     │  │
│  │                                                               │  │
│  │  Step 1: Enter Email        Step 2: Enter OTP       Step 3:  │  │
│  │  ┌──────────────┐           ┌──────────────┐      Username   │  │
│  │  │ Email Input  │           │ OTP Input    │      Password   │  │
│  │  │ Send OTP Btn │   ────→  │ Verify Btn   │  ──→  Sign Up   │  │
│  │  └──────────────┘           └──────────────┘      Btn        │  │
│  └────────┬─────────────────────────┬─────────────────────┬──────┘  │
│           │                         │                     │          │
│           │ POST /auth/send-otp     │ POST /auth/verify   │ POST     │
│           │                         │ -otp                │ /auth    │
│           │                         │                     │/register │
└───────────┼─────────────────────────┼─────────────────────┼──────────┘
            │                         │                     │
            │ HTTP + JWT              │ HTTP + JWT          │ HTTP + JWT
            │                         │                     │
┌───────────▼─────────────────────────▼─────────────────────▼──────────┐
│                     RENDER (Backend API)                             │
│                    .NET 8 / ASP.NET Core                             │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                   AuthController                               │ │
│  │                                                                │ │
│  │  POST /auth/send-otp        POST /auth/verify-otp            │ │
│  │  ├─ Validate email          ├─ Validate code format         │ │
│  │  ├─ Check not registered    ├─ Check not expired            │ │
│  │  ├─ Generate OTP code       ├─ Check code exists            │ │
│  │  ├─ Save to DB              ├─ Mark as verified             │ │
│  │  └─ Send email              └─ Return success                │ │
│  │                                                                │ │
│  │  POST /auth/register (Modified)                               │ │
│  │  ├─ Check email verified via OTP                            │ │
│  │  ├─ Hash password                                            │ │
│  │  ├─ Create user in DB                                        │ │
│  │  └─ Return success                                           │ │
│  └────────────┬────────────────────────────────┬────────────────┘ │
│               │                                │                  │
│               │                                │                  │
│  ┌────────────▼────────────────┐  ┌───────────▼──────────────┐   │
│  │      OtpService             │  │   AuthService             │   │
│  │                              │  │                          │   │
│  │  SendOtpAsync()              │  │  RegisterAsync()        │   │
│  │  ├─ Generate 6-digit code    │  │  LoginAsync()            │   │
│  │  ├─ Save to database         │  │  GetUserAsync()          │   │
│  │  └─ Send email via SMTP      │  │                          │   │
│  │                              │  │                          │   │
│  │  VerifyOtpAsync()            │  │                          │   │
│  │  ├─ Find OTP record          │  │                          │   │
│  │  ├─ Validate format          │  │                          │   │
│  │  ├─ Check expiry             │  │                          │   │
│  │  └─ Mark verified            │  │                          │   │
│  │                              │  │                          │   │
│  │  CleanupExpiredOtpsAsync()   │  │                          │   │
│  │  └─ Delete OTPs > 10 min old │  │                          │   │
│  └────────────┬────────────────┘  └────────────┬──────────────┘   │
│               │                                │                  │
│               └────────────┬───────────────────┘                  │
│                            │                                      │
│  ┌─────────────────────────▼──────────────────────────────────┐  │
│  │              AppDbContext (EF Core)                        │  │
│  │                                                             │  │
│  │  DbSet<UserEntity>         │  DbSet<OtpEntity>           │  │
│  │  DbSet<FeatureFlagEntity>  │                             │  │
│  └─────────────────────────┬──────────────────────────────────┘  │
│                            │                                      │
└────────────────────────────┼──────────────────────────────────────┘
                             │
                             │ SQL Queries
                             │
┌────────────────────────────▼──────────────────────────────────────┐
│               NEON.TECH (PostgreSQL Database)                     │
│                                                                   │
│  ┌──────────────────┐  ┌─────────────────┐  ┌──────────────────┐ │
│  │  users table     │  │  feature_flags  │  │  otps table      │ │
│  │                  │  │  table          │  │                  │ │
│  │  ├─ id           │  │                 │  │  ├─ id           │ │
│  │  ├─ username*    │  │  ├─ id          │  │  ├─ email*       │ │
│  │  ├─ email*       │  │  ├─ feature_key │  │  ├─ code         │ │
│  │  ├─ password_    │  │  ├─ display_    │  │  ├─ created_at   │ │
│  │  │   hash        │  │  │  name        │  │  ├─ expires_at   │ │
│  │  ├─ display_name │  │  ├─ is_enabled  │  │  ├─ is_verified  │ │
│  │  ├─ role         │  │  └─ updated_at  │  │  └─ Indexes:     │ │
│  │  ├─ created_at   │  │                 │  │     - email      │ │
│  │  └─ last_login_at│  │                 │  │     - (email,    │ │
│  │                  │  │                 │  │       code)      │ │
│  │  Indexes:        │  │                 │  │                  │ │
│  │  - username      │  │                 │  │                  │ │
│  │  - email         │  │                 │  │                  │ │
│  └──────────────────┘  └─────────────────┘  └──────────────────┘ │
└───────────────────────────────────────────────────────────────────┘
```

## 📧 Email Sending Flow

```
┌─────────────────────────────────┐
│   POST /auth/send-otp Request   │
│   {"email": "user@example.com"} │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│    OtpService.SendOtpAsync()    │
└────────┬────────────────────────┘
         │
         ├─► Check email not already registered
         │
         ├─► Generate random 6-digit code
         │   └─► Random.Next(100000, 999999)
         │
         ├─► Create OTP record
         │   ├─ Email: user@example.com
         │   ├─ Code: 534821
         │   ├─ CreatedAt: 2024-01-15 10:00:00
         │   ├─ ExpiresAt: 2024-01-15 10:10:00 (+10 min)
         │   └─ IsVerified: false
         │
         ├─► Save to database (otps table)
         │
         ├─► Send email via SMTP
         │   ├─ Server: smtp.mailtrap.io:587 (TLS)
         │   ├─ From: noreply@tredingsystem.com
         │   ├─ To: user@example.com
         │   ├─ Subject: "Your OTP Verification Code"
         │   └─ Body: HTML email with 6-digit code
         │
         └─► Return response
             └─ {"success": true, "expiresAt": "..."}
```

## 🔒 OTP Verification Flow

```
┌──────────────────────────────────────────┐
│ POST /auth/verify-otp Request            │
│ {                                        │
│   "email": "user@example.com",          │
│   "code": "534821"                      │
│ }                                        │
└────────────┬─────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────┐
│  OtpService.VerifyOtpAsync()             │
└────────────┬─────────────────────────────┘
             │
             ├─► Validate code format (must be 6 digits)
             │   └─► Regex: ^\d{6}$
             │
             ├─► Find OTP record in database
             │   └─► WHERE email = ? AND code = ? AND is_verified = false
             │
             ├─► Check OTP exists
             │   └─► If not found: Return "Invalid OTP code"
             │
             ├─► Check OTP not expired
             │   ├─ Get ExpiresAt: 2024-01-15 10:10:00
             │   ├─ Get Now: 2024-01-15 10:09:45
             │   └─ If Now > ExpiresAt: Return "OTP has expired"
             │
             ├─► Mark OTP as verified
             │   └─ UPDATE otps SET is_verified = true
             │
             └─► Return response
                 └─ {"success": true, "message": "Email verified"}
```

## 👤 Registration Flow

```
┌────────────────────────────────────────────┐
│  POST /auth/register Request               │
│  {                                         │
│    "username": "john_doe",                │
│    "email": "user@example.com",           │
│    "password": "SecurePass123!",          │
│    "confirmPassword": "SecurePass123!"    │
│  }                                         │
└────────────┬───────────────────────────────┘
             │
             ▼
┌────────────────────────────────────────────┐
│  AuthService.RegisterAsync()               │
└────────────┬───────────────────────────────┘
             │
             ├─► Validate input format
             │   ├─ Email: [EmailAddress]
             │   ├─ Password: 6+ chars
             │   └─ ConfirmPassword matches Password
             │
             ├─► Check email verified via OTP
             │   ├─ Query: WHERE email = ? AND is_verified = true
             │   ├─ If not verified: Return error
             │   │   "Email must be verified with OTP before registration"
             │   └─ If verified: Continue
             │
             ├─► Check email not already registered
             │   └─ Query: WHERE email = ? in users table
             │
             ├─► Check username not already registered
             │   └─ Query: WHERE username = ? in users table
             │
             ├─► Hash password with bcrypt
             │   └─ BcryptNet.HashPassword("SecurePass123!")
             │       = "$2a$11$..."
             │
             ├─► Create UserEntity
             │   ├─ Username: john_doe
             │   ├─ Email: user@example.com
             │   ├─ PasswordHash: $2a$11$...
             │   ├─ DisplayName: john_doe
             │   ├─ Role: Trader
             │   └─ CreatedAt: now
             │
             ├─► Save to users table
             │
             ├─► Mark OTP record as processed (optional)
             │   └─ Store reference to created user
             │
             └─► Return response
                 └─ {"success": true, "message": "User registered"}
```

## 🔄 Data Flow Diagram

```
                    User (Browser)
                          │
                          │ 1. Enter email
                          │    + Click "Send OTP"
                          ▼
                    Frontend (Register.razor)
                          │
                          │ 2. POST /auth/send-otp
                          │    {email: "..."}
                          ▼
                    Render API (AuthController)
                          │
                    ┌─────┴─────┐
                    │           │
                    ▼           ▼
            OtpService    AuthService
                    │
            ┌───────┴────────┐
            │                │
            ▼                ▼
        Database        Email Service
        (Save OTP)      (SMTP Send)
            │                │
            │                ▼
            │           User's Inbox
            │           (Email arrives)
            │
            │ 3. User enters OTP code
            │    + Click "Verify"
            ▼
        Frontend
            │
            │ 4. POST /auth/verify-otp
            │    {email: "...", code: "..."}
            ▼
        Render API
            │
            ▼
        OtpService
            │
            ├─ Query DB for OTP
            ├─ Validate code
            ├─ Check expiry
            └─ Mark verified
            │
            ▼ 5. Success response
        Frontend
            │
            │ 6. User enters username/password
            │    + Click "Sign Up"
            ▼
        Frontend
            │
            │ 7. POST /auth/register
            │    {username: "...", email: "...", password: "..."}
            ▼
        Render API
            │
            ├─ Check OTP verified
            ├─ Validate credentials
            ├─ Hash password
            └─ Create user
            │
            ├─ Save user to DB
            │
            ▼ 8. Success response
        Frontend
            │
            │ 9. Redirect to /login
            ▼
        Login Page
            │
            │ 10. User can now login!
            ▼
        ✅ Registration Complete
```

## 🔌 Integration Points

### Frontend → API
```
AuthService.cs
├─ SendOtpAsync(email)
│  └─ POST /auth/send-otp
│     Request: {email: "..."}
│     Response: SendOtpResponse
│
└─ VerifyOtpAsync(email, code)
   └─ POST /auth/verify-otp
      Request: {email: "...", code: "..."}
      Response: VerifyOtpResponse
```

### API → Database
```
OtpService.cs
├─ _context.Otps.AddAsync(otp)
├─ _context.Otps.FirstOrDefaultAsync(...)
├─ _context.Otps.Where(...).ToListAsync()
└─ _context.SaveChangesAsync()
```

### API → Email
```
OtpService.cs
└─ SendEmailAsync(toEmail, code)
   ├─ SmtpClient(smtpServer, port)
   ├─ client.Credentials = new NetworkCredential(username, password)
   ├─ MailMessage to user
   ├─ HTML body with 6-digit code
   └─ client.SendMailAsync(mailMessage)
```

## 📊 Entity Relationship

```
┌──────────────┐
│ users        │
├──────────────┤
│ id (PK)      │
│ username     │
│ email        │◄─────────┐
│ password_hash│          │
│ role         │          │
│ created_at   │          │ Email lookup
└──────────────┘          │ (verification)
                          │
                    ┌─────┴───────┐
                    │ otps        │
                    ├─────────────┤
                    │ id (PK)     │
                    │ email       │─────┐
                    │ code        │     │
                    │ created_at  │     │
                    │ expires_at  │     │ Time-based
                    │ is_verified │     │ expiry check
                    └─────────────┘     │
                                        │
                        Automatic cleanup
                        after 10 minutes
```

## ⏱️ Timing Diagram

```
Timeline for OTP Verification:

T=0:00    User enters email, clicks "Send OTP"
          │
          ├─► API generates code: 534821
          ├─► API saves to database (created_at)
          └─► API sends email
                │
T=0:02-5  Email in transit / user reading email
          │
T=0:05    User sees email with code: 534821
          │ (OTP still valid: expires at T=10:00)
          │
          ├─► User enters code
          ├─► Clicks "Verify OTP"
          └─► API validates:
                ├─ Code exists in DB
                ├─ Code matches (534821 == 534821) ✓
                ├─ Not expired (10:05 < 10:10) ✓
                └─ Not verified yet (is_verified = false) ✓
          │
T=0:06    API marks as verified (is_verified = true)
          │
          └─► Frontend shows Step 3: Create account
          
T=0:07    User enters username, password
          │
          ├─► Clicks "Complete Sign Up"
          └─► API checks:
                ├─ Email verified (is_verified = true) ✓
                ├─ Email not registered (users table) ✓
                ├─ Username not registered ✓
                └─ Password valid ✓
          │
T=0:08    API creates user, hashes password
          │ Saves to users table
          │
          └─► Frontend redirects to /login
          
T=0:09    User logs in with username/password ✓
          
T=10:10   Meanwhile... OTP expires
          └─► If user tried to verify now:
              "OTP has expired - please request new code"
              
T=10:11   Cleanup job runs (daily)
          └─► Deletes all expired OTPs older than 10 min
              (optional - keeps database clean)
```

## 🔌 Component Interaction

```
Register.razor
│
├─ Calls: AuthService.SendOtpAsync()
│
├─ Calls: AuthService.VerifyOtpAsync()
│
└─ Calls: AuthService.RegisterAsync()
           │
           └─ API calls AuthController.Register()
              │
              ├─ Calls: OtpService.IsEmailVerifiedAsync()
              │
              └─ Calls: AuthService.RegisterAsync()
                 │
                 └─ API calls: _context.Users.Add()
```

---

This architecture ensures:
- ✅ Separation of concerns (Services, Controllers)
- ✅ Reusability (OtpService used by AuthService)
- ✅ Testability (Interfaces for dependencies)
- ✅ Security (HTTPS, TLS, bcrypt)
- ✅ Performance (Indexes, async/await)
- ✅ Scalability (Stateless API, managed database)
