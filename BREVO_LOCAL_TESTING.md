# 🧪 LOCAL TESTING GUIDE - Brevo Email Testing

**Verify Brevo email setup works locally before deploying to Render**

---

## ⚡ Quick Local Test (5 minutes)

### Step 1: Get Test API Key from Brevo
```
1. Go to: https://app.brevo.com
2. Settings → SMTP & API → API Tokens
3. Create new token
4. Copy token (starts with xkeysib-)
```

### Step 2: Update Local Configuration
**File:** `appsettings.Development.json` or `appsettings.json`

```json
{
  "EmailProvider": {
    "Type": "Brevo"
  },
  "EmailProviders": {
    "Brevo": {
      "ApiKey": "xkeysib-paste-your-token-here"
    }
  }
}
```

### Step 3: Run Application
```powershell
# In project directory
dotnet run

# Or in Visual Studio
# Press F5
```

### Step 4: Test Signup Flow
```
1. Open: https://localhost:5000 (or your local port)
2. Click: Sign Up
3. Enter: youremail@gmail.com (use real email)
4. Enter: TestPass123
5. Click: Sign Up
6. Wait for: "Check your email"
```

### Step 5: Check Email
```
1. Go to Gmail/Yahoo/etc
2. Refresh inbox
3. Should see: "Your OTP Verification Code - TredingSystem"
4. Copy 6-digit code
5. Paste into app
6. Click: Verify
7. Success! ✅
```

---

## 🔍 Debugging Locally

### Check Console Logs
```
Look for these success messages:
  ✅ "Email sent successfully via Brevo to: youremail@gmail.com"

Or error messages:
  ⚠️ "Brevo API key not configured"
  ❌ "Error sending email via Brevo"
```

### Test With Different Emails
```
Test cases:
  1. Gmail: youremail@gmail.com
  2. Yahoo: youremail@yahoo.com
  3. Outlook: youremail@outlook.com
  4. Corporate: yourname@company.com

All should work the same way
```

### Check Response Details
The BrevoEmailProvider logs:
```csharp
// Success
_logger.LogInformation("✅ Email sent successfully via Brevo to: {Email}", toEmail);

// Error details
_logger.LogError("❌ Brevo API error: {Status} - {Error}", 
  response.StatusCode, errorContent);
```

---

## 🧬 Code Review Checklist

### Verify BrevoEmailProvider Implementation
**File:** `src/TradingSystem.Api/Services/EmailProviders/EmailService.cs`

```csharp
// Lines 18-82: BrevoEmailProvider class

✅ Check 1: API key configuration
  var apiKey = _configuration["EmailProviders:Brevo:ApiKey"];

✅ Check 2: API endpoint
  private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";

✅ Check 3: HTTP headers
  client.DefaultRequestHeaders.Add("api-key", apiKey);

✅ Check 4: JSON payload
  var payload = new { sender, to, subject, htmlContent };

✅ Check 5: Response handling
  if (response.IsSuccessStatusCode) { success }
  else { error logging }

✅ Check 6: Error messages are helpful
  Logs guide users to enable email if key is missing
```

### Verify OtpService Integration
**File:** `src/TradingSystem.Api/Services/OtpService.cs`

```csharp
// Line 7: Using statement
using TradingSystem.Api.Services.EmailProviders;

// Line 17: Private field
private readonly IEmailService _emailService;

// Line 19-22: Constructor
public OtpService(..., IEmailService emailService, ...)
{
    _emailService = emailService;
}

// Line 64-75: Usage in SendOtpAsync
await _emailService.SendOtpEmailAsync(email, code);

// Verify: Graceful error handling
// Email failure doesn't crash signup (try-catch)
```

---

## 📝 Test Cases

### Test Case 1: Valid Setup
```
Scenario: API key configured correctly
Steps:
  1. Set EmailProvider__Type = Brevo
  2. Set EmailProviders__Brevo__ApiKey = valid-token
  3. Sign up with email
  4. Request OTP

Expected:
  ✅ Console: "Email sent successfully"
  ✅ Inbox: Email received
  ✅ Email contains: OTP code
  ✅ Code works: User verified
```

