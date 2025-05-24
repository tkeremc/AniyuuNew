using Aniyuu.Models.AnimeModels;

namespace Aniyuu.Interfaces.AdminServices;

public interface IAdminAnimeService
{
    Task<List<AnimeModel>> GetAll(CancellationToken cancellationToken);
    Task<bool> Create(int malAnimeId, string backdropLink, List<string> tags, List<string> trailers,
        CancellationToken cancellationToken);
    Task<AnimeModel> Update(int malId, AnimeModel animeModel, CancellationToken cancellationToken, string updatedBy = "system");
    Task<bool> Delete(int malId, CancellationToken cancellationToken);
    Task<bool> IsAnimeExist(int malId, CancellationToken cancellationToken);
}