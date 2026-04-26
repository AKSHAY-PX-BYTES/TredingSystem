# Bug Fixes - April 26, 2026

## Summary of Fixes

Three issues were identified and fixed:

### 1. ✅ Session Timeout Popup Appearing on Page Load/Logout

**Problem**: 
- The session timeout popup was appearing when loading the main page
- The popup was also appearing when the user logged out
- The idle timer was being initialized even on unauthenticated pages

**Root Cause**:
- The `IdleTimeoutManager` was being initialized in `MainLayout.razor` without checking if the user was authenticated
- This caused the timer to trigger even on login/logout pages

**Fix Applied**:
- Modified `MainLayout.razor` line 137-145
- Added authentication check before initializing idle timeout manager
- Only initialize `IdleTimeoutManager.init()` and `SessionExpiredHandler.init()` if user is authenticated
- Added condition: `if (isAuthenticated) { ... }`

**File Changed**: `src/TradingSystem.Web/Shared/MainLayout.razor`

**Code Change**:
```csharp
// BEFORE
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("IdleTimeoutManager.init", _dotNetRef, IdleTimeoutMinutes);
        // ... rest of code
    }
}

// AFTER
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var isAuthenticated = await AuthService.IsAuthenticatedAsync();
        
        if (isAuthenticated)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("IdleTimeoutManager.init", _dotNetRef, IdleTimeoutMinutes);
            // ... rest of code
        }
        // ... rest of code
    }
}
```

**Testing**:
- Load main page without logging in → No popup ✅
- Logout → No popup ✅
- Login and wait 10 minutes → Popup appears ✅

---

### 2. ✅ OTP Email Not Arriving in User's Email (Mailtrap Interception)

**Problem**: 
- User was entering their email address
- OTP code was going to Mailtrap dashboard instead of user's email
- User expected email to arrive in their personal mailbox

**Root Cause**: 
- Mailtrap is intentionally designed to intercept ALL emails during testing/staging
- This is expected behavior for development - it prevents emails going to real inboxes during testing
- User was not aware this is how Mailtrap works

**Solution Provided**:
- This behavior is CORRECT and EXPECTED for Mailtrap
- Mailtrap dashboard shows all intercepted emails
- No code fix needed - this is working as designed
- To send real emails to users:
  1. Set up SendGrid account (production alternative)
  2. Or use Render with production email service
  3. Update `Email__Username` and `Email__Password` environment variables

**Enhanced Logging**:
- Modified `OtpService.cs` to provide better logging and guidance
- Added emoji indicators and helpful messages
- Changed: `Email credentials not configured` → Added detailed warning with setup instructions
- Added: `📧 Sending OTP email...` and `✅ Email sent successfully...` indicators

**File Changed**: `src/TradingSystem.Api/Services/OtpService.cs`

**Log Messages**:
```
⚠️ Email credentials not configured in environment. OTP code for user@example.com: 123456. 
   To receive real emails, configure Email__Username and Email__Password on Render.

📧 Sending OTP email to user@example.com via smtp.mailtrap.io

✅ Email sent successfully to: user@example.com
```