### Test Case 2: Missing API Key
```
Scenario: API key not configured
Steps:
  1. Remove or blank: EmailProviders__Brevo__ApiKey
  2. Sign up with email
  3. Request OTP

Expected:
  ⚠️ Console: "Brevo API key not configured"
  ⚠️ Console: Helpful setup instructions
  ❌ Email not sent
  ✅ OTP saved to DB (graceful failure)
  ✅ App doesn't crash
```

### Test Case 3: Invalid API Key
```
Scenario: Wrong/expired API key
Steps:
  1. Set: EmailProviders__Brevo__ApiKey = wrong-token
  2. Sign up with email
  3. Request OTP

Expected:
  ❌ Console: "Brevo API error: 401"
  ❌ Email not sent
  ✅ OTP saved (doesn't break signup)
  ⚠️ Log entry for debugging
```

### Test Case 4: Invalid Email
```
Scenario: Bad email format
Steps:
  1. Sign up with: not-an-email
  2. Request OTP

Expected:
  ❌ Console: Validation error or "400 Bad Request"
  ❌ Email not sent
  ✅ Error logged
  ⚠️ User sees form validation error first
```

### Test Case 5: Email Template
```
Scenario: Verify email contains all required elements
Steps:
  1. Sign up with valid email
  2. Check received email

Expected:
  ✅ From: noreply@tredingsystem.com
  ✅ Subject: 🔐 Your OTP Verification Code - TredingSystem
  ✅ Body contains: 6-digit code
  ✅ Body contains: "Expires in 10 minutes"
  ✅ Body contains: Security warning
  ✅ Body contains: Professional styling
```

---

## 🎯 Integration Points to Test

### OtpService Integration
```csharp
// Test this flow works:
var otp = await _otpService.SendOtpAsync("user@example.com");

// Should:
// 1. Generate 6-digit code
// 2. Save to database
// 3. Call _emailService.SendOtpEmailAsync()
// 4. Return success

// Verify in logs:
// "✅ Email sent successfully via Brevo"
```

### Dependency Injection
```csharp
// Verify EmailService is registered
// In Program.cs or Startup.cs should have:
services.AddScoped<IEmailService, EmailService>();

// Test: OtpService receives IEmailService
// Should NOT fail with: "Cannot inject IEmailService"
```

### Configuration Binding
```csharp
// Test configuration is read correctly:

var providerName = configuration["EmailProvider:Type"];
// Should be: "Brevo"

var apiKey = configuration["EmailProviders:Brevo:ApiKey"];
// Should be: "xkeysib-..."

// Verify:
// - No null pointer exceptions
// - Values read correctly
// - Async operations complete
```

---

## 📊 Performance Testing

### Email Delivery Speed
```
Measure:
  1. Send OTP request
  2. Note time
  3. Check email inbox
  4. Calculate delivery time

Expected:
  ✅ < 5 seconds (most common)
  ✅ < 10 seconds (acceptable)
  ❌ > 30 seconds (investigate)

Log check:
  Render/local logs show: exact timestamp
  Email shows: received time
  Calculate: difference
```

### Load Testing
```
Single request: ✅ Works
Multiple users: Test signup flow with:
  1. 2 users simultaneously
  2. 5 users in sequence
  3. Rapid requests

Expected:
  ✅ All emails sent
  ✅ No rate limiting
  ✅ No API errors
  ✅ Database consistent
```

---

## 🛠️ Troubleshooting Scenarios

### Scenario 1: "Email key not configured" warning
```
Cause: Missing or empty API key

Check:
  1. appsettings.json has:
     "EmailProviders": {
       "Brevo": {
         "ApiKey": "xkeysib-..."
       }
     }
  
  2. Value is not empty

  3. No typos in key name

Fix:
  1. Get fresh API key from Brevo
  2. Update appsettings.json
  3. Restart app
  4. Test again
```

### Scenario 2: "Brevo API error: 401"
```
Cause: Invalid/expired API key

Check:
  1. Token starts with "xkeysib-"
  2. Token is not truncated
  3. Token hasn't expired
  4. Token has correct permissions

Fix:
  1. Generate new token in Brevo
  2. Update appsettings.json
  3. Restart app
  4. Test again
```

