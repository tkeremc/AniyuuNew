using Aniyuu.Interfaces.AdminServices;
using Aniyuu.Models.AnimeModels;
using Aniyuu.ViewModels.AdminAdViewModels;
using Aniyuu.ViewModels.AnimeViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Aniyuu.Controllers.AdminController;


[ApiController]
[Route("admin/ads")]
[Authorize(Roles = "admin")]
[EnableCors("CorsApi")]
public class AdminAdController(IMapper mapper,
    IAdminAdService adminAdService) : ControllerBase
{
    [HttpGet("get")]
    public async Task<ActionResult<AnimeAdViewModel>> Get(int malId, CancellationToken cancellationToken)
    {
        if (malId == 0) return StatusCode(StatusCodes.Status400BadRequest, "MalId is required.");
        
        var ad = await adminAdService.Get(malId, cancellationToken);
        var adViewModel = mapper.Map<AnimeAdViewModel>(ad);
        return adViewModel;
    }

    [HttpGet("get-all")]
    public async Task<ActionResult<List<AnimeAdViewModel>>> GetAll(CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        if (page < 1) page = 1;
        if (count < 1) count = 1;
        var ads = await adminAdService.GetAll(page, count, cancellationToken);
        var adViewModel = mapper.Map<List<AnimeAdViewModel>>(ads);
        return adViewModel;
    }

    [HttpPost("create")]
    public async Task<ActionResult<bool>> Create(int malId, string? logoLink, string? backdropLink, CancellationToken cancellationToken)
    {
        if (malId == 0) return StatusCode(StatusCodes.Status400BadRequest, "MalId is required.");
        return await adminAdService.Create(malId, cancellationToken, logoLink, backdropLink);
    }

    [HttpPut("update")]
    public async Task<ActionResult<AnimeAdViewModel>> Update(int malId, AnimeAdUpdateViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (malId == 0)  return StatusCode(StatusCodes.Status400BadRequest, "MalId is required.");
        var model = mapper.Map<AnimeAdModel>(viewModel);
        var result = await adminAdService.Update(malId, model, cancellationToken);
        return mapper.Map<AnimeAdViewModel>(result);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<bool>> Delete(int malId, CancellationToken cancellationToken)
    {
        if (malId == 0) return StatusCode(StatusCodes.Status400BadRequest, "MalId is required.");
        return await adminAdService.Delete(malId, cancellationToken);
    }

}