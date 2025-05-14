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
    private readonly IMongoCollection<GenreModel> _animeGenreCollection = mongoDbContext
        .GetCollection<GenreModel>(AppSettingConfig.Configuration["MongoDBSettings:GenreCollection"]!);
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
            Logger.Error("[AnimeService.Get] Anime not found or query failed.");
            throw new AppException("Anime not found or query failed.", 500);
        }
    }

    public async Task<bool> Create(int malAnimeId, string backdropLink, List<string> tags, List<string> trailers,CancellationToken cancellationToken)
    {
        if (await IsAnimeExist(malAnimeId, cancellationToken)!)
        {
            Logger.Error("[AnimeService.Create] Anime already exist.");
            throw new AppException("Anime already exist.", 409);
        }

        var malData = await GetMalData(malAnimeId, cancellationToken);
        var newAnime = new AnimeModel();
        await InitialModelUpdate(newAnime, malData, backdropLink, tags, trailers);

        GenreModel genreModel;
        foreach (var genre in newAnime.Genre)
        {
            genreModel = new GenreModel()
            {
                GenreId = genre.Id,
                GenreName = genre.Name,
                Description = "not set"
            };
            _ = SaveGenre(genreModel, cancellationToken);
        }

        try
        {
            await _animeCollection.InsertOneAsync(newAnime, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[AnimeService.Create] Mongo instert failed.");
            throw new AppException("Mongo insert failed.", 500);
        }
        
        return true;
    }

    public async Task<AnimeModel> Update(int malId, AnimeModel animeModel, CancellationToken cancellationToken, string updatedBy = "system")
    {
        if (!await IsAnimeExist(malId, cancellationToken)!)
        {
            Logger.Error("[AnimeService.Update] Anime not found.");
            throw new AppException("Anime not found.", 409);
        }
        
        var existedAnimeModel = await Get(malId, cancellationToken);
        existedAnimeModel.UpdatedAt = DateTime.UtcNow;
        existedAnimeModel.UpdatedBy = updatedBy;
        animeModel = UpdateCheckHelper.ReplaceNullToOldValues(existedAnimeModel, animeModel);
        try
        {
            var result = await _animeCollection
                .ReplaceOneAsync(x => x.MALId == malId, animeModel, cancellationToken: cancellationToken);
            if (result.ModifiedCount == 0)
            {
                Logger.Error("[AnimeService.Update] Anime not updated.");
                throw new AppException("Anime not updated.", 500);
            }
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.Update] Message: {e}.");
            throw new AppException("Anime update server problem.", 500);
        }
        return animeModel;
    }

    public async Task<bool> Delete(int malId, CancellationToken cancellationToken)
    {
        if (!await IsAnimeExist(malId,cancellationToken)!)
        {
            Logger.Error("[AnimeService.Delete] Anime not found.");
            throw new AppException("Anime not found.", 409);
        }
        var filter = Builders<AnimeModel>.Filter.And(Builders<AnimeModel>.Filter.Eq("MALId", malId),
            Builders<AnimeModel>.Filter.Eq("IsActive", true));
        var update = Builders<AnimeModel>.Update.Set(u => u.IsActive, false);
        
        try
        {
            await _animeCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.Delete] Update failed: {e}.");
            throw new AppException("Anime update failed.", 500);
        }
        return true;
    }
    
    public async Task<bool>? IsAnimeExist(int malId, CancellationToken cancellationToken)
    {
        var result = await _animeCollection.Find(x => x.MALId == malId && x.IsActive == true).AnyAsync(cancellationToken);
        return result;
    }


    private async Task InitialModelUpdate(AnimeModel animeModel, MALResponseModel malModel, string backdropLink, List<string> tags, List<string> trailers)
    {
        animeModel.Title = malModel.Title;
        animeModel.AlternativeTitles = malModel.AlternativeTitles;
        animeModel.Description = await TranslateHelper.Translate(malModel.Synopsis);
        animeModel.BannerLink = malModel.Pictures![0].Large;
        animeModel.BackdropLink = backdropLink;
        animeModel.EpisodeCount = malModel.NumEpisodes;
        animeModel.SeasonCount = 0;
        animeModel.Genre = malModel.Genres;
        animeModel.Tags ??= tags;
        animeModel.ReleaseDate = Convert.ToDateTime(malModel.StartDate);
        animeModel.Status = malModel.Status;
        animeModel.MALId = malModel.Id;
        animeModel.MALScore = malModel.Mean;
        animeModel.MALRank = malModel.Rank;
        animeModel.MALRating = malModel.Rating;
        animeModel.Studios = malModel.Studios;
        animeModel.Source = malModel.Source;
        animeModel.ViewCount = 0;
        animeModel.FavoriteCount = 0;
        animeModel.Slug = SlugHelper.FormatString(animeModel.Title);
        animeModel.BunnyLibraryId = "423517";
        animeModel.IsActive = true;
        animeModel.TrailerLinks ??= trailers;
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

    private async Task SaveGenre(GenreModel genreModel, CancellationToken cancellationToken)
    {
        try
        {
            if (genreModel == null! || await _animeGenreCollection.Find(x => x.GenreId == genreModel.GenreId)
                    .AnyAsync(cancellationToken))
            {
                throw new AppException("Genre already exists or genreModel is null.", 409);
            }

            await _animeGenreCollection.InsertOneAsync(genreModel, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.SaveGenre] Server problem: {e.Message}");
            throw new AppException("Server error.", 500);
        }
    }
}