using Aniyuu.Models.AnimeModels;

namespace Aniyuu.Interfaces;

public interface IAdService
{
    Task<List<AnimeAdModel>> GetAll(CancellationToken cancellationToken);
}