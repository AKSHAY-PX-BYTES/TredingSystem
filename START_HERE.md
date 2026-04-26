# 🎯 START HERE - Email OTP Configuration (3 Minutes)

**Quick 3-step setup for email delivery**

---

## ⚡ The 3 Steps

### Step 1️⃣: Get Email Credentials (5 minutes)

**Choose Mailtrap (Recommended for testing):**
```
1. Visit: https://mailtrap.io
2. Sign up (free)
3. Go: Email Testing → Inboxes → Demo Inbox
4. Click: "Show Credentials"
5. Copy: Username and Password
```

**OR Choose Gmail (For production):**
```
1. Visit: https://myaccount.google.com/apppasswords
2. Log in
3. Select: "Mail" and "Windows Computer"
4. Copy: 16-character password Google generates
```

### Step 2️⃣: Set Render Variables (3 minutes)

```
1. Go to: https://dashboard.render.com
2. Click: TredingSystem service
3. Click: "Environment"
4. Add Variables:

For Mailtrap:
   Email__Username = [paste-your-username]
   Email__Password = [paste-your-password]

For Gmail:
   Email__Username = your-email@gmail.com
   Email__Password = [paste-16-char-password]
   Email__SmtpServer = smtp.gmail.com

5. Click: "Save"
```

### Step 3️⃣: Test It Works (2 minutes)

```
1. Wait 2-3 minutes for Render to redeploy
2. Sign up in your app
3. Enter email → Click "Send OTP"
4. Check email:
   - Mailtrap: Check Mailtrap dashboard
   - Gmail: Check Gmail inbox
5. ✅ Email received!
```

---

## 📚 Need More Help?

| If You Need | Read This |
|------------|-----------|
| Fast overview (1 min) | This file ✓ |
| Quick reference (5 min) | `OTP_QUICK_REFERENCE.md` |
| Complete guide (20 min) | `EMAIL_OTP_SETUP_GUIDE.md` |
| Visual guide (10 min) | `VISUAL_SETUP_GUIDE.md` |
| All docs index | `README_DOCUMENTATION_INDEX.md` |

---

## ✅ That's It!

Your system is now ready to send OTP emails to users.

**Questions?** Check the documentation files above!

