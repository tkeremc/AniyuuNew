using Aniyuu.Models.AnimeModels;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.AnimeInterfaces;

public interface IAnimeService
{
    Task<List<AnimeModel>> GetAll(CancellationToken cancellationToken);
    Task<AnimeModel> Get(string id, CancellationToken cancellationToken);
    Task<AnimeModel> Create(int malAnimeId, CancellationToken cancellationToken);
    Task<AnimeModel> Update(AnimeModel animeModel, CancellationToken cancellationToken);
    Task<AnimeModel> Delete(string id, CancellationToken cancellationToken);
}