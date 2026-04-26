# OTP Email Setup - Quick Reference Card
**One-Page Guide - Print This!**

---

## 🎯 The 3 Email Options

| Option | Setup Time | Cost | Email Reception | Best For |
|--------|-----------|------|-----------------|----------|
| **No Credentials** | 0 min | Free | Logs only | Quick testing |
| **Mailtrap** | 5 min | Free | Mailtrap inbox | Safe testing |
| **Gmail** | 10 min | Free | Real inbox | Production testing |

---

## ⚡ Fastest Setup (Mailtrap)

### Step 1️⃣: Get Credentials
```
1. Go to https://mailtrap.io
2. Sign up
3. Email Testing → Inboxes → Demo Inbox → Show Credentials
4. Copy Username & Password
```

### Step 2️⃣: Set on Render
```
Render Dashboard
  → TredingSystem service
  → Environment
  
Add these variables:
  Email__Username = [PASTE_USERNAME]
  Email__Password = [PASTE_PASSWORD]
  
Click "Save"
```

### Step 3️⃣: Test
```
Wait 2 min for redeploy
Sign up in app
Request OTP
Check Mailtrap inbox ✅
```

---

## 🔧 Environment Variables Reference

### For Mailtrap
```
Email__Username      = [Your Mailtrap username]
Email__Password      = [Your Mailtrap password]
Email__SmtpServer    = smtp.mailtrap.io  (default)
Email__SmtpPort      = 587  (default)
```

### For Gmail
```
Email__Username      = your-email@gmail.com
Email__Password      = 16-char-app-password
Email__SmtpServer    = smtp.gmail.com
Email__SmtpPort      = 587
Email__SenderEmail   = your-email@gmail.com
```

**Get Gmail App Password:**
1. Go to https://myaccount.google.com/apppasswords
2. Select "Mail" and "Windows Computer"
3. Copy the 16-character password

---

## 📧 Email Flow

```
User Sign Up
    ↓
Send OTP Request
    ↓
Generate Random Code
    ↓
Save to Database (10 min expiry)
    ↓
Send Email
    ├─ If No Credentials → Log to console ⚠️
    └─ If Credentials → Send to inbox ✅
    ↓
User Receives Email
    ↓
Copy Code
    ↓
Verify OTP
    ↓
Complete Sign Up ✅
```

---

## ✅ Checklist

### Setup
- [ ] Choose email provider
- [ ] Get credentials
- [ ] Set environment variables on Render
- [ ] Wait for redeploy

### Testing
- [ ] Sign up with test email
- [ ] Request OTP
- [ ] Check email received
- [ ] Verify code works
- [ ] Complete signup

### Troubleshooting
- [ ] Logs show "Email sent successfully" ✅
- [ ] Check Mailtrap inbox (not spam)
- [ ] Credentials copied exactly
- [ ] Environment variable names correct

---

## 🆘 Common Issues

**"Email credentials not configured"**
- ✅ Solution: Set Email__Username and Email__Password

**"SMTP Connection failed"**
- ✅ Solution: Check SMTP server and port are correct

**"No email received"**
- ✅ Solution: Check Mailtrap inbox, not spam folder

**"Invalid sender email"**
- ✅ Solution: Use email matching SMTP account

---

## 📱 Email Content

**From:** noreply@tredingsystem.com  
**Subject:** 🔐 Your OTP Verification Code - TredingSystem

**Body:**
```
Hello,

Thank you for signing up to TredingSystem!

Your OTP Code:
┌─────────────┐
│ 1 2 3 4 5 6 │
└─────────────┘

Expires in 10 minutes

🔒 Never share this code with anyone!
```

---

## 🔗 Important Links

| Resource | URL |
|----------|-----|
| Mailtrap | https://mailtrap.io |
| Gmail App Password | https://myaccount.google.com/apppasswords |
| Render Docs | https://docs.render.com/env-vars |
| Setup Guide | See EMAIL_OTP_SETUP_GUIDE.md |

---

## 💬 Quick Q&A

**Q: Where's my OTP code?**
- A: In application logs (no credentials) OR in Mailtrap inbox (with credentials)

**Q: How long is OTP valid?**
- A: 10 minutes

**Q: Can I use the same code twice?**
- A: No, single-use only

**Q: What's the code format?**
- A: 6 random digits (100000-999999)

**Q: How to get Mailtrap username?**
- A: Mailtrap Inbox → Show Credentials section

---

## 🚀 Status

- ✅ OTP service working
- ✅ Email template created
- ⏳ Waiting for: Environment variables setup
- 🎯 Ready to: Test email delivery

---

**Need help?** Check `EMAIL_OTP_SETUP_GUIDE.md` for detailed instructions!

