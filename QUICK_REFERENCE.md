# Email OTP Quick Reference

## 🚀 Deploy in 15 Minutes

### 1️⃣ Push Code (2 min)
```bash
git add .
git commit -m "Add email OTP verification"
git push origin main
# Render auto-deploys
```

### 2️⃣ Setup Mailtrap (5 min)
1. Visit https://mailtrap.io/
2. Sign up free
3. Create inbox "Trading System"
4. Copy SMTP credentials

### 3️⃣ Configure Render (2 min)
1. Go to Render dashboard
2. Select TredingSystem API
3. Settings → Environment
4. Add these 4 variables:
   ```
   Email__SmtpServer = smtp.mailtrap.io
   Email__SmtpPort = 587
   Email__Username = [from Mailtrap]
   Email__Password = [from Mailtrap]
   ```
5. Save & Deploy

### 4️⃣ Test (3 min)
1. Visit https://tredingsystem.netlify.app/register
2. Enter email → Send OTP
3. Check Mailtrap inbox for code
4. Enter code → Verify
5. Enter username/password → Sign Up
6. Login with new account ✅

## 📋 What Changed

### New Features
- ✅ Email verification with 6-digit OTP
- ✅ 10-minute OTP expiry
- ✅ Beautiful 3-step registration
- ✅ Automatic email sending
- ✅ Prevents duplicate registrations

### New Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/auth/send-otp` | Send OTP code to email |
| POST | `/auth/verify-otp` | Verify OTP code from email |
| POST | `/auth/register` | Create account (requires verified OTP) |

### New Database Table
```
otps
├── id (primary key)
├── email
├── code (6 digits)
├── created_at
├── expires_at
└── is_verified
```

## 🔑 Key Files

| File | Purpose |
|------|---------|
| `OtpEntity.cs` | Database entity |
| `OtpService.cs` | OTP business logic |
| `AuthController.cs` | API endpoints |
| `Register.razor` | 3-step registration UI |
| `AuthService.cs` | Frontend API client |

## ⚙️ Configuration

### Development (No Real Email)
- Leave SMTP credentials empty
- OTP codes logged to console
- Use logged code to test

### Production (Mailtrap)
```json
{
  "Email": {
    "SmtpServer": "smtp.mailtrap.io",
    "SmtpPort": 587,
    "Username": "your-mailtrap-username",
    "Password": "your-mailtrap-password"
  },
  "Otp": {
    "ExpiryMinutes": 10,
    "CodeLength": 6
  }
}
```

## 🧪 Test Cases

### Happy Path
```
1. Email: test@example.com
2. Send OTP → Receive: 123456
3. Verify OTP → Success
4. Username: testuser
5. Password: TestPass123
6. Sign Up → Success
7. Login → Success ✅
```

### Error Cases
```
Wrong Code → "Invalid OTP code"
Expired Code (>10 min) → "OTP has expired"
Already Registered → "Email is already registered"
Skip OTP → "Email must be verified"
```

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| No email received | Check Mailtrap inbox (not real email!) |
| "Credentials not configured" warning | Normal in dev. Set env vars for production |
| Code not working after 11+ minutes | OTP expires. Send new code |
| "Already registered" error | Use different email or wait 10 min |
| Email sending very slow | Check Render logs for SMTP errors |

## 📊 Monitoring

### Check Status
```bash
# Render logs
tail -f logs | grep -i "email\|otp"

# Mailtrap inbox
https://mailtrap.io/ → Your inbox
```

### Monitor Metrics
- Emails sent/day
- Successful verifications
- Failed verifications
- Average delivery time

## 🔒 Security

- ✅ 6-digit random codes (not predictable)
- ✅ 10-minute expiry time
- ✅ One OTP per email
- ✅ Auto-delete expired codes
- ✅ Email validation enforced
- ✅ HTTPS encrypted

## 📱 User Flow

```
Visit /register
    ↓
Enter email → Send OTP
    ↓
Receive 6-digit code in email
    ↓
Enter code → Verify OTP
    ↓
Enter username & password
    ↓
Complete Sign Up
    ↓
Redirected to /login
    ↓
Login successful ✅
```

## 🔗 Related Docs

- `OTP_IMPLEMENTATION.md` - Full technical details
- `OTP_PRODUCTION_SETUP.md` - Detailed Mailtrap setup
- `DEPLOYMENT_CHECKLIST.md` - Step-by-step deployment
- `SESSION_SUMMARY.md` - Complete session overview

## 💡 Tips

1. **Testing without emails**: Leave SMTP credentials empty, codes appear in console
2. **Mailtrap vs Production**: Mailtrap captures all emails (perfect for testing), real services send to user inboxes
3. **Dev vs Prod**: Same code works both ways - just different configuration
4. **Error debugging**: Check Render logs (email errors) + Mailtrap inbox (delivery status)
5. **Rate limiting**: Consider adding later to prevent spam

## ⏱️ Expected Timeline

| Task | Time |
|------|------|
| Code deployment | 2-3 min |
| Mailtrap setup | 5 min |
| Render config | 2 min |
| Testing | 3-5 min |
| **Total** | **~15 min** |

## ✅ Success Checklist

After deployment, verify:
- [ ] `/register` page loads
- [ ] Can send OTP
- [ ] Email arrives in Mailtrap
- [ ] Can verify OTP code
- [ ] Can complete registration
- [ ] New user can login
- [ ] Old users still login
- [ ] Admin dashboard works
- [ ] Feature flags work
- [ ] All other features work

## 🚨 Common Issues

### "The form data could not be sent"
- Check Render is running (Settings → Activity)
- Verify DATABASE_URL env var is set
- Check Render logs for errors

### "Connection refused"
- API service not running
- Check Render Settings → Event Log
- Restart service from Render dashboard

### "Email sent but didn't receive"
- Check Mailtrap inbox (not Gmail inbox!)
- Render logs show "Email sent successfully"
- In Mailtrap, see email delivery status
- Spam folder in Mailtrap (check Events tab)

### Infinite loop at email step
- Save & Deploy might not have completed
- Wait 2-3 minutes for restart
- Check Activity log in Render for status

## 📞 Support

For issues:
1. Check this Quick Reference first
2. Review OTP_IMPLEMENTATION.md for details
3. Check Render logs (Activity tab)
4. Check Mailtrap inbox for delivery status
5. Verify environment variables are saved

---

**Status**: ✅ Ready to Deploy

**Next Step**: Follow "🚀 Deploy in 15 Minutes" above
