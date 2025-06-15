using Aniyuu.Models.AnimeModels;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.AnimeInterfaces;

public interface IAnimeService
{ 
    Task<AnimeModel> Get(int malId, CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetAll(int page, int count, CancellationToken cancellationToken);
    Task<List<AnimeModel>> Search(string query, int page, int count, CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetMostPopular(CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetNewAnimes(CancellationToken cancellationToken);
    Task<bool> AddWatchlist(int malId, CancellationToken cancellationToken);
    Task<bool> RemoveWatchlist(int malId, CancellationToken cancellationToken);
    Task<List<AnimeModel>> GetWatchlist(int page, int count, CancellationToken cancellationToken);
}