using Aniyuu.Models.AnimeModels;

namespace Aniyuu.Interfaces.AdminServices;

public interface IAdminADService
{
    Task<bool> CreateAD(int malId, CancellationToken cancellationToken, string logoLink = "not set", string backdropLink = null);
    Task<List<AnimeADModel>> GetAll(int page, int count, CancellationToken cancellationToken);
    Task<AnimeADModel> Get(int malId, CancellationToken cancellationToken);
    Task<AnimeADModel> Update(AnimeADModel animeADModel, CancellationToken cancellationToken, string updatedBy = "system");
    Task<bool> Delete(int malId, CancellationToken cancellationToken);
}