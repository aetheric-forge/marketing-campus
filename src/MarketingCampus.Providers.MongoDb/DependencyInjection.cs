using MarketingCampus.Core.Campaigns;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MarketingCampus.Providers.MongoDb;

public static class DependencyInjection
{
    /// <summary>The host supplies its configured database; this package does not own credentials or a Mongo client.</summary>
    public static IServiceCollection AddMarketingDraftStorage(this IServiceCollection services, IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(database);
        services.AddSingleton<ICampaignDraftRepository>(new MongoCampaignDraftRepository(database));
        return services;
    }
}