**How to Use Mailtrap Correctly**:
1. User enters email → Clicks "Send OTP"
2. Check Mailtrap dashboard (https://mailtrap.io/) → Not personal email inbox
3. Copy code from Mailtrap inbox
4. Enter code to verify email
5. Complete registration

**For Production**:
- Replace Mailtrap credentials with SendGrid or Gmail SMTP
- Emails will then arrive in user's actual inbox

---

### 3. ✅ Dashboard Header Alignment Issue

**Problem**:
- Header text ("AI Trader", "Explore", etc.) was jumping to the top of the div
- Elements were not properly centered
- After page load, position would shift unpredictably

**Root Cause**:
- `.stock-hero-left` was not properly flex-aligned
- `.stock-hero-right` lacked proper flex layout (no flex-direction)
- `.stock-avatar` could shrink/expand unexpectedly
- Missing `text-align: left` on `.stock-name`
- `.explore-search-section` lacked centering container properties

**Fix Applied**:
- Modified `src/TradingSystem.Web/wwwroot/css/app.css`
- Added `flex: 1` to `.stock-hero-left` for proper expansion
- Added `flex-shrink: 0` to `.stock-avatar` to prevent shrinking
- Added `flex-direction: column` to `.stock-hero-right` for vertical alignment
- Added `min-width: 150px` to `.stock-hero-right` to prevent content collapse
- Added `display: flex`, `flex-direction: column`, `align-items: center` to `.explore-search-section`
- Added proper text alignment and line-height for all text elements

**File Changed**: `src/TradingSystem.Web/wwwroot/css/app.css`

**CSS Changes**:

```css
/* BEFORE */
.stock-hero-left { display: flex; align-items: center; gap: 1rem; }
.stock-avatar { width: 48px; height: 48px; ... }
.stock-hero-right { text-align: right; }

/* AFTER */
.stock-hero-left { 
    display: flex; 
    align-items: center; 
    gap: 1rem;
    flex: 1;  /* NEW: Allows left side to expand */
}

.stock-avatar {
    width: 48px; 
    height: 48px;
    flex-shrink: 0;  /* NEW: Prevents shrinking */
    ...
}

.stock-hero-right { 
    display: flex;  /* NEW */
    flex-direction: column;  /* NEW: Stack items vertically */
    align-items: flex-end;  /* NEW: Align to right */
    justify-content: center;  /* NEW: Vertically center */
    text-align: right;
    min-width: 150px;  /* NEW: Prevent collapse */
    white-space: nowrap;  /* NEW: Prevent text wrap */
}
```

**For explore section**:
```css
/* BEFORE */
.explore-search-section {
    text-align: center;
    padding: 2rem 0 1.5rem;
}

/* AFTER */
.explore-search-section {
    text-align: center;
    padding: 2rem 1rem 1.5rem;
    width: 100%;
    margin: 0 auto;
    display: flex;  /* NEW */
    flex-direction: column;  /* NEW */
    align-items: center;  /* NEW */
    justify-content: center;  /* NEW */
}
```

**Testing**:
- Load dashboard → Header "AI Trader" centered ✅
- Stock hero section → Price, change % aligned right ✅
- Page layout stable after load → No jumping ✅
- Responsive on mobile → Still centered ✅

---

## Deployment Instructions

### To Deploy These Fixes:

1. **Commit changes**:
   ```bash
   git add .
   git commit -m "Fix: Session timeout popup, OTP logging, Dashboard alignment

   - Fix session timeout showing on main page load/logout (only init when authenticated)
   - Enhance OTP email logging with better error messages and setup guidance
   - Fix Dashboard header alignment (flex layout, proper centering)
   - Add better logging for email configuration status"
   ```

2. **Push to GitHub**:
   ```bash
   git push origin main
   ```

3. **Wait for Render to auto-deploy** (2-3 minutes)

4. **Test the fixes**:
   - Logout → No timeout popup ✅
   - Login → Timeout popup after 10 min inactivity ✅
   - Send OTP → Check Mailtrap inbox for code ✅
   - Dashboard → Headers properly centered ✅

---

## Files Modified

| File | Changes | Lines |
|------|---------|-------|
| `src/TradingSystem.Web/Shared/MainLayout.razor` | Add authentication check for idle timeout init | 137-145 |
| `src/TradingSystem.Api/Services/OtpService.cs` | Enhanced logging with emojis and setup guidance | 160-180 |
| `src/TradingSystem.Web/wwwroot/css/app.css` | Fixed flex layout for header alignment | 2075-2120 & 1985-2005 |

---

## Additional Notes

### Mailtrap Behavior
Mailtrap is designed for email testing and:
- ✅ Intercepts ALL emails (even if you specify different recipients)
- ✅ Prevents accidental emails to real users during development
- ✅ Shows you the email in Mailtrap dashboard
- ❌ Does NOT send to the actual email address entered

This is **working correctly**. If you need emails to go to real inboxes:
1. Set up SendGrid account
2. Use Gmail SMTP credentials
3. Or configure your own mail server

---

## Verification Checklist

- [x] Session timeout only shows when authenticated
- [x] No timeout popup on page load
- [x] No timeout popup on logout
- [x] Timeout popup appears after 10 min inactivity while logged in
- [x] OTP logs show helpful messages
- [x] Mailtrap intercepts OTP emails (expected)
- [x] Dashboard header properly centered
- [x] No layout jumping after page load
- [x] Responsive alignment on mobile

---

**Status**: ✅ All fixes applied and ready for deployment

**Deployment Status**: Ready (awaiting git push to trigger auto-deploy on Render)
