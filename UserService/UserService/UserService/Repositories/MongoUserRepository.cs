using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UserService.Data;
using UserService.Domain;

namespace UserService.Repositories;

/// <summary>
/// Клиентская/сотрудническая база поверх MongoDB — хранит только профиль (пароли и сама
/// аутентификация теперь полностью в Keycloak, см. UserService.Services.Keycloak). Id
/// хранится как строка (Guid.ToString()), а не через встроенную BSON-сериализацию Guid —
/// так надёжнее для разных версий MongoDB.Driver: представление Guid в BSON менялось между
/// версиями драйвера и требует явной настройки, которую в этой среде нельзя было бы
/// проверить компиляцией. Id профиля совпадает с id того же пользователя в Keycloak.
/// </summary>
public sealed class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> _collection;

    public MongoUserRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        _collection = database.GetCollection<UserDocument>(options.Value.UsersCollection);
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync(UserRole? role = null, string? activity = null)
    {
        var filterBuilder = Builders<UserDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (role is not null)
        {
            filter &= filterBuilder.Eq(x => x.Role, role.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(activity))
        {
            filter &= filterBuilder.Regex(x => x.Activity, ExactIgnoreCase(activity));
        }

        var documents = await _collection.Find(filter).ToListAsync();
        return documents.Select(ToDomain).ToList();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var document = await _collection.Find(x => x.Id == id.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToDomain(document);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var filter = Builders<UserDocument>.Filter.Regex(x => x.Email, ExactIgnoreCase(email));
        var document = await _collection.Find(filter).FirstOrDefaultAsync();
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(User user)
    {
        await _collection.InsertOneAsync(ToDocument(user));
    }

    public async Task UpdateAsync(User user)
    {
        await _collection.ReplaceOneAsync(x => x.Id == user.Id.ToString(), ToDocument(user), new ReplaceOptions { IsUpsert = true });
    }

    public async Task DeleteAsync(Guid id)
    {
        await _collection.DeleteOneAsync(x => x.Id == id.ToString());
    }

    private static MongoDB.Bson.BsonRegularExpression ExactIgnoreCase(string value) =>
        new($"^{Regex.Escape(value)}$", "i");

    private static UserDocument ToDocument(User user) => new()
    {
        Id = user.Id.ToString(),
        FirstName = user.FirstName,
        SecondName = user.SecondName,
        LastName = user.LastName,
        Phone = user.Phone,
        Email = user.Email,
        Role = user.Role.ToString(),
        Activity = user.Activity,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    private static User ToDomain(UserDocument document) => new()
    {
        Id = Guid.Parse(document.Id),
        FirstName = document.FirstName,
        SecondName = document.SecondName,
        LastName = document.LastName,
        Phone = document.Phone,
        Email = document.Email,
        Role = Enum.Parse<UserRole>(document.Role),
        Activity = document.Activity,
        IsActive = document.IsActive,
        CreatedAt = document.CreatedAt
    };

    [BsonIgnoreExtraElements] // на диске в MongoDB ещё могут лежать старые документы
    // с полями, которых больше нет в классе (например, PasswordHash из эпохи до Keycloak) —
    // без этого атрибута десериализация такого документа падает с FormatException.
    private sealed class UserDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Activity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
