using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using NLog;

namespace Aniyuu.Services.AnimeServices;

public class AnimeService(IMongoDbContext mongoDbContext,
    ICurrentUserService currentUserService) :  IAnimeService
{
    private readonly IMongoCollection<AnimeModel> _animeCollection = mongoDbContext
        .GetCollection<AnimeModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeCollection"]!);
    private readonly IMongoCollection<WatchlistModel> _watchlistCollection = mongoDbContext
        .GetCollection<WatchlistModel>(AppSettingConfig.Configuration["MongoDBSettings:WatchlistCollection"]!);
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

    public async Task<List<AnimeModel>> Search(string query, int page, int count, CancellationToken cancellationToken)
    {
        var fuzzyOptions = new SearchFuzzyOptions
        {
            MaxEdits = 1,
            PrefixLength = 3
        };
        
        var search = Builders<AnimeModel>.Search.Compound()
            .Must(
                Builders<AnimeModel>.Search.Autocomplete(
                    x => x.Title,
                    query,
                    SearchAutocompleteTokenOrder.Any,
                    fuzzyOptions
                )
                )
            .Should(
                Builders<AnimeModel>.Search.Text(x => x.Slug, query),
                Builders<AnimeModel>.Search.Text("AlternativeTitles.En", query),
                Builders<AnimeModel>.Search.Text("AlternativeTitles.Ja", query),
                Builders<AnimeModel>.Search.Text("AlternativeTitles.Synonyms", query)
            )
            .Filter(Builders<AnimeModel>.Search.Equals(x => x.IsActive, true));

        var results = await _animeCollection.Aggregate()
            .Search(search)
            .Skip((page -1) * count)
            .SortByDescending(x => x.SearchScore)
            .Limit(count)
            .ToListAsync(cancellationToken: cancellationToken);

        if (results.Count != 0) return results;
        Logger.Info("No results.");
        throw new AppException("No results.", 404);
    }

    public async Task<List<AnimeModel>> GetMostPopular(CancellationToken cancellationToken)
    {
        var animes = await _animeCollection
            .Find(x => true)
            .SortByDescending(x => x.MALScore)
            .Limit(12)
            .ToListAsync(cancellationToken);
        if (animes.Count != 0)
            return animes;
        Logger.Info("No results.");
        throw new AppException("No results.", 404);
    }

    public async Task<List<AnimeModel>> GetNewAnimes(CancellationToken cancellationToken)
    {
        var animes = await _animeCollection
            .Find(x => true)
            .SortByDescending(x => x.ReleaseDate)
            .Limit(12)
            .ToListAsync(cancellationToken);
        if (animes.Count != 0)
            return animes;
        Logger.Info("No results.");
        throw new AppException("No results.", 404);
    }

    public async Task<bool> AddWatchlist(int malId, CancellationToken cancellationToken)
    {
        return await UpdateWatchlist(malId, 1, cancellationToken);
    }

    public async Task<bool> RemoveWatchlist(int malId, CancellationToken cancellationToken)
    {
        return await UpdateWatchlist(malId, 2, cancellationToken);
    }

    public async Task<List<AnimeModel>> GetWatchlist(int page, int count, CancellationToken cancellationToken)
    {
        var watchlist = await _watchlistCollection.Find(x => x.UserId == currentUserService.GetUserId()).FirstOrDefaultAsync(cancellationToken);
        if (watchlist == null)
        {
            Logger.Error("[AnimeService.AddWatchlist] Watchlist not found.");
            throw new AppException("Watchlist not found.", 404);
        }

        if (watchlist.AnimeMALIds!.Count == 0)
        {
            Logger.Error("[AnimeService.AddWatchlist] watchlist is empty.");
            throw new AppException("Watchlist is empty.", 404);
        }

        var animeList = new List<AnimeModel>();

        foreach (var anime in watchlist.AnimeMALIds)
        {
            var animeModel = await Get(anime, cancellationToken);
            animeList.Add(animeModel);
        }
        return animeList.Skip((page - 1) * count).Take(count).ToList();
    }

    private async Task<bool> UpdateWatchlist(int malId, int process,  CancellationToken cancellationToken)
    {
        var isAnimeExist = await _animeCollection.Find(x => x.MALId == malId && x.IsActive == true)
            .AnyAsync(cancellationToken);
        if (!isAnimeExist)
        {
            Logger.Error("Anime not found.");
            throw new AppException("Anime not found.", 404);
        }
        var filter = Builders<WatchlistModel>.Filter.Eq("UserId", currentUserService.GetUserId());
        var watchlist = await _watchlistCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (watchlist == null)
        {
            Logger.Error("[AnimeService.AddWatchlist] Watchlist not found.");
            throw new AppException("Watchlist not found.", 404);
        }

        UpdateDefinition<WatchlistModel> update;
        switch (process)
        {
            case 1:
                if (watchlist.AnimeMALIds!.Any(x => x.Equals(malId)))
                {
                    Logger.Error("[AnimeService.AddWatchlist] MalId already exist.");
                    throw new AppException("MalId already exist.", 404);
                }
                
                update = Builders<WatchlistModel>.Update.Push("AnimeMALIds", malId)
                .Set(x => x.UpdatedBy, currentUserService.GetUserId())
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
                break;
            case 2:
                if (!watchlist.AnimeMALIds!.Any(x => x.Equals(malId)))
                {
                    Logger.Error("[AnimeService.AddWatchlist] MalId is not exist.");
                    throw new AppException("MalId is not exist.", 404);
                }
                
                update = Builders<WatchlistModel>.Update.Pull("AnimeMALIds", malId)
                    .Set(x => x.UpdatedBy, currentUserService.GetUserId())
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);
                break;
            default:
                Logger.Error("[AnimeService.AddWatchlist] Process not supported.");
                throw new AppException("Process not supported.", 404);
        }
        
        try
        {
            var result = await _watchlistCollection.UpdateOneAsync(filter, update, null, cancellationToken);
            if (result.ModifiedCount == 0)
            {
                throw new AppException("Error during updating watchlist", 500);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
            throw new AppException("Error during updating watchlist", 500);
        }
        return true;
    }
}