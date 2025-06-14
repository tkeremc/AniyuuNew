using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Models.AnimeModels;
using Aniyuu.ViewModels;
using Aniyuu.ViewModels.AnimeViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("genre")]

[EnableCors("CorsApi")]
public class GenreController(IMapper mapper,
    IGenreService genreService) : ControllerBase
{
    [HttpGet("get-all")]
    public async Task<ActionResult<List<GenreViewModel>>> GetAll(CancellationToken cancellationToken)
    {
        var genres = await genreService.GetAll(cancellationToken);
        return mapper.Map<List<GenreViewModel>>(genres);
    }

    [HttpGet("get")]
    public async Task<ActionResult<GenreViewModel>> Get(int genreId, CancellationToken cancellationToken)
    {
        if (genreId < 0) return StatusCode(StatusCodes.Status400BadRequest, "Genre ID is invalid.");
        var genre = await genreService.Get(genreId, cancellationToken);
        return mapper.Map<GenreViewModel>(genre);
    }

    [HttpPut("update")]
    public async Task<ActionResult<GenreViewModel>> Update(int genreId, GenreViewModel genreViewModel,
        CancellationToken cancellationToken)
    {
        if (genreId < 0) return StatusCode(StatusCodes.Status400BadRequest, "Genre ID is invalid.");
        var genreModel = mapper.Map<GenreModel>(genreViewModel);
        var result = await genreService.Update(genreId, genreModel, cancellationToken);
        return mapper.Map<GenreViewModel>(result);
    }
    

    [HttpGet("get-anime-with-genre")]
    public async Task<ActionResult<List<AnimeSearchResultViewModel>>> GetAnimeWithGenre(int genreId,
        CancellationToken cancellationToken, int page = 1, int count = 10)
    {
        if (genreId < 0) return StatusCode(StatusCodes.Status400BadRequest, "Genre ID is invalid.");
        if (page < 1) page = 1;
        if (count < 1) count = 1;
        var animes = await genreService.GetAnimesWithGenre(genreId, page, count, cancellationToken);
        return mapper.Map<List<AnimeSearchResultViewModel>>(animes);
    }
}