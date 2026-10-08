namespace Infrastructure.Services;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Aurum Messaging";
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}