using Microsoft.AspNetCore.Mvc;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Messaging;
using PartnerService.WebHost.Models;
using PartnerService.WebHost.Services;

namespace PartnerService.Controllers
{
    /// <summary>
    /// Выдача приглашений в коворкинг клиентам с подходящей деятельностью.
    /// </summary>
    [ApiController]
    [Route("partners/{partnerId:guid}/[controller]")]
    public class InvitesController : ControllerBase
    {
        private readonly IRepository<Partner> _partnersRepository;
        private readonly IRepository<Activity> _activitiesRepository;
        private readonly IRepository<Invite> _invitesRepository;
        private readonly IInvitePublisher _invitePublisher;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IConfiguration _configuration;

        public InvitesController(
            IRepository<Partner> partnersRepository,
            IRepository<Activity> activitiesRepository,
            IRepository<Invite> invitesRepository,
            IInvitePublisher invitePublisher,
            IUserServiceClient userServiceClient,
            IConfiguration configuration)
        {
            _partnersRepository = partnersRepository;
            _activitiesRepository = activitiesRepository;
            _invitesRepository = invitesRepository;
            _invitePublisher = invitePublisher;
            _userServiceClient = userServiceClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Выдать приглашение в коворкинг клиентам с указанной деятельностью.
        /// Доступно либо партнёру через API (заголовок X-Api-Key), либо менеджеру/администратору (JWT).
        /// Приглашение публикуется в RabbitMQ и рассылается клиентам асинхронно — UserService
        /// заберёт сообщение из очереди и создаст приглашения в клиентской базе.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InviteResponse>> IssueInviteAsync(
            [FromRoute] Guid partnerId,
            [FromBody] IssueInviteRequest request,
            CancellationToken ct)
        {
            var expectedApiKey = _configuration["PartnerApi:ApiKey"];
            var hasValidApiKey = !string.IsNullOrEmpty(expectedApiKey) &&
                Request.Headers.TryGetValue("X-Api-Key", out var providedKey) &&
                providedKey == expectedApiKey;

            var isManagerOrAdmin = User.Identity?.IsAuthenticated == true &&
                (User.IsInRole("Manager") || User.IsInRole("Admin"));

            if (!hasValidApiKey && !isManagerOrAdmin)
            {
                return Unauthorized("Нужен либо заголовок X-Api-Key партнёра, либо JWT менеджера/администратора");
            }

            var partner = await _partnersRepository.GetByIdAsync(partnerId);
            if (partner is null)
            {
                return NotFound("Коворкинг не найден");
            }

            var activity = await _activitiesRepository.GetByIdAsync(request.ActivityId);
            if (activity is null)
            {
                return BadRequest("Указанная деятельность не найдена");
            }

            // Ранняя проверка вместимости: если мест уже нет, приглашение не сохраняем и не
            // рассылаем. Окончательный лимит применяет UserService при самой рассылке (он же
            // и считает занятые места), поэтому если он не ответил — проверку пропускаем.
            var occupiedSeats = await _userServiceClient.GetOccupiedSeatsAsync(partner.Id, ct);
            if (occupiedSeats is { } occupied && occupied >= partner.Capacity)
            {
                return Conflict(
                    $"В коворкинге «{partner.Name}» нет свободных мест: вместимость {partner.Capacity}, занято {occupied}");
            }

            var invite = new Invite
            {
                Id = Guid.NewGuid(),
                PartnerId = partner.Id,
                ActivityId = activity.Id,
                Description = request.Description,
                IssuedAt = DateTime.UtcNow
            };

            await _invitesRepository.AddAsync(invite);

            var queuedForDelivery = await _invitePublisher.PublishAsync(
                new InviteBroadcastMessage
                {
                    PartnerId = partner.Id,
                    PartnerName = partner.Name,
                    Specialization = partner.Specialization,
                    Activity = activity.Name,
                    Description = request.Description,
                    Capacity = partner.Capacity
                },
                ct);

            var response = new InviteResponse
            {
                Id = invite.Id,
                PartnerId = partner.Id,
                PartnerName = partner.Name,
                ActivityId = activity.Id,
                ActivityName = activity.Name,
                Description = invite.Description,
                IssuedAt = invite.IssuedAt,
                QueuedForDelivery = queuedForDelivery
            };

            return Ok(response);
        }
    }
}
