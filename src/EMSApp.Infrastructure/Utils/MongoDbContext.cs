using EMSApp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public sealed class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _db;
    public MongoDbContext(IMongoClient client, IOptions<DatabaseSettings> options)
    {
        var settings = options.Value;
        _db = client.GetDatabase(settings.DatabaseName);
    }
    public IMongoCollection<T> GetCollection<T>(string name) => _db.GetCollection<T>(name);
}
