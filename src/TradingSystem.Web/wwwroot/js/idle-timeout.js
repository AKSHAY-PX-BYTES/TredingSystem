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

        // Create timer element
        const timer = document.createElement('div');
        timer.id = 'idle-timer-display';
        timer.innerHTML = `
            <span style="margin-right: 6px;">⏱️</span>
            <span>Session: </span>
            <strong id="idle-timer-countdown" style="color: #00d09c; margin-left: 4px;">05:00</strong>
        `;
        timer.style.cssText = `
            position: fixed !important;
            bottom: 80px !important;
            right: 20px !important;
            background: #161b22 !important;
            border: 2px solid #00d09c !important;
            border-radius: 10px !important;
            padding: 10px 16px !important;
            font-size: 13px !important;
            font-weight: 500 !important;
            color: #c9d1d9 !important;
            z-index: 999999 !important;
            display: flex !important;
            align-items: center !important;
            box-shadow: 0 4px 20px rgba(0,0,0,0.5) !important;
            font-family: 'Inter', sans-serif !important;
        `;
        document.body.appendChild(timer);
        this.timerElement = document.getElementById('idle-timer-countdown');
        console.log('[IdleTimeout] Timer display created');
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

        // Change color when less than 1 minute
        const container = document.getElementById('idle-timer-display');
        if (container) {
            if (this.remainingSeconds <= 60) {
                container.style.borderColor = '#ef4444';
                this.timerElement.style.color = '#ef4444';
            } else if (this.remainingSeconds <= 120) {
                container.style.borderColor = '#fbbf24';
                this.timerElement.style.color = '#fbbf24';
            } else {
                container.style.borderColor = '#00d09c';
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
