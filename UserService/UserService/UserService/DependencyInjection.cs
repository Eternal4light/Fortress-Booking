using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserService.Data;
using UserService.Repositories;
using UserService.Services;
using UserService.Services.Keycloak;

namespace UserService;

public static class DependencyInjection
{
    public static void AddUserServiceDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var client = new MongoClient(options.ConnectionString);
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddSingleton<IUserRepository, MongoUserRepository>();
        services.AddSingleton<IUserInviteRepository, MongoUserInviteRepository>();
        services.AddHttpClient<IKeycloakClient, KeycloakClient>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IInviteBroadcastService, InviteBroadcastService>();
        services.AddSingleton<IUserInviteService, UserInviteService>();
    }
}
