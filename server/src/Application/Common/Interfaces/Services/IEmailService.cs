namespace Application.Common.Interfaces.Services;

public interface IEmailService
{
    // send mail for email confirmation
    Task SendEmailConfirmation(string toEmail, string username, string confirmationLink, CancellationToken cancellationToken = default);
    // send mail for password reset
    Task SendPasswordResetOtp(string toEmail, string username, string otp, CancellationToken cancellationToken = default);
}