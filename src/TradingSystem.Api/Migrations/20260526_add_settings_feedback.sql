-- Migration: Add user settings, feedback, and profile fields
-- Run this on your PostgreSQL database (Neon.tech)

-- New columns on users table
ALTER TABLE users ADD COLUMN IF NOT EXISTS first_name VARCHAR(50);
ALTER TABLE users ADD COLUMN IF NOT EXISTS last_name VARCHAR(50);
ALTER TABLE users ADD COLUMN IF NOT EXISTS country VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMP;
ALTER TABLE users ADD COLUMN IF NOT EXISTS trading_experience VARCHAR(20);

-- Legal consents
ALTER TABLE users ADD COLUMN IF NOT EXISTS consent_financial_risk BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS consent_terms BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS consent_privacy BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS consent_ai_signals BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS consented_at TIMESTAMP;

-- Security
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_changed_at TIMESTAMP;
ALTER TABLE users ADD COLUMN IF NOT EXISTS recovery_email VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS mfa_enabled BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS mfa_secret VARCHAR(200);
ALTER TABLE users ADD COLUMN IF NOT EXISTS session_token VARCHAR(200);
ALTER TABLE users ADD COLUMN IF NOT EXISTS session_token_issued_at TIMESTAMP;

-- Preferences
ALTER TABLE users ADD COLUMN IF NOT EXISTS theme VARCHAR(10) DEFAULT 'system';
ALTER TABLE users ADD COLUMN IF NOT EXISTS notify_whats_new BOOLEAN DEFAULT TRUE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS notify_recommendations BOOLEAN DEFAULT TRUE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS notify_email_updates BOOLEAN DEFAULT TRUE;

-- Account deletion
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP;
ALTER TABLE users ADD COLUMN IF NOT EXISTS deletion_reason VARCHAR(500);

-- Feedbacks table
CREATE TABLE IF NOT EXISTS feedbacks (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    type VARCHAR(20) NOT NULL DEFAULT 'Suggestion',
    subject VARCHAR(200) NOT NULL,
    message VARCHAR(5000) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Open',
    admin_response VARCHAR(5000),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    responded_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_feedbacks_user_id ON feedbacks(user_id);
CREATE INDEX IF NOT EXISTS idx_feedbacks_status ON feedbacks(status);
CREATE INDEX IF NOT EXISTS idx_feedbacks_created_at ON feedbacks(created_at);

-- Set password_changed_at for existing users (default to created_at)
UPDATE users SET password_changed_at = created_at WHERE password_changed_at IS NULL;
UPDATE users SET session_token = md5(random()::text) WHERE session_token IS NULL;
UPDATE users SET session_token_issued_at = NOW() WHERE session_token_issued_at IS NULL;
