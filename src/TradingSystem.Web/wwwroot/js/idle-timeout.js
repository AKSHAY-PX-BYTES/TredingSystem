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
        this.dotNetRef = dotNetReference;
        this.timeoutDuration = (timeoutMinutes || 5) * 60 * 1000;
        this.remainingSeconds = Math.floor(this.timeoutDuration / 1000);
        this.isActive = true;
        
        // Create timer display element
        this.createTimerDisplay();
        
        // Track user activity
        const events = ['mousedown', 'mousemove', 'keydown', 'scroll', 'touchstart', 'click'];
        events.forEach(event => {
            document.addEventListener(event, () => this.resetTimer(), { passive: true });
        });

        this.resetTimer();
        this.startCountdown();
        console.log('[IdleTimeout] Initialized with ' + (this.timeoutDuration / 60000) + ' minute timeout');
    },

    createTimerDisplay: function () {
        // Remove existing timer if any
        const existing = document.getElementById('idle-timer-display');
        if (existing) existing.remove();

        // Create timer element
        const timer = document.createElement('div');
        timer.id = 'idle-timer-display';
        timer.innerHTML = `
            <div class="idle-timer-inner">
                <span class="idle-timer-icon">⏱️</span>
                <span class="idle-timer-text">Session: <strong id="idle-timer-countdown">05:00</strong></span>
            </div>
        `;
        timer.style.cssText = `
            position: fixed;
            bottom: 20px;
            right: 20px;
            background: rgba(13, 17, 23, 0.95);
            border: 1px solid rgba(0, 208, 156, 0.3);
            border-radius: 8px;
            padding: 8px 14px;
            font-size: 12px;
            color: #8b949e;
            z-index: 9999;
            display: flex;
            align-items: center;
            gap: 6px;
            backdrop-filter: blur(10px);
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            transition: all 0.3s ease;
        `;
        document.body.appendChild(timer);
        this.timerElement = document.getElementById('idle-timer-countdown');
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
        if (!this.timerElement) return;

        const mins = Math.floor(this.remainingSeconds / 60);
        const secs = this.remainingSeconds % 60;
        const timeStr = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        this.timerElement.textContent = timeStr;

        // Change color when less than 1 minute
        const container = document.getElementById('idle-timer-display');
        if (container) {
            if (this.remainingSeconds <= 60) {
                container.style.borderColor = 'rgba(239, 68, 68, 0.5)';
                this.timerElement.style.color = '#ef4444';
            } else if (this.remainingSeconds <= 120) {
                container.style.borderColor = 'rgba(251, 191, 36, 0.5)';
                this.timerElement.style.color = '#fbbf24';
            } else {
                container.style.borderColor = 'rgba(0, 208, 156, 0.3)';
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
