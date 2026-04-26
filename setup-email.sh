#!/bin/bash
# Email OTP Configuration Setup Script for TredingSystem
# This script helps configure email delivery for OTP verification

echo "🔐 TredingSystem - Email OTP Configuration Setup"
echo "=================================================="
echo ""
echo "Choose your email service provider:"
echo ""
echo "1) Mailtrap (Recommended for Testing)"
echo "2) Gmail (Production)"
echo "3) SendGrid (Enterprise)"
echo "4) Exit"
echo ""
read -p "Select option (1-4): " choice

case $choice in
    1)
        echo ""
        echo "📧 Mailtrap Setup"
        echo "=================="
        echo ""
        echo "1. Go to https://mailtrap.io"
        echo "2. Sign up or log in"
        echo "3. Go to 'Email Testing' → 'Inboxes'"
        echo "4. Click 'Demo Inbox' and then 'Show Credentials'"
        echo "5. Copy your SMTP credentials"
        echo ""
        read -p "Enter your Mailtrap Username: " MAILTRAP_USER
        read -s -p "Enter your Mailtrap Password: " MAILTRAP_PASS
        echo ""
        
        echo ""
        echo "🚀 Setting up Render environment variables..."
        echo ""
        echo "On Render Dashboard:"
        echo "  1. Go to your TredingSystem service"
        echo "  2. Click 'Environment'"
        echo "  3. Add these variables:"
        echo ""
        echo "    Email__Username = $MAILTRAP_USER"
        echo "    Email__Password = [Your Mailtrap Password]"
        echo "    Email__SmtpServer = smtp.mailtrap.io"
        echo "    Email__SmtpPort = 587"
        echo ""
        echo "4. Click 'Save'"
        echo "5. Wait for auto-redeploy (2-3 minutes)"
        echo ""
        echo "✅ Configuration ready! Test signup to verify."
        ;;
    2)
        echo ""
        echo "📧 Gmail Setup"
        echo "==============="
        echo ""
        echo "1. Go to https://myaccount.google.com/apppasswords"
        echo "2. Log in to your Google account"
        echo "3. Select 'Mail' and 'Windows Computer'"
        echo "4. Google will generate a 16-character password"
        echo "5. Copy this password"
        echo ""
        read -p "Enter your Gmail address: " GMAIL_USER
        read -s -p "Enter your 16-character App Password: " GMAIL_PASS
        echo ""
        
        echo ""
        echo "🚀 Setting up Render environment variables..."
        echo ""
        echo "On Render Dashboard:"
        echo "  1. Go to your TredingSystem service"
        echo "  2. Click 'Environment'"
        echo "  3. Add these variables:"
        echo ""
        echo "    Email__Username = $GMAIL_USER"
        echo "    Email__Password = [Your 16-char App Password]"
        echo "    Email__SmtpServer = smtp.gmail.com"
        echo "    Email__SmtpPort = 587"
        echo "    Email__SenderEmail = $GMAIL_USER"
        echo ""
        echo "4. Click 'Save'"
        echo "5. Wait for auto-redeploy (2-3 minutes)"
        echo ""
        echo "✅ Gmail configuration ready! Test signup to verify."
        ;;
    3)
        echo ""
        echo "📧 SendGrid Setup"
        echo "=================="
        echo ""
        echo "1. Go to https://sendgrid.com"
        echo "2. Sign up or log in"
        echo "3. Go to 'Settings' → 'API Keys'"
        echo "4. Click 'Create API Key'"
        echo "5. Copy the API Key (starts with 'SG.')"
        echo ""
        echo "⚠️  SendGrid requires code changes to OtpService.cs"
        echo "    (NuGet package: SendGrid required)"
        echo ""
        echo "📞 Contact support for SendGrid integration"
        ;;
    4)
        echo "Exiting..."
        exit 0
        ;;
    *)
        echo "Invalid option!"
        exit 1
        ;;
esac

echo ""
echo "📚 Documentation available in: EMAIL_OTP_SETUP_GUIDE.md"
echo ""
