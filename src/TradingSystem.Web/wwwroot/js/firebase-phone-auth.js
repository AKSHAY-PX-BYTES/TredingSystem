// Firebase Phone Authentication for Blazor WASM
// Free tier: 10,000 verifications/month — no credit card needed
window.FirebasePhoneAuth = {
    app: null,
    auth: null,
    recaptchaVerifier: null,
    confirmationResult: null,
    isInitialized: false,

    // Step 1: Initialize Firebase with your project config
    init: function (firebaseConfig) {
        try {
            if (this.isInitialized) {
                console.log('[Firebase] Already initialized');
                return true;
            }

            // Parse config if it's a string
            if (typeof firebaseConfig === 'string') {
                firebaseConfig = JSON.parse(firebaseConfig);
            }

            this.app = firebase.initializeApp(firebaseConfig);
            this.auth = firebase.auth();

            // Use device language for SMS
            this.auth.useDeviceLanguage();

            this.isInitialized = true;
            console.log('[Firebase] ✅ Initialized successfully');
            return true;
        } catch (err) {
            console.error('[Firebase] ❌ Init failed:', err);
            return false;
        }
    },

    // Step 2: Setup invisible reCAPTCHA (required by Firebase)
    setupRecaptcha: function (buttonId) {
        try {
            // Clear any existing verifier
            if (this.recaptchaVerifier) {
                this.recaptchaVerifier.clear();
                this.recaptchaVerifier = null;
            }

            // Remove any existing recaptcha container
            const existing = document.getElementById('recaptcha-container');
            if (existing) existing.remove();

            // Create a hidden container for reCAPTCHA
            const container = document.createElement('div');
            container.id = 'recaptcha-container';
            container.style.display = 'none';
            document.body.appendChild(container);

            this.recaptchaVerifier = new firebase.auth.RecaptchaVerifier('recaptcha-container', {
                size: 'invisible',
                callback: function (response) {
                    console.log('[Firebase] reCAPTCHA solved');
                }
            });

            console.log('[Firebase] ✅ reCAPTCHA setup complete');
            return true;
        } catch (err) {
            console.error('[Firebase] ❌ reCAPTCHA setup failed:', err);
            return false;
        }
    },

    // Step 3: Send OTP to phone number
    sendOtp: async function (phoneNumber) {
        try {
            if (!this.auth) {
                return { success: false, error: 'Firebase not initialized' };
            }

            // Ensure reCAPTCHA is set up
            if (!this.recaptchaVerifier) {
                this.setupRecaptcha();
            }

            console.log('[Firebase] Sending OTP to:', phoneNumber.substring(0, 4) + '****');

            this.confirmationResult = await this.auth.signInWithPhoneNumber(
                phoneNumber,
                this.recaptchaVerifier
            );

            console.log('[Firebase] ✅ OTP sent successfully');
            return { success: true, message: 'OTP sent to your phone number' };

        } catch (err) {
            console.error('[Firebase] ❌ Send OTP failed:', err);

            // Reset reCAPTCHA on error so user can retry
            if (this.recaptchaVerifier) {
                try { this.recaptchaVerifier.clear(); } catch (_) { }
                this.recaptchaVerifier = null;
            }

            let errorMsg = 'Failed to send OTP';
            switch (err.code) {
                case 'auth/invalid-phone-number':
                    errorMsg = 'Invalid phone number format. Use format: +CountryCodeNumber';
                    break;
                case 'auth/too-many-requests':
                    errorMsg = 'Too many attempts. Please try again later.';
                    break;
                case 'auth/captcha-check-failed':
                    errorMsg = 'reCAPTCHA verification failed. Please try again.';
                    break;
                case 'auth/quota-exceeded':
                    errorMsg = 'SMS quota exceeded. Please try again tomorrow.';
                    break;
                case 'auth/missing-phone-number':
                    errorMsg = 'Please enter a phone number.';
                    break;
                default:
                    errorMsg = err.message || 'Failed to send OTP';
            }

            return { success: false, error: errorMsg };
        }
    },

    // Step 4: Verify OTP code
    verifyOtp: async function (code) {
        try {
            if (!this.confirmationResult) {
                return { success: false, error: 'No OTP was sent. Please request a new one.' };
            }

            const result = await this.confirmationResult.confirm(code);

            console.log('[Firebase] ✅ Phone verified! UID:', result.user.uid);

            // Sign out from Firebase auth (we only need phone verification, not Firebase session)
            await this.auth.signOut();

            return {
                success: true,
                message: 'Phone number verified successfully!',
                uid: result.user.uid,
                phone: result.user.phoneNumber
            };

        } catch (err) {
            console.error('[Firebase] ❌ Verify OTP failed:', err);

            let errorMsg = 'Invalid OTP code';
            switch (err.code) {
                case 'auth/invalid-verification-code':
                    errorMsg = 'Invalid OTP code. Please check and try again.';
                    break;
                case 'auth/code-expired':
                    errorMsg = 'OTP has expired. Please request a new one.';
                    break;
                default:
                    errorMsg = err.message || 'Verification failed';
            }

            return { success: false, error: errorMsg };
        }
    },

    // Cleanup
    cleanup: function () {
        if (this.recaptchaVerifier) {
            try { this.recaptchaVerifier.clear(); } catch (_) { }
            this.recaptchaVerifier = null;
        }
        this.confirmationResult = null;
        const container = document.getElementById('recaptcha-container');
        if (container) container.remove();
    }
};
