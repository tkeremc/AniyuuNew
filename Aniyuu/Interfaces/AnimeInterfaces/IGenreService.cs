using Aniyuu.Models.AnimeModels;

namespace Aniyuu.Interfaces.AnimeInterfaces;

public interface IGenreService
{
    Task<List<GenreModel>> GetAll(CancellationToken cancellationToken);
    Task<GenreModel> Get(int genreId, CancellationToken cancellationToken);
    
    Task<GenreModel> Update(int genreId ,GenreModel genreModel, CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetAnimesWithGenre(int genreId, int page, int count, CancellationToken cancellationToken);
}