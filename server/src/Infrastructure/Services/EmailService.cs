using System.Net;
using System.Net.Mail;
using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    // mail for email confirmation
    public async Task SendEmailConfirmation(string toEmail, string username, string confirmationLink, CancellationToken cancellationToken = default)
    {
        var subject = "Confirm your Aurum Messaging account";
        var safeUsername = WebUtility.HtmlEncode(username);
        var safeLink = WebUtility.HtmlEncode(confirmationLink);

        var content = $"""
            <h1 style="margin: 0 0 16px 0; font-size: 22px; font-weight: 700; color: #1a1917; line-height: 1.3;">Confirm your email address</h1>
            <p style="margin: 0 0 16px 0; font-size: 15px; color: #4a4742; line-height: 1.6;">Hello <strong>{safeUsername}</strong>,</p>
            <p style="margin: 0 0 24px 0; font-size: 15px; color: #4a4742; line-height: 1.6;">Welcome to <strong>Aurum Messaging</strong>! To complete your registration and activate your account, please verify your email address by clicking the button below:</p>

            <div style="text-align: center; margin: 32px 0;">
                <a href="{safeLink}" target="_blank" style="display: inline-block; background-color: #c89b4a; color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600; padding: 14px 32px; border-radius: 8px; box-shadow: 0 2px 8px rgba(200, 155, 74, 0.35);">Verify Email Address</a>
            </div>

            <p style="margin: 0 0 16px 0; font-size: 13px; color: #7a766e; line-height: 1.5;">This verification link will expire in <strong>24 hours</strong>.</p>

            <div style="margin-top: 28px; padding-top: 20px; border-top: 1px solid #eeebe3; font-size: 13px; color: #8a867e; line-height: 1.5;">
                If you did not create an account with Aurum Messaging, please disregard this email. No account will be activated without confirmation.
            </div>
        """;

        var body = BuildEmailLayout("Confirm your email", content);
        await SendEmail(toEmail, subject, body, confirmationLink);
    }

    // mail for password reset
    public async Task SendPasswordResetOtp(string toEmail, string username, string otp, CancellationToken cancellationToken = default)
    {
        var subject = "Password Reset Code - Aurum Messaging";
        var safeUsername = WebUtility.HtmlEncode(username);
        var safeOtp = WebUtility.HtmlEncode(otp);

        var content = $"""
            <h1 style="margin: 0 0 16px 0; font-size: 22px; font-weight: 700; color: #1a1917; line-height: 1.3;">Password Reset Request</h1>
            <p style="margin: 0 0 16px 0; font-size: 15px; color: #4a4742; line-height: 1.6;">Hello <strong>{safeUsername}</strong>,</p>
            <p style="margin: 0 0 24px 0; font-size: 15px; color: #4a4742; line-height: 1.6;">We received a request to reset the password for your Aurum Messaging account. Use the verification code below to proceed with setting a new password:</p>

            <div style="text-align: center; margin: 28px 0;">
                <div style="display: inline-block; background-color: #f7f5ef; border: 1px solid #e3ddd1; border-radius: 8px; padding: 12px 32px;">
                    <span style="font-family: -apple-system, BlinkMacSystemFont, 'SF Mono', Consolas, 'Courier New', monospace; font-size: 24px; font-weight: 700; letter-spacing: 4px; color: #23201b; display: block;">
                        {safeOtp}
                    </span>
                </div>
                <span style="display: block; font-size: 12px; color: #8a867e; margin-top: 10px;">This code expires in <strong>5 minutes</strong>.</span>
            </div>

            <div style="background-color: #fef7ea; border-left: 4px solid #d1a153; padding: 12px 16px; border-radius: 4px; margin-bottom: 24px;">
                <p style="margin: 0; font-size: 13px; color: #7a5818; line-height: 1.5;">
                    <strong>Security Notice:</strong> Never share this code with anyone. Aurum Messaging representatives will never ask for your verification code.
                </p>
            </div>

            <div style="margin-top: 28px; padding-top: 20px; border-top: 1px solid #eeebe3; font-size: 13px; color: #8a867e; line-height: 1.5;">
                If you did not request a password reset, please ignore this email or review your account security immediately. Your password remains unchanged.
            </div>
        """;

        var body = BuildEmailLayout("Password Reset", content);
        await SendEmail(toEmail, subject, body, otp);
    }

    // helper for building email layout
    private static string BuildEmailLayout(string title, string contentHtml)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{WebUtility.HtmlEncode(title)}</title>
            </head>
            <body style="margin: 0; padding: 0; background-color: #f5f2eb; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #2c2925;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: #f5f2eb; padding: 40px 16px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width: 540px; margin: 0 auto;">
                                <tr>
                                    <td align="center" style="padding-bottom: 24px;">
                                        <span style="font-size: 20px; font-weight: 800; letter-spacing: 1.5px; color: #b5832a; text-transform: uppercase;">Aurum Messaging</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: #ffffff; border-radius: 14px; border: 1px solid #e7e2d7; box-shadow: 0 4px 18px rgba(0, 0, 0, 0.05); overflow: hidden;">
                                            <tr>
                                                <td height="5" style="background-color: #c89b4a; font-size: 1px; line-height: 1px;">&nbsp;</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 36px 32px;">
                                                    {contentHtml}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td align="center" style="padding-top: 24px; font-size: 12px; color: #9c978f; line-height: 1.6;">
                                        <p style="margin: 0;">This is an automated notification. Please do not reply directly to this email.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
        """;
    }

    // helper for sending email
    private async Task SendEmail(string toEmail, string subject, string htmlBody, string fallbackDevInfo)
    {
        // if no email settings are configured, log the email to the console
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            _logger.LogInformation("=================================================");
            _logger.LogInformation("[DEV EMAIL SIMULATOR] To: {ToEmail}", toEmail);
            _logger.LogInformation("[DEV EMAIL SIMULATOR] Subject: {Subject}", subject);
            _logger.LogInformation("[DEV EMAIL SIMULATOR] Payload: {FallbackInfo}", fallbackDevInfo);
            _logger.LogInformation("=================================================");
            return;
        }

        try
        {   
            // creates a smtp client
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };
            // composes the email
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);
            // sends the email
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}. Fallback info: {FallbackInfo}", toEmail, fallbackDevInfo);
            _logger.LogInformation("[FALLBACK EMAIL LOG] {Subject} -> {ToEmail} | Payload: {FallbackInfo}", subject, toEmail, fallbackDevInfo);
        }
    }
}