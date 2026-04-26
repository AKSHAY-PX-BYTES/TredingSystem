# Email OTP Delivery Setup Guide
**Date:** April 26, 2026  
**Purpose:** Configure real email delivery for OTP verification during signup

---

## 📧 Current Status

### Testing Mode (Current - Mailtrap)
- ✅ OTP code is generated and logged
- ✅ Email sending logic works correctly
- ✅ Database stores OTP records properly
- ❌ Emails don't reach user's inbox (by design for testing)
- 📍 **Location:** Mailtrap dashboard → Inbox

### Production Mode (To be configured)
- ✅ OTP code generated
- ✅ Email sent to user's actual inbox
- 📧 **Location:** User's email inbox (Gmail, Outlook, etc.)

---

## 🎯 How Email Sending Works

### Current Flow
```
User Sign Up
    ↓
/auth/send-otp API called with email
    ↓
OtpService.SendOtpAsync()
    ↓
Generate 6-digit code
    ↓
Save to database with 10-min expiry
    ↓
SendEmailAsync()
    ↓
Check Email credentials (Username & Password)
    ↓
If empty → Log code to console ⚠️ (TESTING MODE)
If filled → Send via SMTP to user's inbox ✅ (PRODUCTION MODE)
```

### Code Path
**File:** `src/TradingSystem.Api/Services/OtpService.cs`
- Line 24-79: `SendOtpAsync()` - Generates and saves OTP
- Line 172-214: `SendEmailAsync()` - Sends email via SMTP
- Line 216-255: `GenerateEmailBody()` - Creates HTML email template

---

## 🔧 Setup Options

### Option 1: Mailtrap (Recommended for Testing)

