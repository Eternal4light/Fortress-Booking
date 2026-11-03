using System.Collections.Concurrent;
using UserService.Domain;

namespace UserService.Repositories;

public class InMemoryUserInviteRepository : IUserInviteRepository
{
    private readonly ConcurrentBag<UserInvite> _invites = new();

    public Task<IReadOnlyCollection<UserInvite>> GetByUserIdAsync(Guid userId)
    {
        var result = _invites
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.IssuedAt)
            .ToList();

        return Task.FromResult((IReadOnlyCollection<UserInvite>)result);
    }

    public Task<int> AddRangeAsync(IEnumerable<UserInvite> invites)
    {
        var list = invites.ToList();

        foreach (var invite in list)
        {
            _invites.Add(invite);
        }

        return Task.FromResult(list.Count);
    }
}
