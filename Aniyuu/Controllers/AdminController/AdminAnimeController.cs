using Aniyuu.Interfaces.AdminServices;
using Aniyuu.Models.AnimeModels;
using Aniyuu.ViewModels.AnimeViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers.AdminController;

[ApiController]
[Authorize(Roles = "admin")]
[Route("admin/anime")]
[EnableCors("CorsApi")]
public class AdminAnimeController(IAdminAnimeService adminAnimeService, 
    IMapper mapper) : ControllerBase
{
    [HttpGet("get-all")]
    public async Task<ActionResult<List<AnimeViewModel>>> GetAllAnime(CancellationToken cancellationToken)
    {
        var animes = await adminAnimeService.GetAll(cancellationToken);
        var  animeViewModels = mapper.Map<List<AnimeViewModel>>(animes);
        return animeViewModels;
    }

    [HttpGet("is-anime-exists")]
    public async Task<ActionResult<bool>> IsAnimeExists(int malId, CancellationToken cancellationToken)
    {
        var animeExist = await adminAnimeService.IsAnimeExist(malId, cancellationToken);
        return animeExist;
    }
    
    [HttpPost("create-anime")]
    public async Task<ActionResult<bool>> Create([FromBody] AnimeCreateViewModel animeCreateViewModel,
        CancellationToken cancellationToken)
    {
        return await adminAnimeService.Create(animeCreateViewModel.MalId,
            animeCreateViewModel.BackdropLink!,
            animeCreateViewModel.Tags,
            animeCreateViewModel.Trailers,
            cancellationToken,
            animeCreateViewModel.MalId);
    }
    
    [HttpDelete("delete")]
    public async Task<bool> Delete(int malAnimeId, CancellationToken cancellationToken)
    {
        return await adminAnimeService.Delete(malAnimeId, cancellationToken);
    }

    [HttpPut("update-anime")]
    public async Task<AnimeViewModel> Update(int malId, string updatedBy, [FromBody] AnimeUpdateViewModel animeUpdateViewModel, CancellationToken cancellationToken)
    {
        var animeModel = mapper.Map<AnimeModel>(animeUpdateViewModel);
        var result = await adminAnimeService.Update(malId, animeModel, cancellationToken, updatedBy);
        var resultViewModel = mapper.Map<AnimeViewModel>(result);
        return resultViewModel;
    }
}