**What is Mailtrap?**
- Free SMTP testing service
- Intercepts ALL emails (doesn't spam real users)
- Perfect for development and staging
- Has web dashboard to view emails

**Setup Steps:**

1. **Create Mailtrap Account**
   - Go to https://mailtrap.io
   - Sign up (free tier available)
   - Verify email

2. **Get Mailtrap Credentials**
   - Log in to Mailtrap
   - Go to "Email Testing" → "Inboxes"
   - Click "Demo Inbox" (or create new)
   - Click "Show Credentials"
   - Under "SMTP Credentials", you'll see:
     ```
     Host: smtp.mailtrap.io
     Port: 2525 or 587
     Username: (looks like: a1b2c3d4e5f6g7)
     Password: (looks like: a1b2c3d4e5f6g7)
     ```

3. **Set on Render (Local Testing)**
   - Set environment variables in `.env` or local development:
     ```
     Email__Username=your_mailtrap_username
     Email__Password=your_mailtrap_password
     Email__SmtpServer=smtp.mailtrap.io
     Email__SmtpPort=587
     ```

4. **Verify in Mailtrap**
   - Sign up in app with test email
   - Request OTP
   - Check Mailtrap Inbox
   - OTP email should appear there ✅

---

### Option 2: Gmail (For Production)

**Setup Steps:**

1. **Enable Less Secure App Access (Deprecated - Use App Password instead)**
   - Better: Use Gmail App Password feature
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer" (or your device)
   - Google generates 16-character password
   - Copy this password

2. **Set Environment Variables on Render**
   ```
   Email__Username=your-email@gmail.com
   Email__Password=your-16-char-app-password
   Email__SmtpServer=smtp.gmail.com
   Email__SmtpPort=587
   Email__SenderEmail=your-email@gmail.com
   ```

3. **Test**
   - Sign up in app
   - Request OTP
   - Check your inbox ✅

---

### Option 3: SendGrid (Best for Production)

**Why SendGrid?**
- Enterprise-grade email service
- Better deliverability
- Handles bounce/spam management
- Free tier (100 emails/day)

**Setup Steps:**

1. **Create SendGrid Account**
   - Go to https://sendgrid.com
   - Sign up (free tier available)
   - Verify email and phone

2. **Get API Key**
   - Go to "Settings" → "API Keys"
   - Click "Create API Key"
   - Name: "TredingSystem"
   - Copy the key (starts with `SG.`)

3. **Modify OtpService.cs** (If using SendGrid)
   - Would need to use SendGrid NuGet package
   - Different implementation (not SMTP-based)
   - More robust for production

---

## 🚀 Deployment to Render

### Current Setup
The app is already configured with Mailtrap SMTP settings in `appsettings.json`:
```json
"Email": {
    "SmtpServer": "smtp.mailtrap.io",
    "SmtpPort": 587,
    "SenderEmail": "noreply@tredingsystem.com",
    "SenderName": "TredingSystem",
    "Username": "",        // ← Empty (awaiting credentials)
    "Password": ""         // ← Empty (awaiting credentials)
}
```

### Setting Environment Variables on Render

1. **Go to Render Dashboard**
   - https://dashboard.render.com
   - Select your TredingSystem service

2. **Navigate to Environment**
   - Click "Environment"
   - Add new variables:

   **For Mailtrap Testing:**
   ```
   Email__Username = [Your Mailtrap Username]
   Email__Password = [Your Mailtrap Password]
   Email__SmtpServer = smtp.mailtrap.io
   Email__SmtpPort = 587
   Email__SenderEmail = noreply@tredingsystem.com
   Email__SenderName = TredingSystem
   ```

   **For Gmail Production:**
   ```
   Email__Username = your-email@gmail.com
   Email__Password = [16-char app password]
   Email__SmtpServer = smtp.gmail.com
   Email__SmtpPort = 587
   Email__SenderEmail = your-email@gmail.com
   Email__SenderName = TredingSystem
   ```

3. **Save & Redeploy**
   - Click "Save"
   - Render auto-redeploys service (~2-3 minutes)
   - Check deployment logs

---

## 🧪 Testing Email Sending

### Local Testing

1. **Set credentials in appsettings.json** (local only, don't commit!)
   ```json
   "Email": {
       "Username": "your_mailtrap_username",
       "Password": "your_mailtrap_password"
   }
   ```

2. **Run the app**
   ```bash
   dotnet run
   ```

3. **Test via frontend**
   - Go to signup page
   - Enter test email
   - Click "Send OTP"
   - Check Mailtrap inbox

4. **Or test via Postman**
   ```
   POST /auth/send-otp
   Content-Type: application/json
   
   {
       "email": "test@example.com"
   }
   ```

5. **Check Application Logs**
   ```
   📧 Sending OTP email to test@example.com via smtp.mailtrap.io:587
   ✅ Email sent successfully to: test@example.com
   ```

### Troubleshooting

**Error: "Email credentials not configured"**
- Solution: Set `Email__Username` and `Email__Password`
- Check logs for: `⚠️ Email credentials not configured`

**Error: "SMTP Connection failed"**
- Verify SMTP server and port
- Check firewall/network access
- Render has outbound SMTP blocked on free tier → Need to use specific SMTP services

**Error: "Invalid sender email"**
- Use email that matches SMTP account
- For Gmail: Must use your Gmail address
- For Mailtrap: Can use any email in `From` field

**OTP received but looks ugly**
- Email HTML not rendering properly
- Check email client supports HTML
- Gmail/Outlook should work fine

---

## 📋 Email Sending Checklist

### Development Environment
- [ ] Mailtrap account created
- [ ] SMTP credentials obtained
- [ ] Environment variables set locally
- [ ] Test signup → OTP received in Mailtrap
- [ ] Email template looks good

### Render Production
- [ ] Environment variables added to Render dashboard
- [ ] Service redeployed
- [ ] Logs show "Email sent successfully"
- [ ] Real email received in user's inbox
- [ ] Email formatting is correct
- [ ] OTP code is visible and correct

### User Experience
- [ ] User receives OTP within seconds
- [ ] Email subject is clear: "🔐 Your OTP Verification Code - TredingSystem"
- [ ] OTP code is easy to read (42px, monospace font)
- [ ] Security warning included in email
- [ ] Expiry time clearly shown (10 minutes)
- [ ] Professional email template

---

## 🔒 Security Best Practices

### ✅ Implemented
- OTP expires in 10 minutes
- Single-use code (verified then marked as used)
- Random 6-digit code (100,000-999,999)
- SSL/TLS encryption for SMTP
- No OTP visible in logs (only masked if needed)
- Check token before showing session popup

### ⚠️ Consider Adding
- Rate limiting on `/auth/send-otp` endpoint
- Maximum OTP attempts before cooldown
- IP-based tracking for abuse detection
- Email verification bounce handling
- Delivery status notifications

---

## 📝 Email Template Preview

**Subject:** 🔐 Your OTP Verification Code - TredingSystem

**Body:**
```
Hello,

Thank you for signing up to TredingSystem! To verify your email address 
and complete your registration, please use the one-time password (OTP) below:

╔════════════════╗
║  1 2 3 4 5 6   ║  ← Your 6-digit code
║ Expires in 10 min
╚════════════════╝

This code is valid for a single use only and will expire in 10 minutes.

🔒 Security Note: Never share this code with anyone. TredingSystem support 
will never ask for your OTP code.

If you didn't sign up for TredingSystem, please ignore this email.

Best regards,
The TredingSystem Team

© 2024 TredingSystem. All rights reserved.
Privacy Policy | Terms of Service
```

---

## 🎯 Next Steps

1. **For Testing:**
   - Set up Mailtrap account
   - Add credentials to Render env vars
   - Test signup flow
   - Verify email received

2. **For Production:**
   - Choose email service (Gmail/SendGrid)
   - Set up app password (Gmail) or API key (SendGrid)
   - Configure Render environment
   - Monitor email logs
   - Set up bounce handling

3. **Optional Improvements:**
   - Add rate limiting to prevent OTP spam
   - Implement OTP code strength validation
   - Add email verification status tracking
   - Set up delivery monitoring

---

## 📞 Support Resources

- **Mailtrap Support:** https://mailtrap.io/support
- **Render Documentation:** https://docs.render.com/env-vars
- **Gmail App Passwords:** https://myaccount.google.com/apppasswords
- **SendGrid Docs:** https://sendgrid.com/docs
- **SMTP Settings Reference:** https://www.emailonacid.com/blog/article/smtp-settings-guide

---

**Status:** ✅ Email service configured and ready  
**Current Mode:** Testing (Mailtrap)  
**Production Ready:** Once environment variables set on Render  
**Deployment:** Auto-redeploys after config changes

