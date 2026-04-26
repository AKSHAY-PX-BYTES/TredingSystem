# 🎨 Brevo Visual Setup Guide - Step by Step

**This is a visual walkthrough of the Brevo setup process**

---

## 📸 Step 1: Create Brevo Account

### Screen 1: Visit Website
```
┌─────────────────────────────────────────────────────────┐
│  https://www.brevo.com/free-email                       │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │                                                   │  │
│  │    🚀 Brevo (Formerly Sendinblue)               │  │
│  │                                                   │  │
│  │    Sign up for free                              │  │
│  │    ┌──────────────────────────────┐              │  │
│  │    │ Start Free (Button)          │              │  │
│  │    └──────────────────────────────┘              │  │
│  │                                                   │  │
│  │    300 emails/day free                           │  │
│  │    No credit card needed                         │  │
│  │                                                   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 2: Sign Up Form
```
┌─────────────────────────────────────────────────────────┐
│  Create Your Account                                    │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Email Address:                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ your.email@gmail.com                             │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  Password (min 8 chars):                                │
│  ┌──────────────────────────────────────────────────┐  │
│  │ ••••••••                                         │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ☑️ I agree to Terms of Service                       │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Create Account (Blue Button)                   │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 3: Verify Email
```
┌─────────────────────────────────────────────────────────┐
│  ✅ Email Verification                                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  We sent a verification link to:                        │
│  your.email@gmail.com                                   │
│                                                         │
│  📧 Check your inbox and click the link                 │
│     (Check spam folder if not found)                    │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Continue to Dashboard (Button)                 │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🔑 Step 2: Generate API Key

### Navigation Path
```
Brevo Dashboard
    ↓
Settings (⚙️ icon, bottom left)
    ↓
Left Sidebar → SMTP & API
    ↓
API Tokens (tab)
    ↓
Create a new API token (button)
```

### Screen 1: Settings Menu
```
┌─────────────────────────────────────────────────────────┐
│  Dashboard                                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Left Sidebar:                                          │
│  ┌──────────────────────────┐                          │
│  │ 📊 Dashboard             │                          │
│  │ 📧 Transactional         │                          │
│  │ 📬 Marketing             │                          │
│  │ 👥 Contacts              │                          │
│  │ ⚙️  Settings             │ ← Click this
│  │ 🆘 Help                  │                          │
│  └──────────────────────────┘                          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 2: SMTP & API Section
```
┌─────────────────────────────────────────────────────────┐
│  Settings                                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Left Menu:                                             │
│  ┌────────────────────────────────────────────────┐    │
│  │ General                                        │    │
│  │ Billing                                        │    │
│  │ Team                                           │    │
│  │ ✓ SMTP & API                                  │    │ ← Select
│  │ Security                                       │    │
│  │ Notifications                                  │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  Right Panel: SMTP & API                                │
│  ┌────────────────────────────────────────────────┐    │
│  │ [SMTP] [API Tokens] [Webhooks]                │    │
│  │                                                │    │
│  │ Select: API Tokens ← Click this               │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 3: API Tokens Page
```
┌─────────────────────────────────────────────────────────┐
│  API Tokens                                             │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Your API Tokens:                                       │
│  ┌──────────────────────────────────────────────────┐  │
│  │                                                  │  │
│  │  [Create a new API token] (Blue button)         │  │
│  │                                                  │  │
│  │  No tokens yet                                   │  │
│  │  ← Click button above                            │  │
│  │                                                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 4: Create Token Dialog
```
┌─────────────────────────────────────────────────────────┐
│  Create a new API token                                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Token Name:                                            │
│  ┌──────────────────────────────────────────────────┐  │
│  │ TredingSystem                                    │  │
│  └──────────────────────────────────────────────────┘  │
│  (Can be anything, like "MyApp" or "TredingSystem")    │
│                                                         │
│  Permissions: ✓ Full Access (default)                  │
│                                                         │
│  ┌──────────────────┐  ┌──────────────────┐           │
│  │  Cancel          │  │  Generate Token  │           │
│  └──────────────────┘  └──────────────────┘           │
│                              ↓ Click this              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 5: Token Generated
```
┌─────────────────────────────────────────────────────────┐
│  ✅ API Token Created Successfully                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Your API Token (copy and save securely):              │
│  ┌──────────────────────────────────────────────────┐  │
│  │  xkeysib-1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p      │  │
│  │                                                  │  │
│  │  [📋 Copy Button]                               │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ⚠️  IMPORTANT: Save this somewhere safe!             │
│      You won't see it again!                           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## ⚙️ Step 3: Configure Render

