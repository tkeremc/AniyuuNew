using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.AnimeServices;

public class AnimeService(IMongoDbContext mongoDbContext,
    IHttpClientFactory httpClientFactory) :  IAnimeService
{
    private readonly IMongoCollection<AnimeModel> _animeCollection = mongoDbContext
        .GetCollection<AnimeModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<List<AnimeModel>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var allAnimes = await _animeCollection.Find(x => x.IsActive == true).ToListAsync(cancellationToken);
            if (allAnimes == null || allAnimes.Count == 0)
            {
                Logger.Error("[AnimeService.GetAll] No animes found.");
                throw new AppException("Mongo query failed.", 409);
            }
            return allAnimes;
        }
        catch (Exception e)
        {
            Logger.Error("[AnimeService.GetAll] Mongo query failed.");
            throw new AppException("Mongo query failed.", 500);
        }
    }

    public async Task<AnimeModel> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var anime = await _animeCollection.Find(x => x.Id == id && x.IsActive == true).FirstOrDefaultAsync(cancellationToken);
            if (anime == null)
            {
                Logger.Error("[AnimeService.Get] Anime not found.");
                throw new AppException("Anime not found.", 409);
            }
            return anime;
        }
        catch (Exception e)
        {
            Logger.Error("[AnimeService.Get] Mongo query failed.");
            throw new AppException("Mongo query failed.", 500);
        }
    }

    public async Task<AnimeModel> Create(int malAnimeId, CancellationToken cancellationToken)
    {
        if (await _animeCollection
                .Find(x => x.MALId == malAnimeId && x.IsActive == true)
                .AnyAsync(cancellationToken))
        {
            Logger.Error("[AnimeService.Create] Anime already exist.");
            throw new AppException("Anime already exist.", 409);
        }

        var malData = await GetMalData(malAnimeId, cancellationToken);
        var newAnime = new AnimeModel();
        await InitialModelUpdate(newAnime, malData);

        try
        {
            await _animeCollection.InsertOneAsync(newAnime, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[AnimeService.Create] Mongo instert failed.");
            throw new AppException("Mongo insert failed.", 500);
        }
        
        return newAnime;
    }

    public Task<AnimeModel> Update(AnimeModel animeModel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<AnimeModel> Delete(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    private async Task InitialModelUpdate(AnimeModel animeModel, MALResponseModel malModel)
    {
        animeModel.Title = malModel.Title;
        animeModel.AlternativeTitles = malModel.AlternativeTitles;
        animeModel.Description = await TranslateHelper.Translate(malModel.Synopsis);
        animeModel.BannerLink = malModel.Pictures![0].Large;
        animeModel.BackdropLink = "";
        animeModel.EpisodeCount = malModel.NumEpisodes;
        animeModel.SeasonCount = 0;
        animeModel.Genre = malModel.Genres;
        animeModel.Tags ??= [];
        animeModel.Tags.Add("not set");
        animeModel.ReleaseDate = Convert.ToDateTime(malModel.StartDate);
        animeModel.Status = malModel.Status;
        animeModel.MALId = malModel.Id;
        animeModel.MALScore = malModel.Mean;
        animeModel.MALRank = malModel.Rank;
        animeModel.MALRating = malModel.Rating;
        animeModel.ViewCount = 0;
        animeModel.FavoriteCount = 0;
        animeModel.Slug = SlugHelper.FormatString(animeModel.Title);
        animeModel.BunnyLibraryId = "423517";
        animeModel.IsActive = true;
        animeModel.TrailerLinks ??= [];
    }

    private async Task<MALResponseModel?> GetMalData(int malId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        var clientId = AppSettingConfig.Configuration["MAL:ClientId"]!;
        
        const string fields = "id,title,main_picture,alternative_titles,start_date,end_date,synopsis,mean,rank,popularity,nsfw,media_type,status,genres,num_episodes,start_season,broadcast,source,rating,pictures,related_anime,studios";
        var requestUri = $"https://api.myanimelist.net/v2/anime/{malId}?fields={fields}";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-MAL-CLIENT-ID", clientId);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var animeData = await response.Content.ReadFromJsonAsync<MALResponseModel>(cancellationToken: cancellationToken);
                return animeData;
            }

            var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.Error($"[AnimeService.GetMalData] MalData not found. Message: {errorResponse}");
            throw new AppException("MalData not found.", 404);
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.GetMalData] Api response exception: {e.Message}");
            throw new AppException("Api response exception occurred.", 404);
        }
    }
}