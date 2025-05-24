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
    [HttpGet("get-anime-images")]
    public async Task<ActionResult<List<AnimeImageViewModel>>> GetAnimeImages(CancellationToken cancellationToken)
    {
        var animeModels = await animeService.GetAll(cancellationToken);
        var animeImageModels = mapper.Map<List<AnimeImageViewModel>>(animeModels);
        return animeImageModels;
    }
    
    [Authorize]
    [HttpGet("get")]
    public async Task<AnimeViewModel> Get(int malId, CancellationToken cancellationToken)
    {
        var animeModel = await animeService.Get(malId, cancellationToken);
        var animeViewModel = mapper.Map<AnimeViewModel>(animeModel);
        return animeViewModel;
    }
    
    [Authorize]
    [HttpGet("user/get")]
    public async Task<List<AnimeViewModel>> GetAnime(CancellationToken cancellationToken, int page = 1, int size = 10)
    {
        var animes = await animeService.GetAll(cancellationToken);
        var animeViewModel = mapper.Map<List<AnimeViewModel>>(animes);
        return animeViewModel.Skip((page -1) * size).Take(size).ToList();
    }
    
    [Authorize(Roles = "admin")]
    [HttpGet("admin/is-anime-exists")]
    public async Task<bool> IsAnimeExists(int malAnimeId, CancellationToken cancellationToken)
    {
        return await animeService.IsAnimeExist(malAnimeId, cancellationToken)!;
    }
    
    [Authorize(Roles = "admin")]
    [HttpPost("admin/create-anime")]
    public async Task<ActionResult<bool>> Create([FromBody] AnimeCreateViewModel animeCreateViewModel,
        CancellationToken cancellationToken)
    {
        return await animeService.Create(animeCreateViewModel.MalId,
            animeCreateViewModel.BackdropLink!,
            animeCreateViewModel.Tags,
            animeCreateViewModel.Trailers,
            cancellationToken);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("admin/delete")]
    public async Task<bool> Delete(int malAnimeId, CancellationToken cancellationToken)
    {
        return await animeService.Delete(malAnimeId, cancellationToken);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("admin/update-anime")]
    public async Task<AnimeViewModel> Update(int malId, string updatedBy, [FromBody] AnimeUpdateViewModel animeUpdateViewModel, CancellationToken cancellationToken)
    {
        var animeModel = mapper.Map<AnimeModel>(animeUpdateViewModel);
        var result = await animeService.Update(malId, animeModel, cancellationToken, updatedBy);
        var resultViewModel = mapper.Map<AnimeViewModel>(result);
        return resultViewModel;
    }
}
