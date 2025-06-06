using Aniyuu.Models.AnimeModels;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.AnimeInterfaces;

public interface IAnimeService
{ 
    Task<AnimeModel> Get(int malId, CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetAll(int page, int count, CancellationToken cancellationToken);
    Task<List<AnimeModel>> Search(string _query, int page, int count, CancellationToken cancellationToken);
}