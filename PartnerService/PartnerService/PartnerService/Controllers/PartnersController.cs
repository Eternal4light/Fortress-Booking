using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Models;

namespace PartnerService.Controllers
{
    /// <summary>
    /// Коворкинги. Доступно только сотрудникам сервиса (менеджерам и администратору) —
    /// клиенты и посторонние список коворкингов не видят.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class PartnersController : ControllerBase
    {
        private const string ListCacheKey = "partners:list";

        // Список читается часто (открывается при каждом заходе на страницу коворкингов),
        // а меняется только через CreatePartnerAsync — короткий TTL плюс явная инвалидация
        // при создании держат кэш свежим без риска показать давно устаревшие данные.
        private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(2);

        private readonly IRepository<Partner> _partnersRepository;
        private readonly IDistributedCache _cache;

        public PartnersController(IRepository<Partner> partnersRepository, IDistributedCache cache)
        {
            _partnersRepository = partnersRepository;
            _cache = cache;
        }

        /// <summary>
        /// Получить список партнеров
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PartnerResponse>>> GetPartnersAsync()
        {
            var cached = await _cache.GetStringAsync(ListCacheKey);
            if (cached is not null)
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<List<PartnerResponse>>(cached);
                    if (cachedResponse is not null)
                    {
                        return Ok(cachedResponse);
                    }
                }
                catch (JsonException)
                {
                    // повреждённая или несовместимая запись в кэше (например, после
                    // деплоя с изменённым PartnerResponse) — просто читаем из базы заново
                }
            }

            var partners = await _partnersRepository.GetAllAsync();

            var response = partners.Select(x => new PartnerResponse()
            {
                Id = x.Id,
                Name = x.Name,
                Specialization = x.Specialization,
                IsActive = x.IsActive,
                Capacity = x.Capacity
            }).ToList();

            await _cache.SetStringAsync(
                ListCacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ListCacheTtl });

            return Ok(response);
        }

        /// <summary>
        /// Добавить коворкинг (партнера)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PartnerResponse>> CreatePartnerAsync([FromBody] CreatePartnerRequest request)
        {
            if (request.Capacity < 1)
            {
                return BadRequest("Вместимость коворкинга должна быть не меньше 1");
            }

            var partner = new Partner
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Specialization = request.Specialization,
                IsActive = true,
                Capacity = request.Capacity
            };

            await _partnersRepository.AddAsync(partner);

            // кэшированный список устарел — убираем, следующий GET перечитает из базы
            // и положит в кэш уже актуальные данные
            await _cache.RemoveAsync(ListCacheKey);

            var response = new PartnerResponse
            {
                Id = partner.Id,
                Name = partner.Name,
                Specialization = partner.Specialization,
                IsActive = partner.IsActive,
                Capacity = partner.Capacity
            };

            return Created($"/partners/{partner.Id}", response);
        }

    }
}
