using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.AnimeServices;

public class StudioService(IMongoDbContext mongoDbContext) : IStudioService
{
    private readonly IMongoCollection<AnimeModel> _animeCollection = mongoDbContext
        .GetCollection<AnimeModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeCollection"]!);
    private readonly IMongoCollection<StudioModel> _studioCollection = mongoDbContext
        .GetCollection<StudioModel>(AppSettingConfig.Configuration["MongoDBSettings:StudioCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<List<StudioModel>> GetAll(int page, int count, CancellationToken cancellationToken)
    {
        var studios = await  _studioCollection
            .Find(x=> true)
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);

        if (studios.Count != 0) return studios;
        Logger.Error("No studios found");
        throw new AppException("No studios found",404);
    }

    public async Task<StudioModel> Get(int studioId, CancellationToken cancellationToken)
    {
        var studio = await _studioCollection
            .Find(x=>x.StudioId == studioId)
            .FirstOrDefaultAsync(cancellationToken);
        if (studio != null) return studio;
        Logger.Error("Studio not found");
        throw new AppException("Studio not found",404);
    }

    public async Task<List<AnimeModel>> GetAnimesByStudio(int studioId, int page, int count, CancellationToken cancellationToken)
    {
        var animes = await _animeCollection
            .Find(x => x.IsActive && x.Studios!.Any(s => s.Id == studioId))
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);

        if (animes.Count != 0) return animes;
        Logger.Info("No results.");
        throw new AppException("No results.", 404);
    }
}