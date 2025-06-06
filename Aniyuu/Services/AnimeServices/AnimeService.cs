using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using Aniyuu.ViewModels.AnimeViewModels;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NLog;

namespace Aniyuu.Services.AnimeServices;

public class AnimeService(IMongoDbContext mongoDbContext) :  IAnimeService
{
    private readonly IMongoCollection<AnimeModel> _animeCollection = mongoDbContext
        .GetCollection<AnimeModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<AnimeModel> Get(int malId, CancellationToken cancellationToken)
    {
        try
        {
            var anime = await _animeCollection.Find(x => x.MALId == malId && x.IsActive == true).FirstOrDefaultAsync(cancellationToken);
            if (anime == null)
            {
                throw new AppException("Anime not found.", 409);
            }
            return anime;
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.Get] Anime not found or query failed. {e.Message}");
            throw new AppException("Anime not found or query failed.", 500);
        }
    }

    public async Task<List<AnimeModel>> GetAll(int page, int count, CancellationToken cancellationToken)
    {
        var animes = await _animeCollection
            .Find(x => true)
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);
        
        return animes;
    }

    public async Task<List<AnimeModel>> Search(string _query, int page, int count, CancellationToken cancellationToken)
    {
        var result = await _animeCollection.Aggregate()
            .Search(Builders<AnimeModel>.Search.Text(g => g.Title, _query), indexName: "default")
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);
        
        
        var queryablecollection = _animeCollection.AsQueryable();
        var query = queryablecollection
            .Search(Builders<AnimeModel>.Search.Text(g => g.Title, _query), indexName: "default")
            .Select(g => new AnimeSearchResultViewModel());
        
        return result;
    }
}