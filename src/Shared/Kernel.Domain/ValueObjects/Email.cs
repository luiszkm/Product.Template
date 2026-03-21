using System.Text.RegularExpressions;

namespace Product.Template.Core.Identity.Domain.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalizedEmail))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new Email(normalizedEmail);
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
