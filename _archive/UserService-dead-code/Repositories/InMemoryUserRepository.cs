using System.Collections.Concurrent;
using UserService.Domain;

namespace UserService.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users;

    public InMemoryUserRepository(IEnumerable<User> seed)
    {
        _users = new ConcurrentDictionary<Guid, User>(seed.Select(u => new KeyValuePair<Guid, User>(u.Id, u)));
    }

    public Task<IReadOnlyCollection<User>> GetAllAsync(UserRole? role = null, string? activity = null)
    {
        IEnumerable<User> query = _users.Values;

        if (role is not null)
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(activity))
        {
            query = query.Where(u => string.Equals(u.Activity, activity, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult((IReadOnlyCollection<User>)query.ToList());
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _users.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
