using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.ViewModels.AnimeViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;


[ApiController]
[Route("anime/admin")]
[EnableCors("CorsApi")]
[Authorize(Roles = "admin")]
public class AnimeController(IAnimeService animeService) : ControllerBase
{
    [HttpGet("get")]
    public async Task<AnimeModel> Get(int malId, CancellationToken cancellationToken)
    {
        var animeModel = await animeService.Get(malId, cancellationToken);
        return animeModel;
    }

    [HttpGet("get-all")]
    public async Task<List<AnimeModel>> GetAll(CancellationToken cancellationToken)
    {
        var animeModels = await animeService.GetAll(cancellationToken);
        return animeModels;
    }
    
    [HttpPost("create-anime")]
    public async Task<ActionResult<bool>> Create([FromBody] AnimeCreateViewModel animeCreateViewModel,
        CancellationToken cancellationToken)
    {
        return await animeService.Create(animeCreateViewModel.MalId,
            animeCreateViewModel.BackdropLink,
            animeCreateViewModel.Tags,
            animeCreateViewModel.Trailers,
            cancellationToken);
    }

    [HttpDelete("delete")]
    public async Task<bool> Delete(int malAnimeId, CancellationToken cancellationToken)
    {
        return await animeService.Delete(malAnimeId, cancellationToken);
    }

    [HttpGet("is-anime-exists")]
    public async Task<bool> IsAnimeExists(int malAnimeId, CancellationToken cancellationToken)
    {
        return await animeService.IsAnimeExist(malAnimeId, cancellationToken);
    }
}