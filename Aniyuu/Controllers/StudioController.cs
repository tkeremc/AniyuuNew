using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.ViewModels.AnimeViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("studio")]
[Authorize]
[EnableCors("CorsApi")]
public class StudioController(IStudioService studioService,
    IMapper mapper) : ControllerBase
{
    [HttpGet("get-all")]
    public async Task<ActionResult<List<StudioViewModel>>> GetAll(CancellationToken cancellationToken, int page = 1,
        int count = 10)
    {
        if (page < 1)  page = 1;
        if (count < 1) count = 1;
        
        var studios = await studioService.GetAll(page, count, cancellationToken);
        var studioViewModels = mapper.Map<List<StudioViewModel>>(studios);
        return studioViewModels;
    }

    [HttpGet("get")]
    public async Task<ActionResult<StudioViewModel>> Get(int studioId, CancellationToken cancellationToken)
    {
        if (studioId < 1) return StatusCode(StatusCodes.Status400BadRequest, "Studio ID is invalid");
        var studio = await studioService.Get(studioId, cancellationToken);
        var studioViewModel = mapper.Map<StudioViewModel>(studio);
        return studioViewModel;
    }

    [HttpGet("get-animes-with-studio")]
    public async Task<ActionResult<List<AnimeSearchResultViewModel>>> GetAnimesWithStudio(int studioId,
        CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        if (studioId < 1) return StatusCode(StatusCodes.Status400BadRequest, "Studio ID is invalid");
        if (page < 1)  page = 1;
        if (count < 1) count = 1;
        var animes = await studioService.GetAnimesByStudio(studioId, page, count, cancellationToken);
        var animesViewModels = mapper.Map<List<AnimeSearchResultViewModel>>(animes);
        return animesViewModels;
    }
    
}