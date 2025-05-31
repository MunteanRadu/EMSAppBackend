using MongoDB.Driver;

namespace EMSApp.Infrastructure;

public interface IMongoDbContext
{
    IMongoCollection<T> GetCollection<T>(string name);
}
