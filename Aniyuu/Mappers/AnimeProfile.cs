using Aniyuu.Models.AnimeModels;
using Aniyuu.ViewModels.AdminAdViewModels;
using Aniyuu.ViewModels.AnimeViewModels;
using AutoMapper;

namespace Aniyuu.Mappers;

public class AnimeProfile :  Profile
{
    public AnimeProfile()
    {
        AllowNullCollections = true;
        CreateMap<AnimeCreateViewModel, AnimeModel>().ReverseMap();
        CreateMap<AnimeUpdateViewModel, AnimeModel>().ReverseMap();
        CreateMap<AnimeViewModel, AnimeModel>().ReverseMap();
        CreateMap<AnimeImageViewModel, AnimeModel>().ReverseMap();
        CreateMap<AnimeSearchResultViewModel, AnimeModel>().ReverseMap();
        CreateMap<HelloAnimeViewModel, AnimeModel>().ReverseMap();



        CreateMap<AnimeAdUpdateViewModel, AnimeAdModel>().ReverseMap();
        CreateMap<AnimeAdViewModel, AnimeAdModel>().ReverseMap();
    }
}