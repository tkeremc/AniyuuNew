using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.ViewModels.AnimeViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;


[ApiController]
[Route("anime")]
[EnableCors("CorsApi")]
public class AnimeController(IAnimeService animeService,
    IMapper mapper) : ControllerBase
{
    
    [Authorize]
    [HttpGet("get")]
    public async Task<ActionResult<AnimeViewModel>> Get(int malId, CancellationToken cancellationToken)
    {
        var animeModel = await animeService.Get(malId, cancellationToken);
        var animeViewModel = mapper.Map<AnimeViewModel>(animeModel);
        return animeViewModel;
    }

    [Authorize]
    [HttpGet("get-all")]
    public async Task<ActionResult<List<AnimeViewModel>>> GetAll(CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        var animeModel = await animeService.GetAll(page, count, cancellationToken);
        var animeViewModel = mapper.Map<List<AnimeViewModel>>(animeModel);
        return animeViewModel;
    }

    [Authorize]
    [HttpGet("search")]
    public async Task<ActionResult<List<AnimeSearchResultViewModel>>> Search(string query, CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 3)
            return StatusCode(StatusCodes.Status400BadRequest, "Sorgu boş veya 3 karakterden az olamaz.");

        if (page < 1 || count > 50)
            return StatusCode(StatusCodes.Status400BadRequest, "Sayfa 1'den küçük, miktar 50'den fazla olamaz.");
        
        
        var result = await animeService.Search(query, page, count, cancellationToken);
        var animeViewModel = mapper.Map<List<AnimeSearchResultViewModel>>(result);
        return animeViewModel;
    }

    [HttpGet("get-popular")]
    public async Task<ActionResult<List<PopularAnimeViewModel>>> GetPopular(CancellationToken cancellationToken)
    {
        var animes = await animeService.GetMostPopular(cancellationToken);
        const int maxLength = 50;
        foreach (var anime in animes)
        {
            anime.Description = anime.Description!.Length > maxLength ? anime.Description[..maxLength] + "..." : anime.Description;
        }
        var popularAnimeViewModel = mapper.Map<List<PopularAnimeViewModel>>(animes);
        return popularAnimeViewModel;
    }
}
