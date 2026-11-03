using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Models;

namespace PartnerService.Controllers
{
    /// <summary>
    /// Справочник деятельностей клиентов — используется при выдаче приглашений,
    /// как партнёрами через API, так и менеджерами через веб-интерфейс.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ActivitiesController : ControllerBase
    {
        private const string ListCacheKey = "activities:list";

        // Деятельности сейчас нельзя ни создать, ни изменить через API (write-эндпоинтов
        // нет вовсе), поэтому TTL можно держать заметно дольше, чем у списка партнёров —
        // инвалидировать кэш явно всё равно неоткуда.
        private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(15);

        private readonly IRepository<Activity> _activitiesRepository;
        private readonly IDistributedCache _cache;

        public ActivitiesController(IRepository<Activity> activitiesRepository, IDistributedCache cache)
        {
            _activitiesRepository = activitiesRepository;
            _cache = cache;
        }

        /// <summary>
        /// Получить список деятельностей
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ActivityResponse>>> GetActivitiesAsync()
        {
            var cached = await _cache.GetStringAsync(ListCacheKey);
            if (cached is not null)
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<List<ActivityResponse>>(cached);
                    if (cachedResponse is not null)
                    {
                        return Ok(cachedResponse);
                    }
                }
                catch (JsonException)
                {
                    // повреждённая или несовместимая запись в кэше — читаем из базы заново
                }
            }

            var activities = await _activitiesRepository.GetAllAsync();

            var response = activities.Select(x => new ActivityResponse
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();

            await _cache.SetStringAsync(
                ListCacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ListCacheTtl });

            return Ok(response);
        }
    }
}
