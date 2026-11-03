using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UserService.Data;
using UserService.Domain;

namespace UserService.Repositories;

/// <summary>
/// Приглашения, полученные клиентами, поверх MongoDB. Как и в MongoUserRepository,
/// Id хранится как строка, а не через встроенную BSON-сериализацию Guid.
/// </summary>
public sealed class MongoUserInviteRepository : IUserInviteRepository
{
    private readonly IMongoCollection<UserInviteDocument> _collection;

    public MongoUserInviteRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        _collection = database.GetCollection<UserInviteDocument>(options.Value.InvitesCollection);
    }

    public async Task<IReadOnlyCollection<UserInvite>> GetByUserIdAsync(Guid userId)
    {
        var documents = await _collection
            .Find(x => x.UserId == userId.ToString())
            .SortByDescending(x => x.IssuedAt)
            .ToListAsync();

        return documents.Select(ToDomain).ToList();
    }

    public async Task<UserInvite?> GetByIdAsync(Guid id)
    {
        var key = id.ToString();
        var document = await _collection.Find(x => x.Id == key).FirstOrDefaultAsync();
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyCollection<UserInvite>> GetActiveByPartnerAsync(Guid partnerId, DateTime utcNow)
    {
        var filter = Builders<UserInviteDocument>.Filter;
        var activeForPartner =
            filter.Eq(x => x.PartnerId, partnerId.ToString()) &
            (filter.Eq(x => x.Status, UserInviteStatus.Accepted) |
             (filter.Eq(x => x.Status, UserInviteStatus.Pending) & filter.Gt(x => x.ExpiresAt, utcNow)));

        var documents = await _collection.Find(activeForPartner).ToListAsync();
        return documents.Select(ToDomain).ToList();
    }

    public async Task<int> AddRangeAsync(IEnumerable<UserInvite> invites)
    {
        var documents = invites.Select(ToDocument).ToList();
        if (documents.Count == 0)
        {
            return 0;
        }

        await _collection.InsertManyAsync(documents);
        return documents.Count;
    }

    public async Task<bool> TryRespondAsync(Guid id, UserInviteStatus newStatus, DateTime utcNow)
    {
        var filter = Builders<UserInviteDocument>.Filter;
        var stillPending =
            filter.Eq(x => x.Id, id.ToString()) &
            filter.Eq(x => x.Status, UserInviteStatus.Pending) &
            filter.Gt(x => x.ExpiresAt, utcNow);

        var update = Builders<UserInviteDocument>.Update
            .Set(x => x.Status, newStatus)
            .Set(x => x.RespondedAt, (DateTime?)utcNow);

        var result = await _collection.UpdateOneAsync(stillPending, update);
        return result.ModifiedCount == 1;
    }

    public async Task<long> ExpireOverdueAsync(DateTime utcNow)
    {
        var filter = Builders<UserInviteDocument>.Filter;
        var overdue =
            filter.Eq(x => x.Status, UserInviteStatus.Pending) &
            (filter.Lte(x => x.ExpiresAt, utcNow) | filter.Exists(x => x.ExpiresAt, false));

        var update = Builders<UserInviteDocument>.Update.Set(x => x.Status, UserInviteStatus.Expired);

        var result = await _collection.UpdateManyAsync(overdue, update);
        return result.ModifiedCount;
    }

    private static UserInviteDocument ToDocument(UserInvite invite) => new()
    {
        Id = invite.Id.ToString(),
        UserId = invite.UserId.ToString(),
        PartnerId = invite.PartnerId.ToString(),
        PartnerName = invite.PartnerName,
        Specialization = invite.Specialization,
        Activity = invite.Activity,
        Description = invite.Description,
        IssuedAt = invite.IssuedAt,
        ExpiresAt = invite.ExpiresAt,
        Status = invite.Status,
        RespondedAt = invite.RespondedAt
    };

    private static UserInvite ToDomain(UserInviteDocument document) => new()
    {
        Id = Guid.Parse(document.Id),
        UserId = Guid.Parse(document.UserId),
        PartnerId = Guid.Parse(document.PartnerId),
        PartnerName = document.PartnerName,
        Specialization = document.Specialization,
        Activity = document.Activity,
        Description = document.Description,
        IssuedAt = document.IssuedAt,
        ExpiresAt = document.ExpiresAt,
        Status = document.Status,
        RespondedAt = document.RespondedAt
    };

    [BsonIgnoreExtraElements] // на диске в MongoDB ещё могут лежать старые документы
    // с полями, которых больше нет в классе (например, PasswordHash из эпохи до Keycloak) —
    // без этого атрибута десериализация такого документа падает с FormatException.
    private sealed class UserInviteDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string PartnerId { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string Activity { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime IssuedAt { get; set; }

        // У документов, созданных до появления статусов, этих полей нет: Status тогда
        // читается как Pending, а ExpiresAt как DateTime.MinValue — такие приглашения
        // сразу считаются просроченными (см. UserInvite.GetEffectiveStatus).
        public DateTime ExpiresAt { get; set; }

        [BsonRepresentation(BsonType.String)]
        public UserInviteStatus Status { get; set; } = UserInviteStatus.Pending;

        public DateTime? RespondedAt { get; set; }
    }
}
