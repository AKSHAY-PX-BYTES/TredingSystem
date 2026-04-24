// Idle Timeout Manager - Auto logout after 5 minutes of inactivity
window.IdleTimeoutManager = {
    timeoutId: null,
    timeoutDuration: 5 * 60 * 1000, // 5 minutes in milliseconds
    dotNetRef: null,
    isActive: false,

    init: function (dotNetReference, timeoutMinutes) {
        this.dotNetRef = dotNetReference;
        this.timeoutDuration = (timeoutMinutes || 5) * 60 * 1000;
        this.isActive = true;
        
        // Track user activity
        const events = ['mousedown', 'mousemove', 'keydown', 'scroll', 'touchstart', 'click'];
        events.forEach(event => {
            document.addEventListener(event, () => this.resetTimer(), { passive: true });
        });

        this.resetTimer();
        console.log('[IdleTimeout] Initialized with ' + (this.timeoutDuration / 60000) + ' minute timeout');
    },

    resetTimer: function () {
        if (!this.isActive) return;

        // Clear existing timeout
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
        }

        // Set new timeout
        this.timeoutId = setTimeout(() => this.onTimeout(), this.timeoutDuration);
    },

    onTimeout: function () {
        if (!this.isActive || !this.dotNetRef) return;

        console.log('[IdleTimeout] User inactive for ' + (this.timeoutDuration / 60000) + ' minutes, triggering logout');
        
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
        this.dotNetRef = null;
        console.log('[IdleTimeout] Stopped');
    },

    // Get remaining time in seconds (for UI display if needed)
    getRemainingSeconds: function () {
        return Math.ceil(this.timeoutDuration / 1000);
    }
};
