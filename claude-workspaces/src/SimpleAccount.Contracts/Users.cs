namespace SimpleAccount.Contracts;

public record UserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName);

/// <summary>
/// Sent by the gateway on every successful Google login. Idempotent on <see cref="GoogleSubject"/>.
/// </summary>
public record UpsertUserRequest(
    string GoogleSubject,
    string Email,
    string? FirstName,
    string? LastName);

public record UpdateUserRequest(
    string? FirstName,
    string? LastName);
