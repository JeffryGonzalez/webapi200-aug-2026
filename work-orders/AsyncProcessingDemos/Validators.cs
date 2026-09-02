using System.Text.RegularExpressions;

namespace AsyncProcessingDemos;

public static partial class Validators
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public static bool IsValidEmail(string email)
    {
        // Simple regex for email validation
      
        return EmailRegex().IsMatch(email);
    }
}
