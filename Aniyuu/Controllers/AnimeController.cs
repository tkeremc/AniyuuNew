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
    public async Task<AnimeViewModel> Get(int malId, CancellationToken cancellationToken)
    {
        var animeModel = await animeService.Get(malId, cancellationToken);
        var animeViewModel = mapper.Map<AnimeViewModel>(animeModel);
        return animeViewModel;
    }

    [Authorize]
    [HttpGet("get-all")]
    public async Task<List<AnimeViewModel>> GetAll(CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        var animeModel = await animeService.GetAll(page, count, cancellationToken);
        var animeViewModel = mapper.Map<List<AnimeViewModel>>(animeModel);
        return animeViewModel;
    }

    [HttpGet("search")]
    public async Task<List<AnimeSearchResultViewModel>> Search(string query, CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        var result = await animeService.Search(query, page, count, cancellationToken);
        var animeViewModel = mapper.Map<List<AnimeSearchResultViewModel>>(result);
        return animeViewModel;
    }
}
