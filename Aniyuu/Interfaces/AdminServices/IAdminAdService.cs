using Aniyuu.Models.AnimeModels;

namespace Aniyuu.Interfaces.AdminServices;

public interface IAdminAdService
{
    Task<bool> Create(int malId, CancellationToken cancellationToken, string logoLink = "not set", string backdropLink = null);
    Task<List<AnimeAdModel>> GetAll(int page, int count, CancellationToken cancellationToken);
    Task<AnimeAdModel> Get(int malId, CancellationToken cancellationToken);
    Task<AnimeAdModel> Update(int malId, AnimeAdModel animeADModel, CancellationToken cancellationToken, string updatedBy = "system");
    Task<bool> Delete(int malId, CancellationToken cancellationToken);
}