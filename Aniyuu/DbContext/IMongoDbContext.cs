using MongoDB.Driver;

namespace Aniyuu.DbContext;

public interface IMongoDbContext
{
    IMongoCollection<TEntity> GetCollection<TEntity>(string name);
    Task<int> SaveChanges();
}