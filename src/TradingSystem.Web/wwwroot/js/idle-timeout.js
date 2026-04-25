// Idle Timeout Manager - Auto logout after 5 minutes of inactivity
window.IdleTimeoutManager = {
    timeoutId: null,
    countdownId: null,
    timeoutDuration: 5 * 60 * 1000, // 5 minutes in milliseconds
    remainingSeconds: 300,
    dotNetRef: null,
    isActive: false,
    timerElement: null,

    init: function (dotNetReference, timeoutMinutes) {
        try {
            this.dotNetRef = dotNetReference;
            this.timeoutDuration = (timeoutMinutes || 5) * 60 * 1000;
            this.remainingSeconds = Math.floor(this.timeoutDuration / 1000);
            this.isActive = true;
            
            // Create timer display element
            this.createTimerDisplay();
            
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

    createTimerDisplay: function () {
        // Remove existing timer if any
        const existing = document.getElementById('idle-timer-display');
        if (existing) existing.remove();

        // Create timer element — will be placed in navbar
        const timer = document.createElement('div');
        timer.id = 'idle-timer-display';
        timer.innerHTML = `
            <span style="margin-right: 4px;">⏱️</span>
            <strong id="idle-timer-countdown" style="color: #00d09c;">05:00</strong>
        `;
        timer.style.cssText = `
            display: flex !important;
            align-items: center !important;
            gap: 2px !important;
            background: rgba(0,208,156,0.1) !important;
            border: 1px solid rgba(0,208,156,0.3) !important;
            border-radius: 8px !important;
            padding: 6px 12px !important;
            font-size: 12px !important;
            font-weight: 600 !important;
            color: #c9d1d9 !important;
            font-family: 'Inter', monospace !important;
            white-space: nowrap !important;
        `;

        // Try to insert into navbar-right, before the Sign Out button
        const navRight = document.querySelector('.navbar-right');
        const signOutBtn = document.querySelector('.logout-pill');
        if (navRight && signOutBtn) {
            navRight.insertBefore(timer, signOutBtn);
        } else if (navRight) {
            navRight.appendChild(timer);
        } else {
            // Fallback: fixed position top-right
            timer.style.cssText += `
                position: fixed !important;
                top: 12px !important;
                right: 120px !important;
                z-index: 999999 !important;
            `;
            document.body.appendChild(timer);
        }

        this.timerElement = document.getElementById('idle-timer-countdown');
        console.log('[IdleTimeout] Timer display created in navbar');
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

            this.updateDisplay();
        }, 1000);
    },

    updateDisplay: function () {
        if (!this.timerElement) {
            this.timerElement = document.getElementById('idle-timer-countdown');
            if (!this.timerElement) return;
        }

        const mins = Math.floor(this.remainingSeconds / 60);
        const secs = this.remainingSeconds % 60;
        const timeStr = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        this.timerElement.textContent = timeStr;

        // Change color based on remaining time
        const container = document.getElementById('idle-timer-display');
        if (container) {
            if (this.remainingSeconds <= 60) {
                container.style.borderColor = '#ef4444';
                container.style.background = 'rgba(239,68,68,0.1)';
                this.timerElement.style.color = '#ef4444';
            } else if (this.remainingSeconds <= 120) {
                container.style.borderColor = '#fbbf24';
                container.style.background = 'rgba(251,191,36,0.1)';
                this.timerElement.style.color = '#fbbf24';
            } else {
                container.style.borderColor = 'rgba(0,208,156,0.3)';
                container.style.background = 'rgba(0,208,156,0.1)';
                this.timerElement.style.color = '#00d09c';
            }
        }
    },

    resetTimer: function () {
        if (!this.isActive) return;

        // Reset countdown
        this.remainingSeconds = Math.floor(this.timeoutDuration / 1000);
        this.updateDisplay();

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
