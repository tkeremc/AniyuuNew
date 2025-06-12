using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services;

public class AdService(IMongoDbContext mongoDbContext) : IAdService
{
    private readonly IMongoCollection<AnimeAdModel> _animeAdCollection = mongoDbContext
        .GetCollection<AnimeAdModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeAdCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    
    public async Task<List<AnimeAdModel>> GetAll(CancellationToken cancellationToken)
    {
        var animes = await _animeAdCollection.Find(x => x.IsActive == true).ToListAsync(cancellationToken);
        if (animes.Count != 0) return animes;
        Logger.Error("No Anime Ads Found");
        throw new AppException("No Anime Ads Found",404);
    }
}