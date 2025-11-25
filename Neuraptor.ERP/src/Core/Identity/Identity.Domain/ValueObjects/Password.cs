using System.Text.RegularExpressions;

namespace Neuraptor.ERP.Core.Identity.Domain.ValueObjects;

public record Password
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static Password Create(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long", nameof(password));

        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new ArgumentException("Password must contain at least one uppercase letter", nameof(password));

        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new ArgumentException("Password must contain at least one lowercase letter", nameof(password));

        if (!Regex.IsMatch(password, @"\d"))
            throw new ArgumentException("Password must contain at least one digit", nameof(password));

        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            throw new ArgumentException("Password must contain at least one special character", nameof(password));

        return new Password(password);
    }

    public override string ToString() => "********";

    public static implicit operator string(Password password) => password.Value;
}
