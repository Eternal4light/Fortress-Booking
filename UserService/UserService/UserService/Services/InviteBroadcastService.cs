using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;

namespace UserService.Services
{
    public sealed class InviteBroadcastService : IInviteBroadcastService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserInviteRepository _inviteRepository;
        private readonly TimeProvider _timeProvider;

        public InviteBroadcastService(
            IUserRepository userRepository,
            IUserInviteRepository inviteRepository,
            TimeProvider timeProvider)
        {
            _userRepository = userRepository;
            _inviteRepository = inviteRepository;
            _timeProvider = timeProvider;
        }

        public async Task<int> BroadcastAsync(BroadcastInviteRequest request, CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var clients = await _userRepository.GetAllAsync(UserRole.Client, request.Activity);

            // Место в коворкинге занимает клиент, у которого есть приглашение в ожидании
            // ответа или принятое. Клиентам, у кого такое приглашение уже есть, новое не
            // создаём, а остальным — не больше, чем осталось свободных мест.
            IEnumerable<User> recipients = clients;
            if (request.Capacity is { } capacity)
            {
                var active = await _inviteRepository.GetActiveByPartnerAsync(request.PartnerId, now);
                var alreadyInvited = active.Select(i => i.UserId).ToHashSet();
                var freeSeats = Math.Max(0, capacity - alreadyInvited.Count);

                recipients = clients.Where(c => !alreadyInvited.Contains(c.Id)).Take(freeSeats);
            }

            var invites = recipients.Select(c => new UserInvite
            {
                Id = Guid.NewGuid(),
                UserId = c.Id,
                PartnerId = request.PartnerId,
                PartnerName = request.PartnerName,
                Specialization = request.Specialization,
                Activity = request.Activity,
                Description = request.Description,
                IssuedAt = now,
                ExpiresAt = now + UserInvite.Lifetime,
                Status = UserInviteStatus.Pending
            }).ToList();

            return await _inviteRepository.AddRangeAsync(invites);
        }
    }
}
