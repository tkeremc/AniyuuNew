using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.AnimeServices;

public class GenreService(IMongoDbContext mongoDbContext) : IGenreService
{
    private readonly IMongoCollection<AnimeModel> _animeCollection = mongoDbContext
        .GetCollection<AnimeModel>(AppSettingConfig.Configuration["MongoDBSettings:AnimeCollection"]!);
    private readonly IMongoCollection<GenreModel> _genreCollection = mongoDbContext
        .GetCollection<GenreModel>(AppSettingConfig.Configuration["MongoDBSettings:GenreCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<List<GenreModel>> GetAll(CancellationToken cancellationToken)
    {
        var genres = await _genreCollection.Find(g => true).ToListAsync(cancellationToken);
        if (genres.Count != 0) return genres;
        Logger.Error("[GenreService.GetAll] No genre found");
        throw new AppException("No genre found",404);
    }

    public async Task<GenreModel> Get(int genreId, CancellationToken cancellationToken)
    {
        var genre = await _genreCollection.Find(g => g.GenreId == genreId).FirstOrDefaultAsync(cancellationToken);
        if (genre != null) return genre;
        Logger.Error($"[GenreService.Get] Genre {genreId} not found");
        throw new AppException($"Genre {genreId} not found",404);
    }

    public async Task<GenreModel> Update(int genreId, GenreModel genreModel, CancellationToken cancellationToken)
    {
        var existedGenre = await Get(genreId, cancellationToken);
        
        if (existedGenre == null)
        {
            Logger.Error($"[GenreService.Update] Genre {genreId} not found");
            throw new AppException($"Genre {genreId} not found",404);
        }
        
        genreModel = UpdateCheckHelper.ReplaceNullToOldValues(existedGenre, genreModel);
        
        var filter = Builders<GenreModel>.Filter.Eq(g => g.GenreId, genreId);
        var update = Builders<GenreModel>.Update.Set(g => g.GenreName, genreModel.GenreName)
            .Set(g => g.Description, genreModel.Description);
        try
        {
            var result = await _genreCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            if (result.ModifiedCount == 0)
            {
                throw new AppException($"Genre {genreId} not updated");
            }
        }
        catch (Exception e)
        {
            Logger.Error("[GenreService.Update]" + e.Message);
            throw new AppException($"Genre {genreId} not updated", 500);
        }
        
        return genreModel;
        
    }

    public async Task<List<AnimeModel>> GetAnimesWithGenre(int genreId, int page, int count, CancellationToken cancellationToken)
    {
        var animes = await _animeCollection
            .Find(x => x.Genre!.Any(a => a.Id == genreId))
            .Skip((page - 1) * count)
            .Limit(count)
            .ToListAsync(cancellationToken);
        if (animes.Count != 0) return animes;
        Logger.Error("[GenreService.GetAnimesWithGenre] No anime found");
        throw new AppException("No anime found",404);
    }
}