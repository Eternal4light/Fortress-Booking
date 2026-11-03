using System.Net.Http.Json;

namespace PartnerService.WebHost.Services
{
    public class UserServiceClient : IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UserServiceClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<int> BroadcastInviteAsync(
            Guid partnerId,
            string partnerName,
            string? specialization,
            string activity,
            string? description,
            CancellationToken ct)
        {
            var internalKey = _configuration["InternalApi:Key"];

            var request = new HttpRequestMessage(HttpMethod.Post, "api/users/invites/broadcast")
            {
                Content = JsonContent.Create(new
                {
                    partnerId,
                    partnerName,
                    specialization,
                    activity,
                    description
                })
            };

            if (!string.IsNullOrEmpty(internalKey))
            {
                request.Headers.Add("X-Internal-Api-Key", internalKey);
            }

            try
            {
                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return 0;
                }

                var result = await response.Content.ReadFromJsonAsync<BroadcastInviteResult>(cancellationToken: ct);
                return result?.RecipientsCount ?? 0;
            }
            catch (HttpRequestException)
            {
                // UserService недоступен — приглашение всё равно сохранено локально,
                // но клиенты о нём не узнают. Для прототипа считаем это допустимым.
                return 0;
            }
        }

        public async Task<int?> GetOccupiedSeatsAsync(Guid partnerId, CancellationToken ct)
        {
            var internalKey = _configuration["InternalApi:Key"];

            var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/invites/occupancy?partnerId={partnerId}");
            if (!string.IsNullOrEmpty(internalKey))
            {
                request.Headers.Add("X-Internal-Api-Key", internalKey);
            }

            try
            {
                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<OccupancyResult>(cancellationToken: ct);
                return result?.OccupiedSeats;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // таймаут HttpClient, а не отмена запроса пользователем
                return null;
            }
        }

        private sealed class OccupancyResult
        {
            public int OccupiedSeats { get; set; }
        }

        private sealed class BroadcastInviteResult
        {
            public int RecipientsCount { get; set; }
        }
    }
}
