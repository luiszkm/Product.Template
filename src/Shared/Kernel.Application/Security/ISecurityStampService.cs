namespace Product.Template.Kernel.Application.Security;

/// <summary>
/// Manages the security stamp for users, enabling near-immediate token revocation
/// when roles or permissions change.
/// </summary>
public interface ISecurityStampService
{
    /// <summary>
    /// Regenerates the user's security stamp, invalidating all existing access tokens
    /// for that user within the cache TTL window.
    /// </summary>
    Task RegenerateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the given stamp matches the user's current security stamp.
    /// </summary>
    Task<bool> ValidateAsync(Guid userId, string stamp, CancellationToken cancellationToken = default);
}
