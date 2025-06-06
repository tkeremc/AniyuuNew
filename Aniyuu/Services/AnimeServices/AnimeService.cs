using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using MongoDB.Driver.Search;
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
}