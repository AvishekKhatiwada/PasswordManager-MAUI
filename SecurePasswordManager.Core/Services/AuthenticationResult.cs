namespace SecurePasswordManager.Core.Services;

public sealed class AuthenticationResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public TimeSpan? RetryAfter { get; init; }

    public VaultSession? Session { get; init; }

    public static AuthenticationResult Success(VaultSession session, string message = "Vault unlocked.") =>
        new() { Succeeded = true, Message = message, Session = session };

    public static AuthenticationResult Failure(string message, TimeSpan? retryAfter = null) =>
        new() { Succeeded = false, Message = message, RetryAfter = retryAfter };
}
