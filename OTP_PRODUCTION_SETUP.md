# Email OTP Setup for Production (Render)

## Quick Setup (5 minutes)

### Option 1: Mailtrap (Recommended - Free, 500 emails/day)

1. **Sign up at Mailtrap.io**
   - Go to https://mailtrap.io/
   - Sign up for free account
   - Verify email

2. **Create Email Inbox**
   - Click "Inboxes" → "Create Inbox"
   - Name it "Trading System"
   - Save

3. **Get SMTP Credentials**
   - Go to Integrations → SMTP
   - Copy these values:
     ```
     Host: smtp.mailtrap.io
     Port: 587
     Username: [copy from Mailtrap]
     Password: [copy from Mailtrap]
     ```

4. **Set Environment Variables on Render**
   - Go to render.com dashboard
   - Select "TredingSystem API" service
   - Settings → Environment
   - Add these variables:
     ```
     Email__SmtpServer=smtp.mailtrap.io
     Email__SmtpPort=587
     Email__Username=<paste Mailtrap username>
     Email__Password=<paste Mailtrap password>
     Email__SenderEmail=noreply@tredingsystem.com
     Email__SenderName=TredingSystem
     ```
   - Click Save & Deploy

5. **Test Email Sending**
   - Go to `/register` page
   - Enter any test email
   - Click "Send OTP"
   - Check Mailtrap inbox (not your real inbox!)
   - You'll see the test email there

### Option 2: SendGrid (Free tier, 40,000 emails/month)

1. **Sign up at SendGrid**
   - Go to https://sendgrid.com/
   - Sign up free account
   - Verify email

2. **Create API Key**
   - Settings → API Keys
   - Create new API key
   - Copy the key (starts with SG.)

3. **Set on Render** (Note: SendGrid requires code changes - use Mailtrap instead for now)

### Option 3: Development (No Real Email)

If you want to test without sending real emails:
1. Leave `Email__Username` and `Email__Password` empty
2. OTP codes will appear in Render logs
3. Use those codes to test the flow

## Verification Checklist

After setup, verify everything works:

```bash
# 1. SSH into Render (or check logs)
# 2. Register for new account
# 3. Should see: "OTP sent to your email"

# 4. Go to Mailtrap inbox (not real email!)
# 5. You should see email with 6-digit code

# 6. Copy code from email
# 7. Enter code in verification step
# 8. Complete registration

# 9. Login with new account - should work!
```

## Monitoring & Logs

**View Email Sending Logs:**
1. Go to Render dashboard
2. Select TradingSystem API
3. Logs tab
4. Search for "Email sent successfully" or "Error sending email"

**View Email Bounces:**
1. Go to Mailtrap dashboard
2. Inboxes → Your inbox
3. All emails are captured here (testing + production)

## Troubleshooting

### No email received (but no error in logs)
1. Check Mailtrap inbox (emails go there, not real inbox)
2. Wait 2-3 seconds, refresh inbox
3. Check spam folder in Mailtrap

### "SMTP connection failed" error
1. Verify SMTP server address: `smtp.mailtrap.io`
2. Verify port is 587 (not 25 or 465)
3. Check credentials are copied correctly
4. Make sure environment variables are saved

### Email address already registered
1. Use a different email
2. Or use same email but wait for OTP to expire (10 min)

### Production emails going to Mailtrap instead of real inbox
**This is normal!** Mailtrap intercepts ALL emails. To test with real emails:
1. Create SendGrid account (requires code changes)
2. Or manually mark OTPs as verified in database for testing

## Important Notes

⚠️ **DO NOT commit real email credentials to GitHub!**
- Always use environment variables on Render
- Never put credentials in code or appsettings.json
- Check `.gitignore` for sensitive files

✅ **Why Mailtrap for Testing**
- 500 free emails/day
- No real emails sent (all captured in Mailtrap)
- Perfect for testing, staging, demo
- Prevents accidental emails to real users during testing

✅ **Best Practice for Production**
- Mailtrap: Great for demo/testing on deployed site
- SendGrid/AWS SES: For real production emails to actual users
- Both require paid plans for production volume

## Next Steps

1. Deploy this code to Render
2. Set up Mailtrap (5 minutes)
3. Add environment variables (2 minutes)
4. Restart API service on Render (1 minute)
5. Test registration flow at https://tredingsystem.netlify.app/register

Total setup time: ~10 minutes

Questions? Check `OTP_IMPLEMENTATION.md` for detailed documentation.
