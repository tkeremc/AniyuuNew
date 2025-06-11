using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces.AdminServices;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.AdminServices;

public class AdminADService(IAnimeService animeService,
    IAdminAnimeService adminAnimeService,
    ICurrentUserService currentUserService,
    IMongoDbContext mongoDbContext) : IAdminADService
{
    private readonly IMongoCollection<AnimeADModel> _animeADCollection = mongoDbContext
        .GetCollection<AnimeADModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeADCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public async Task<bool> CreateAD(int malId, CancellationToken cancellationToken, string logoLink = "not set", string backdropLink = null)
    {
        var isADExist = await Get(malId, cancellationToken);
        if (isADExist != null)
        {
            Logger.Error($"AD already exists with {malId}");
            throw new AppException("AD already exists");
        }
        
        var anime = await animeService.Get(malId, cancellationToken);

        if (anime.BackdropLink == null)
        {
            if (backdropLink == null)
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

        var newAdModel = new AnimeADModel
        {
            Id = "",
            BackdropLink = anime.BackdropLink,
            Title = anime.Title,
            Description = anime.Description,
            LogoLink = logoLink,
            MALId = anime.MALId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = currentUserService.GetUserId()
        };

        try
        {
            await _animeADCollection.InsertOneAsync(newAdModel, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[AdminADService.Create]" + e.Message);
            throw new AppException("An error occured during saving new ad");
        }

        return true;
    }

    public async Task<List<AnimeADModel>> GetAll(int page, int count, CancellationToken cancellationToken)
    {
        var ads = await _animeADCollection.Find(x => x.IsActive == true)
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);
        if (ads.Count != 0)
            return ads;
        Logger.Error("[AdminADService.GetAll] No ads found");
        throw new AppException("[AdminADService.GetAll] No ads found", 404);
    }

    public async Task<AnimeADModel> Get(int malId, CancellationToken cancellationToken)
    {
        var ad = await _animeADCollection
            .Find(x => x.MALId == malId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (ad != null) return ad;
        Logger.Error("[AdminADService.Get] Advertisement not found");
        throw new AppException("Advertisement not found", 404);
    }

    public Task<AnimeADModel> Update(AnimeADModel animeADModel, CancellationToken cancellationToken, string updatedBy = "system")
    {
        throw new NotImplementedException();
    }

    public Task<bool> Delete(int malId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}