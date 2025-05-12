using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;


[ApiController]
[Route("anime")]
[EnableCors("CorsApi")]
public class AnimeController(IAnimeService animeService) : ControllerBase
{
    [HttpPost("create-anime")]
    public async Task<ActionResult<AnimeModel>> Create(int malAnimeId, CancellationToken cancellationToken)
    {
        return await animeService.Create(malAnimeId, cancellationToken);
    }
}