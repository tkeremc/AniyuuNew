using Aniyuu.Interfaces;
using Aniyuu.ViewModels.AdminAdViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("ads")]
[EnableCors("CorsApi")]
public class AdController(IAdService adService,
    IMapper mapper) : ControllerBase
{
    [HttpGet("get-all")]
    [Authorize]
    public async Task<List<AnimeAdViewModel>> GetAll(CancellationToken cancellationToken)
    {
        var ads = await adService.GetAll(cancellationToken);
        return mapper.Map<List<AnimeAdViewModel>>(ads);
    }
}