### Scenario 3: "Brevo API error: 400"
```
Cause: Bad request (wrong email format or payload)

Check:
  1. Email format is valid: user@domain.com
  2. No special characters in email
  3. No spaces in email
  4. Sender email is valid

Fix:
  1. Validate email before sending
  2. Check payload structure
  3. Review Brevo API docs
  4. Enable detailed logging
```

### Scenario 4: "Response status 500"
```
Cause: Brevo server error

Check:
  1. Brevo status page: https://status.brevo.com
  2. API is online
  3. No maintenance window

Fix:
  1. Wait 5-10 minutes
  2. Retry
  3. Contact Brevo support if persistent
```

---

## 🔐 Security Testing

### Test 1: API Key Not Exposed
```
Verify:
  1. API key NOT in logs
  2. API key NOT in database
  3. API key NOT in error messages to user
  4. API key ONLY in environment variables

Check logs:
  Search for: "xkeysib-"
  Result: Should be ZERO matches
```

### Test 2: Email Content Security
```
Verify:
  1. HTML escaping works
  2. XSS attempts blocked
  3. No injection possible
  4. Template is sanitized

Test with email: <script>alert('xss')</script>@test.com
Expected: Rejected or escaped, not executed
```

### Test 3: User Privacy
```
Verify:
  1. OTP code not logged in plaintext
  2. Email addresses not exposed
  3. Delivery tracking is secure
  4. No data leakage
```

---

## 📋 Local Test Checklist

### Pre-Testing
- [ ] Brevo account created
- [ ] API key generated (xkeysib-...)
- [ ] API key added to appsettings.json
- [ ] EmailService.cs present
- [ ] OtpService.cs updated
- [ ] Application compiles without errors

### Testing
- [ ] App starts without errors
- [ ] Sign up page loads
- [ ] Signup flow works
- [ ] OTP email sent to inbox
- [ ] Email arrives < 5 seconds
- [ ] Email contains correct code
- [ ] Code format is 6 digits
- [ ] Code works in app
- [ ] Account created successfully

### Verification
- [ ] Console logs show success message
- [ ] No errors in application logs
- [ ] Email template looks professional
- [ ] All test cases pass
- [ ] No security issues

### Post-Testing
- [ ] Document any issues
- [ ] Review logs for errors
- [ ] Check Brevo dashboard
- [ ] Verify statistics
- [ ] Ready for production

---

## 📞 If Issues Occur

### Check These First
```
1. Is Brevo account active?
   → Login to https://app.brevo.com

2. Is API key valid?
   → Copy fresh from Brevo
   → Paste into config

3. Is EmailService.cs properly formatted?
   → Check for JSON syntax errors
   → Verify no typos

4. Is OtpService using IEmailService?
   → Check constructor
   → Verify dependency injection

5. Are errors logged?
   → Check console output
   → Search for "Brevo" in logs
```

### Log File Locations
```
Console: Real-time in terminal/VS
Application logs: var/logs/
Event Viewer: Windows logs
```

### Get Help
```
Brevo Support:
  https://help.brevo.com
  support@brevo.com
  
Render Documentation:
  https://render.com/docs
  
Our Code:
  src/TradingSystem.Api/Services/EmailProviders/EmailService.cs
```

---

## ✅ Success Criteria

Local testing is complete when:

- ✅ Brevo account created and active
- ✅ API key generated and configured
- ✅ Application starts without errors
- ✅ OTP emails sent successfully
- ✅ Emails arrive in inbox < 5 seconds
- ✅ Email template looks professional
- ✅ All 5 test cases pass
- ✅ No security vulnerabilities found
- ✅ Console logs show success messages
- ✅ Ready to deploy to Render

**All checked? → Ready to deploy! 🚀**

---

## 🎉 Next Steps

1. **Complete local testing** using this guide
2. **Deploy to Render** using BREVO_SETUP_GUIDE.md
3. **Test on production** with live URL
4. **Monitor deployment** with Render logs
5. **Verify end-to-end** email delivery

**You're ready to go live!** ✨

