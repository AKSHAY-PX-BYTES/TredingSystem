// Idle Timeout Manager - Auto logout after 5 minutes of inactivity
window.IdleTimeoutManager = {
    timeoutId: null,
    countdownId: null,
    timeoutDuration: 5 * 60 * 1000, // 5 minutes in milliseconds
    remainingSeconds: 300,
    dotNetRef: null,
    isActive: false,

    init: function (dotNetReference, timeoutMinutes) {
        try {
            this.dotNetRef = dotNetReference;
            this.timeoutDuration = (timeoutMinutes || 5) * 60 * 1000;
            this.remainingSeconds = Math.floor(this.timeoutDuration / 1000);
            this.isActive = true;
            
            // Track user activity - only reset on actual user actions, not mousemove
            const events = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click'];
            events.forEach(event => {
                document.addEventListener(event, () => this.resetTimer(), { passive: true });
            });

            this.resetTimer();
            this.startCountdown();
            console.log('[IdleTimeout] ✅ Initialized with ' + (this.timeoutDuration / 60000) + ' minute timeout');
        } catch (err) {
            console.error('[IdleTimeout] ❌ Init failed:', err);
        }
    },

    startCountdown: function () {
        if (this.countdownId) clearInterval(this.countdownId);
        
        this.countdownId = setInterval(() => {
            if (!this.isActive) {
                clearInterval(this.countdownId);
                return;
            }

            this.remainingSeconds--;

            if (this.remainingSeconds <= 0) {
                clearInterval(this.countdownId);
                this.onTimeout();
                return;
            }
        }, 1000);
    },

    resetTimer: function () {
        if (!this.isActive) return;

        // Reset countdown
        this.remainingSeconds = Math.floor(this.timeoutDuration / 1000);

        // Clear existing timeout
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
        }

        // Set new timeout as backup
        this.timeoutId = setTimeout(() => this.onTimeout(), this.timeoutDuration + 1000);
    },

    onTimeout: function () {
        if (!this.isActive || !this.dotNetRef) return;

        console.log('[IdleTimeout] User inactive, triggering logout');
        this.stop();
        
        // Call .NET method to logout
        this.dotNetRef.invokeMethodAsync('OnIdleTimeout')
            .catch(err => console.error('[IdleTimeout] Error invoking logout:', err));
    },

    stop: function () {
        this.isActive = false;
        
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
            this.timeoutId = null;
        }
        
        if (this.countdownId) {
            clearInterval(this.countdownId);
            this.countdownId = null;
        }

        // Remove timer display
        const timer = document.getElementById('idle-timer-display');
        if (timer) timer.remove();
        
        this.dotNetRef = null;
        console.log('[IdleTimeout] Stopped');
    },

    // Get remaining time in seconds
    getRemainingSeconds: function () {
        return this.remainingSeconds;
    }
};

// Session Expired Handler — shows popup when API returns 401
window.SessionExpiredHandler = {
    isShowing: false,
    dotNetRef: null,

    init: function (dotNetReference) {
        this.dotNetRef = dotNetReference;
        console.log('[SessionExpired] Handler initialized');
    },

    trigger: function () {
        if (this.isShowing) return;
        this.isShowing = true;

        // Create overlay
        const overlay = document.createElement('div');
        overlay.id = 'session-expired-overlay';
        overlay.style.cssText = `
            position: fixed; top: 0; left: 0; width: 100%; height: 100%;
            background: rgba(0,0,0,0.7); z-index: 9999999;
            display: flex; align-items: center; justify-content: center;
            backdrop-filter: blur(4px);
        `;

        overlay.innerHTML = `
            <div style="
                background: var(--card-bg, #1c2333); border: 1px solid var(--border-color, #2d3748);
                border-radius: 16px; padding: 2rem; max-width: 420px; width: 90%;
                text-align: center; box-shadow: 0 20px 60px rgba(0,0,0,0.5);
            ">
                <div style="font-size: 3rem; margin-bottom: 1rem;">⏰</div>
                <h2 style="color: var(--text-primary, #fff); margin: 0 0 0.5rem 0; font-size: 1.4rem;">Session Expired</h2>
                <p style="color: var(--text-muted, #8b949e); margin: 0 0 1.5rem 0; font-size: 0.95rem; line-height: 1.5;">
                    Your session has expired due to token timeout.<br/>Do you want to continue?
                </p>
                <div style="display: flex; gap: 1rem; justify-content: center;">
                    <button id="session-continue-btn" style="
                        padding: 0.75rem 2rem; background: #00d09c; color: white;
                        border: none; border-radius: 8px; font-weight: 600; font-size: 0.95rem;
                        cursor: pointer; transition: opacity 0.2s;
                    " onmouseover="this.style.opacity='0.85'" onmouseout="this.style.opacity='1'">
                        ✅ Yes, Continue
                    </button>
                    <button id="session-logout-btn" style="
                        padding: 0.75rem 2rem; background: transparent; color: #ef4444;
                        border: 2px solid #ef4444; border-radius: 8px; font-weight: 600; font-size: 0.95rem;
                        cursor: pointer; transition: all 0.2s;
                    " onmouseover="this.style.background='#ef4444';this.style.color='white'" 
                      onmouseout="this.style.background='transparent';this.style.color='#ef4444'">
                        🚪 No, Logout
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(overlay);

        document.getElementById('session-continue-btn').addEventListener('click', () => {
            this.handleContinue();
        });

        document.getElementById('session-logout-btn').addEventListener('click', () => {
            this.handleLogout();
        });
    },

    handleContinue: function () {
        this.removeOverlay();
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnSessionRefresh')
                .catch(err => console.error('[SessionExpired] Refresh failed:', err));
        }
    },

    handleLogout: function () {
        this.removeOverlay();
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnSessionLogout')
                .catch(err => console.error('[SessionExpired] Logout failed:', err));
        }
    },

    removeOverlay: function () {
        const overlay = document.getElementById('session-expired-overlay');
        if (overlay) overlay.remove();
        this.isShowing = false;
    }
};
