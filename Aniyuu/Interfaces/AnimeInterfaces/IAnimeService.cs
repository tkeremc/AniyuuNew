using Aniyuu.Models.AnimeModels;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.AnimeInterfaces;

public interface IAnimeService
{
    Task<List<AnimeModel>> GetAll(CancellationToken cancellationToken);
    Task<AnimeModel> Get(int malId, CancellationToken cancellationToken);

    Task<bool> Create(int malAnimeId, string backdropLink, List<string> tags, List<string> trailers,
        CancellationToken cancellationToken);
    Task<AnimeModel> Update(int malId, AnimeModel animeModel, CancellationToken cancellationToken, string updatedBy = "system");
    Task<bool> Delete(int malId, CancellationToken cancellationToken);
    Task<bool>? IsAnimeExist(int malId, CancellationToken cancellationToken);
}