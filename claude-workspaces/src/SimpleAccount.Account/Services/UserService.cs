using Microsoft.EntityFrameworkCore;
using SimpleAccount.Account.Data;
using SimpleAccount.Contracts;

namespace SimpleAccount.Account.Services;

public class UserService(AccountDbContext db, TimeProvider clock, ILogger<UserService> logger)
{
    /// <summary>
    /// Idempotent on Google subject, so a retried login heals rather than duplicating.
    /// Never matches on email — that would be an account-takeover vector.
    /// </summary>
    public async Task<UserDto> UpsertAsync(UpsertUserRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var user = await db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == request.GoogleSubject, ct);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.CreateVersion7(),
                GoogleSubject = request.GoogleSubject,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Users.Add(user);
            logger.LogInformation("Provisioned new user {UserId}", user.Id);
        }
        else
        {
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.UpdatedAt = now;
            logger.LogInformation("Refreshed profile for user {UserId}", user.Id);
        }

        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto?> UpdateNameAsync(Guid userId, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return null;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName);
}
