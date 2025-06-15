using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.AdminServices;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.AdminServices;

public class AdminAnimeService(IMongoDbContext mongoDbContext,
    IHttpClientFactory httpClientFactory,
    IAnimeService animeService,
    ICurrentUserService currentUserService) :  IAdminAnimeService
{
    private readonly IMongoCollection<AnimeModel> _animeCollection = mongoDbContext
        .GetCollection<AnimeModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeCollection"]!);
    private readonly IMongoCollection<GenreModel> _animeGenreCollection = mongoDbContext
        .GetCollection<GenreModel>(AppSettingConfig.Configuration["MongoDBSettings:GenreCollection"]!);
    private readonly IMongoCollection<StudioModel> _studioCollection = mongoDbContext
        .GetCollection<StudioModel>(AppSettingConfig.Configuration["MongoDBSettings:StudioCollection"]!);
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
            Logger.Error("[AnimeService.GetAll] Mongo query failed. " + e.Message);
            throw new AppException("Mongo query failed.", 500);
        }
    }

    public async Task<bool> Create(int malAnimeId, string backdropLink, List<string> tags, List<string> trailers,CancellationToken cancellationToken)
    {
        if (await IsAnimeExist(malAnimeId, cancellationToken))
        {
            Logger.Error("[AnimeService.Create] Anime already exist.");
            throw new AppException("Anime already exist.", 409);
        }

        var malData = await GetMalData(malAnimeId, cancellationToken);
        var newAnime = new AnimeModel();
        await InitialModelUpdate(newAnime, malData!, backdropLink, tags, trailers);

        foreach (var genreModel in newAnime.Genre!.Select(genre => new GenreModel
                 {
                     GenreId = genre.Id,
                     GenreName = genre.Name,
                     Description = "not set"
                 })) _ = SaveGenre(genreModel, cancellationToken);

        foreach (var studioModel in newAnime.Studios!.Select(studio => new StudioModel
                 {
                     StudioId = studio.Id,
                     StudioName = studio.Name
                 })) _ = SaveStudio(studioModel, cancellationToken);

        var prequelAnime = malData.RelatedAnime.Find(x => x.RelationType == "prequel");
        if (prequelAnime == null) throw new AppException("Prequel anime not found.", 404);
        var prequelAnimeData = await GetMalData(prequelAnime.Node!.Id, cancellationToken);
        if (!await IsAnimeExist(prequelAnimeData!.Id, cancellationToken))
        {
            if (prequelAnimeData.MediaType == "tv")
            {
                Logger.Error($"[AnimeService.Create] There's an anime that needs to be added first. MalId: {prequelAnime.Node.Id}");
                throw new AppException($"There's an anime that needs to be added first. MalId: {prequelAnime.Node.Id}", 409);
            }
        }
        else
        {
            var previousSeason = await animeService.Get(prequelAnimeData.Id, cancellationToken);
            previousSeason.Seasons!.Add(newAnime.MALId!.Value);
            await Update(previousSeason.MALId!.Value, previousSeason,cancellationToken);
        }
        
        // var sequelAnime = malData.RelatedAnime.Find(x => x.RelationType == "sequel");
        // if (sequelAnime == null) throw new AppException("Sequel anime not found.", 404);
        // var sequelAnimeData = await GetMalData(sequelAnime.Node!.Id, cancellationToken);
        // if (!await IsAnimeExist(sequelAnimeData!.Id, cancellationToken))
        // {
        //     
        // }
        
        try
        {
            await _animeCollection.InsertOneAsync(newAnime, new InsertOneOptions(), cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[AnimeService.Create] Mongo instert failed. " + e.Message);
            throw new AppException("Mongo insert failed.", 500);
        }
        
        return true;
    }

    public async Task<AnimeModel> Update(int malId, AnimeModel animeModel, CancellationToken cancellationToken, string updatedBy = "system")
    {
        if (!await IsAnimeExist(malId, cancellationToken))
        {
            Logger.Error("[AnimeService.Update] Anime not found.");
            throw new AppException("Anime not found.", 409);
        }
        
        var existedAnimeModel = await animeService.Get(malId, cancellationToken);
        existedAnimeModel.UpdatedAt = DateTime.UtcNow;
        existedAnimeModel.UpdatedBy = updatedBy;
        animeModel = UpdateCheckHelper.ReplaceNullToOldValues(existedAnimeModel, animeModel);
        try
        {
            var result = await _animeCollection
                .ReplaceOneAsync(x => x.MALId == malId, animeModel, cancellationToken: cancellationToken);
            if (result.ModifiedCount == 0)
            {
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
        if (!await IsAnimeExist(malId,cancellationToken))
        {
            Logger.Error("[AnimeService.Delete] Anime not found.");
            throw new AppException("Anime not found.", 409);
        }
        var filter = Builders<AnimeModel>.Filter.And(Builders<AnimeModel>.Filter.Eq("MALId", malId),
            Builders<AnimeModel>.Filter.Eq("IsActive", true));
        var update = Builders<AnimeModel>.Update.Set(u => u.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, currentUserService.GetUserId());
        
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

    public async Task<bool> IsAnimeExist(int malId, CancellationToken cancellationToken)
    {
        var result = await _animeCollection.Find(x => x.MALId == malId && x.IsActive == true).AnyAsync(cancellationToken);
        return result;
    }


    private async Task InitialModelUpdate(AnimeModel animeModel, MALResponseModel malModel, string backdropLink, List<string> tags, List<string> trailers)
    {
        animeModel.Title = malModel.Title;
        animeModel.AlternativeTitles = malModel.AlternativeTitles;
        animeModel.Description = await TranslateHelper.Translate(malModel.Synopsis!);
        animeModel.BannerLink = malModel.Pictures![0].Large;
        animeModel.BackdropLink = backdropLink;
        animeModel.EpisodeCount = 0;
        animeModel.SeasonCount = 0;
        animeModel.Genre = malModel.Genres;
        animeModel.Tags ??= tags;
        animeModel.ReleaseDate = Convert.ToDateTime(malModel.StartDate);
        animeModel.Status = malModel.Status;
        animeModel.Seasons ??= [];
        animeModel.Seasons.Add(malModel.Id);
        animeModel.MALId = malModel.Id;
        animeModel.SeasonNumber = 0;
        animeModel.MALScore = malModel.Mean;
        animeModel.MALRank = malModel.Rank;
        animeModel.MALRating = malModel.Rating;
        animeModel.Studios = malModel.Studios;
        animeModel.Source = malModel.Source;
        animeModel.ViewCount = 0;
        animeModel.FavoriteCount = 0;
        animeModel.Slug = SlugHelper.FormatString(animeModel.Title!);
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
            genreModel.GenreName = await TranslateHelper.Translate(genreModel.GenreName);
            await _animeGenreCollection.InsertOneAsync(genreModel, new InsertOneOptions(), cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.SaveGenre] Server problem: {e.Message}");
            throw new AppException("Server error.", 500);
        }
    }

    private async Task SaveStudio(StudioModel studioModel, CancellationToken cancellationToken)
    {
        try
        {
            if (studioModel == null! || await _studioCollection.Find(x => x.StudioId == studioModel.StudioId)
                    .AnyAsync(cancellationToken))
            {
                throw new AppException("Studio already exists or studioModel is null.", 409);
            }

            await _studioCollection.InsertOneAsync(studioModel, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[AnimeService.SaveStudio] Server problem: {e.Message}");
            throw new AppException("Server error.", 500);
        }
    }
}