using Aniyuu.Models.AnimeModels;

namespace Aniyuu.Interfaces.AnimeInterfaces;

public interface IStudioService
{
    Task<List<StudioModel>> GetAll(int page, int count, CancellationToken cancellationToken);
    Task<StudioModel> Get(int studioId, CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetAnimesByStudio(int studioId, int page, int count, CancellationToken cancellationToken);
}