### Navigation to Environment
```
Render Dashboard (https://render.com/dashboard)
    ↓
Find Service: TredingSystem
    ↓
Click on service
    ↓
Environment (tab at top)
    ↓
Add environment variables
```

### Screen 1: Render Dashboard
```
┌─────────────────────────────────────────────────────────┐
│  Render Dashboard                                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Your Services:                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │                                                  │  │
│  │  🚀 TredingSystem (Web Service)                 │  │
│  │     https://tredingsystem.onrender.com          │  │
│  │     Click → Open                                 │  │
│  │                                                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 2: Service Page
```
┌─────────────────────────────────────────────────────────┐
│  TredingSystem                                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Tabs:                                                  │
│  [Overview] [Logs] [Events] [Environment] [Settings]   │
│                                                ↑        │
│                                           Click this    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 3: Environment Tab
```
┌─────────────────────────────────────────────────────────┐
│  Environment                                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Environment Variables:                                 │
│  ┌──────────────────────────────────────────────────┐  │
│  │                                                  │  │
│  │  DATABASE_URL      postgres://...              │  │
│  │  ASPNETCORE_URLS   http://localhost:5000       │  │
│  │  ...                                             │  │
│  │                                                  │  │
│  │  [+ Add Variable] Button (Blue)                 │  │
│  │                                                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 4: Add Variable 1
```
┌─────────────────────────────────────────────────────────┐
│  Add Environment Variable                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Key:                                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │ EmailProvider__Type                              │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  Value:                                                 │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Brevo                                            │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                   │
│  │  Add         │  │  Cancel      │                   │
│  └──────────────┘  └──────────────┘                   │
│        ↑                                                │
│    Click this                                           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 5: Add Variable 2
```
┌─────────────────────────────────────────────────────────┐
│  Add Environment Variable                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Key:                                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │ EmailProviders__Brevo__ApiKey                    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  Value:                                                 │
│  ┌──────────────────────────────────────────────────┐  │
│  │ xkeysib-1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p       │  │
│  └──────────────────────────────────────────────────┘  │
│  (Paste your API key from Step 2)                     │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                   │
│  │  Add         │  │  Cancel      │                   │
│  └──────────────┘  └──────────────┘                   │
│        ↑                                                │
│    Click this                                           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 6: Variables Added
```
┌─────────────────────────────────────────────────────────┐
│  Environment                                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Environment Variables:                                 │
│  ┌──────────────────────────────────────────────────┐  │
│  │                                                  │  │
│  │  DATABASE_URL      postgres://...              │  │
│  │  ASPNETCORE_URLS   http://localhost:5000       │  │
│  │  EmailProvider__Type          Brevo            │  │ ← New
│  │  EmailProviders__Brevo__ApiKey xkeysib-...    │  │ ← New
│  │  ...                                             │  │
│  │                                                  │  │
│  │  [Save Changes] Button (Red)                    │  │
│  │                                                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ⚠️  You must click Save Changes!                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 7: Redeploy Status
```
┌─────────────────────────────────────────────────────────┐
│  Overview                                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ⏳ Deploy in progress...                              │
│                                                         │
│  Service will restart with new environment variables   │
│                                                         │
│  Wait 2-3 minutes for completion                       │
│                                                         │
│  [View Events] to see progress                         │
│                                                         │
│  Expected: 🟢 Deploy Succeeded                         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🧪 Step 4: Test Email Delivery

### Screen 1: Sign Up Page
```
┌─────────────────────────────────────────────────────────┐
│  https://tredingsystem.onrender.com                     │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  TredingSystem Sign Up                            │  │
│  ├───────────────────────────────────────────────────┤  │
│  │                                                   │  │
│  │  Email:                                           │  │
│  │  ┌──────────────────────────────────────────────┐ │  │
│  │  │ yourtestemail@gmail.com                      │ │  │
│  │  └──────────────────────────────────────────────┘ │  │
│  │                                                   │  │
│  │  Password:                                        │  │
│  │  ┌──────────────────────────────────────────────┐ │  │
│  │  │ TestPass123!                                 │ │  │
│  │  └──────────────────────────────────────────────┘ │  │
│  │                                                   │  │
│  │  [Sign Up] Button                                │  │
│  │                                                   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 2: OTP Request
```
┌─────────────────────────────────────────────────────────┐
│  Verify Your Email                                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📧 We sent an OTP code to:                             │
│  yourtestemail@gmail.com                                │
│                                                         │
│  📩 Check your inbox for the code                       │
│                                                         │
│  Enter OTP:                                             │
│  ┌──────────────────────────────────────────────────┐  │
│  │ _ _ _ _ _ _                                      │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  [Verify] Button                                        │
│                                                         │
│  ⏱️  Expires in: 9:54 minutes                          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 3: Check Gmail Inbox
```
Gmail Inbox
├─ From: TredingSystem <noreply@tredingsystem.com>
│  Subject: 🔐 Your OTP Verification Code - TredingSystem
│  
│  Preview: 123456
│
│  ┌───────────────────────────────────────────────────┐
│  │ 🔐 TredingSystem                                 │
│  │ Email Verification                              │
│  │                                                 │
│  │ Your OTP Verification Code                      │
│  │                                                 │
│  │ Code: 123456                                    │
│  │                                                 │
│  │ ⏰ Expires in: 10 minutes                        │
│  │                                                 │
│  │ 🔒 This code is confidential                    │
│  │                                                 │
│  │ Thank you for using TredingSystem!              │
│  │                                                 │
│  └───────────────────────────────────────────────────┘
│
│  ← Click to open email
│    Copy code: 123456
```

### Screen 4: Paste Code
```
┌─────────────────────────────────────────────────────────┐
│  Verify Your Email                                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Enter OTP:                                             │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 1 2 3 4 5 6                                     │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  [Verify] Button                                        │
│       ↑                                                 │
│    Click this                                           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Screen 5: Success! 🎉
```
┌─────────────────────────────────────────────────────────┐
│  ✅ Email Verified Successfully                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Welcome to TredingSystem!                              │
│                                                         │
│  Your account has been created and verified.            │
│                                                         │
│  🎉 Brevo email delivery is working!                   │
│                                                         │
│  [Go to Dashboard] Button                               │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## ✅ Verification Checklist

```
□ Brevo account created
□ API key generated (xkeysib-...)
□ Render environment variables added (2 variables)
□ Render redeployed successfully (green checkmark)
□ Test email received in Gmail inbox
□ Email arrived < 5 seconds
□ OTP code visible in email
□ Code successfully verified
□ Account created and logged in

✅ COMPLETE! Brevo is working!
```

---

## 🎯 Summary

| Step | Action | Time |
|------|--------|------|
| 1 | Create Brevo account | 2 min |
| 2 | Generate API key | 2 min |
| 3 | Configure Render | 2 min |
| 4 | Wait for redeploy | 3 min |
| 5 | Test email | 1 min |
| **TOTAL** | **Setup Complete** | **10 min** |

---

**You now have email delivery working! 🚀**

