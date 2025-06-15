using Aniyuu.Interfaces.AnimeInterfaces;
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
    public async Task<ActionResult<List<HelloAnimeViewModel>>> GetPopular(CancellationToken cancellationToken)
    {
        var animes = await animeService.GetMostPopular(cancellationToken);
        var homeAnimeViewModel = mapper.Map<List<HelloAnimeViewModel>>(animes);
        await ShortDesc(homeAnimeViewModel);
        return homeAnimeViewModel;
    }

    [HttpGet("get-new")]
    public async Task<ActionResult<List<HelloAnimeViewModel>>> GetNewAnimes(CancellationToken cancellationToken)
    {
        var animes = await animeService.GetNewAnimes(cancellationToken);
        var animeViewModel = mapper.Map<List<HelloAnimeViewModel>>(animes);
        await ShortDesc(animeViewModel);
        return animeViewModel;
    }

    [Authorize]
    [HttpPost("add-watchlist")]
    public async Task<ActionResult<bool>> AddWatchlist(int malId, CancellationToken cancellationToken)
    {
        if (malId <= 0) return StatusCode(StatusCodes.Status400BadRequest,"Invalid malId");
        var result = await animeService.AddWatchlist(malId, cancellationToken);
        return result;
    }
    
    [Authorize]
    [HttpDelete("remove-watchlist")]
    public async Task<ActionResult<bool>> RemoveWatchlist(int malId, CancellationToken cancellationToken)
    {
        if (malId <= 0) return StatusCode(StatusCodes.Status400BadRequest,"Invalid malId");
        var result = await animeService.RemoveWatchlist(malId, cancellationToken);
        return result;
    }

    [Authorize]
    [HttpGet("get-watchlist")]
    public async Task<ActionResult<List<AnimeSearchResultViewModel>>> GetWatchlist(CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        if (page <= 0) page = 1;
        if (count <= 0) count = 1;
        
        var animes = await animeService.GetWatchlist(page, count, cancellationToken);
        var animeViewModel = mapper.Map<List<AnimeSearchResultViewModel>>(animes);
        const int maxLength = 50;
        foreach (var anime in animeViewModel)
        {
            anime.Description = anime.Description!.Length > maxLength ? 
                anime.Description[..maxLength] + "..." : anime.Description;
        }
        return animeViewModel;
    }

    private static Task ShortDesc(List<HelloAnimeViewModel> list)
    {
        const int maxLength = 50;
        foreach (var anime in list)
        {
            anime.Description = anime.Description!.Length > maxLength ? 
                anime.Description[..maxLength] + "..." : anime.Description;
        }
        return Task.CompletedTask;
    }
}
