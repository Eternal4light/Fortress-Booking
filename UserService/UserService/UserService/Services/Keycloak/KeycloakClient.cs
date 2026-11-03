using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using UserService.Domain;

namespace UserService.Services.Keycloak;

/// <summary>
/// Клиент к Keycloak: логин пользователей (Direct Access Grant) и управление
/// пользователями через Admin REST API (создание/роли/блокировка/удаление).
/// Административный токен получается через встроенного клиента "admin-cli" в realm
/// "master" по логину/паролю бутстрап-администратора — это часть Admin REST API,
/// стабильная на протяжении многих версий Keycloak, поэтому выбрана как наименее
/// рискованный вариант в среде без доступа к компиляции/запуску для проверки.
/// </summary>
public sealed class KeycloakClient : IKeycloakClient
{
    private readonly HttpClient _http;
    private readonly KeycloakOptions _options;

    public KeycloakClient(HttpClient http, IOptions<KeycloakOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.FrontendClientId,
            ["username"] = email,
            ["password"] = password
        };

        using var response = await _http.PostAsync(
            $"{_options.BaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(form),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        return payload?.AccessToken;
    }

    public async Task<Guid> CreateUserAsync(string email, string password, string firstName, string lastName, UserRole role, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users")
        {
            Content = JsonContent.Create(new
            {
                username = email,
                email,
                firstName,
                lastName,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new { type = "password", value = password, temporary = false }
                }
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var createResponse = await _http.SendAsync(createRequest, ct);
        if (!createResponse.IsSuccessStatusCode)
        {
            var details = await createResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Keycloak не смог создать пользователя ({(int)createResponse.StatusCode}): {details}");
        }

        var location = createResponse.Headers.Location
            ?? throw new InvalidOperationException("Keycloak не вернул адрес созданного пользователя (заголовок Location)");
        var userId = Guid.Parse(location.Segments[^1].TrimEnd('/'));

        await AssignRoleAsync(userId, role.ToString(), adminToken, ct);

        return userId;
    }

    public async Task UpdateRoleAsync(Guid keycloakUserId, UserRole oldRole, UserRole newRole, CancellationToken ct = default)
    {
        if (oldRole == newRole)
        {
            return;
        }

        var adminToken = await GetAdminTokenAsync(ct);
        await RemoveRoleAsync(keycloakUserId, oldRole.ToString(), adminToken, ct);
        await AssignRoleAsync(keycloakUserId, newRole.ToString(), adminToken, ct);
    }

    public async Task SetEnabledAsync(Guid keycloakUserId, bool enabled, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);

        // Admin REST API Keycloak ожидает в PUT актуальное полное представление
        // пользователя, поэтому сначала читаем его, меняем только "enabled" и
        // отправляем обратно целиком — так безопаснее, чем слепой частичный PUT.
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{keycloakUserId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var getResponse = await _http.SendAsync(getRequest, ct);
        getResponse.EnsureSuccessStatusCode();

        var json = await getResponse.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);

        var updatedJson = WithEnabledField(document.RootElement, enabled);

        using var putRequest = new HttpRequestMessage(HttpMethod.Put, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{keycloakUserId}")
        {
            Content = new StringContent(updatedJson, Encoding.UTF8, "application/json")
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var putResponse = await _http.SendAsync(putRequest, ct);
        putResponse.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(Guid keycloakUserId, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{keycloakUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = _options.AdminUsername,
            ["password"] = _options.AdminPassword
        };

        using var response = await _http.PostAsync(
            $"{_options.BaseUrl}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(form),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Не удалось получить служебный токен Keycloak ({(int)response.StatusCode}): {details}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        return payload?.AccessToken ?? throw new InvalidOperationException("Keycloak не вернул access_token для служебного пользователя");
    }

    private async Task AssignRoleAsync(Guid userId, string roleName, string adminToken, CancellationToken ct)
    {
        var role = await GetRoleAsync(roleName, adminToken, ct);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task RemoveRoleAsync(Guid userId, string roleName, string adminToken, CancellationToken ct)
    {
        var role = await GetRoleAsync(roleName, adminToken, ct);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<RoleRepresentation> GetRoleAsync(string roleName, string adminToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/roles/{roleName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var role = await response.Content.ReadFromJsonAsync<RoleRepresentation>(cancellationToken: ct);
        return role ?? throw new InvalidOperationException($"Роль '{roleName}' не найдена в Keycloak");
    }

    private static string WithEnabledField(JsonElement representation, bool enabled)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            var wroteEnabled = false;
            foreach (var property in representation.EnumerateObject())
            {
                if (property.NameEquals("enabled"))
                {
                    writer.WriteBoolean("enabled", enabled);
                    wroteEnabled = true;
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            if (!wroteEnabled)
            {
                writer.WriteBoolean("enabled", enabled);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class RoleRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
