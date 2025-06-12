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

public class AdminAdService(IAnimeService animeService,
    IAdminAnimeService adminAnimeService,
    ICurrentUserService currentUserService,
    IMongoDbContext mongoDbContext) : IAdminAdService
{
    private readonly IMongoCollection<AnimeAdModel> _animeAdCollection = mongoDbContext
        .GetCollection<AnimeAdModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeAdCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public async Task<bool> Create(int malId, CancellationToken cancellationToken, string logoLink = "not set", string backdropLink = null)
    {
        var existedAd = await _animeAdCollection.Find(x => x.MALId == malId && x.IsActive).FirstOrDefaultAsync(cancellationToken);
        if (existedAd != null)
        {
            Logger.Error($"AD already exists with {malId}");
            throw new AppException("AD already exists");
        }
        
        var anime = await animeService.Get(malId, cancellationToken);

        if (string.IsNullOrEmpty(anime.BackdropLink))
        {
            if (string.IsNullOrEmpty(backdropLink))
            {
                Logger.Error("AnimeModel has not backdropLink, need to set new one.");
                throw new AppException("AnimeModel has not backdropLink, need to set new one.", 409);
            }
            
            anime.BackdropLink = backdropLink;
            try
            {
                var updatedAnime = new AnimeModel
                {
                    BackdropLink = backdropLink
                };
                _ = await adminAnimeService.Update(malId, updatedAnime, cancellationToken, currentUserService.GetUserId());
            }
            catch (Exception e)
            {
                Logger.Error($"[AdminADService.Create] Error occured while updating anime.BackdropLink: {e.Message}");
            }
        }

        var newAdModel = new AnimeAdModel
        {
            Id = "",
            BackdropLink = anime.BackdropLink,
            Title = anime.Title,
            Description = anime.Description,
            LogoLink = logoLink,
            MALId = anime.MALId,
            Genres = anime.Genre,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = currentUserService.GetUserId()
        };
        
        try
        {
            await _animeAdCollection.InsertOneAsync(newAdModel, new InsertOneOptions(), cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[AdminADService.Create]" + e.Message);
            throw new AppException("An error occured during saving new ad");
        }

        return true;
    }

    public async Task<List<AnimeAdModel>> GetAll(int page, int count, CancellationToken cancellationToken)
    {
        var ads = await _animeAdCollection.Find(x => x.IsActive == true)
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);
        if (ads.Count != 0)
            return ads;
        Logger.Error("[AdminADService.GetAll] No ads found");
        throw new AppException("[AdminADService.GetAll] No ads found", 404);
    }

    public async Task<AnimeAdModel> Get(int malId, CancellationToken cancellationToken)
    {
        if (!await adminAnimeService.IsAnimeExist(malId, cancellationToken))
        {
            Logger.Error("[AdminADService.Get] Anime not found");
            throw new AppException("Anime not found", 404);
        }
        
        var ad = await _animeAdCollection
            .Find(x => x.MALId == malId && x.IsActive == true)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (ad != null) return ad;
        Logger.Error("[AdminADService.Get] Advertisement not found");
        throw new AppException("Advertisement not found", 404);
    }

    public async Task<AnimeAdModel> Update(int malId, AnimeAdModel animeADModel, CancellationToken cancellationToken, string updatedBy = "system")
    {
        var existedAdModel = await Get(malId, cancellationToken);
        
        if (existedAdModel == null)
        {
            Logger.Error("[AdminADService.Update] Ad not found");
            throw new AppException("Ad not found", 404);
        }
        
        existedAdModel.UpdatedAt = DateTime.UtcNow;
        existedAdModel.UpdatedBy = updatedBy;
        animeADModel = UpdateCheckHelper.ReplaceNullToOldValues(existedAdModel, animeADModel);

        try
        {
            var result = await _animeAdCollection.ReplaceOneAsync(x => x.Id == animeADModel.Id, animeADModel, cancellationToken: cancellationToken);
            if (result.MatchedCount == 0 || result.ModifiedCount == 0 || result.IsAcknowledged == false)
            {
                Logger.Error($"[AdminADService.Update] Error occured while updating ad. {result.MatchedCount}/{result.ModifiedCount}/{result.IsAcknowledged}");
                throw new AppException("Update ad error", 404);
            }
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, 404);
        }
        
        return animeADModel;
    }

    public async Task<bool> Delete(int malId, CancellationToken cancellationToken)
    {
        var existedAdModel = await Get(malId, cancellationToken);
        if (existedAdModel == null)
        {
            Logger.Error("[AdminADService.Delete] Anime not found");
            throw new AppException("Anime not found", 404);
        }
        
        var filter = Builders<AnimeAdModel>.Filter.And(
            Builders<AnimeAdModel>.Filter.Eq(x=>x.IsActive,true),
            Builders<AnimeAdModel>.Filter.Eq(x => x.MALId, malId));
        var update = Builders<AnimeAdModel>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, currentUserService.GetUserId())
            .Set(x => x.IsActive, false);

        try
        {
            await _animeAdCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[AnimeADService.Delete] Error occured while deleting ad]");
            throw new AppException(e.Message, 404);
        }
        
        return true;
    }
}