using Aniyuu.Models;
using Aniyuu.Models.UserModels;
using Aniyuu.ViewModels.UserViewModels;
using AutoMapper;

namespace Aniyuu.Mappers;

public class UserProfile : Profile
{
    public UserProfile()
    {
        AllowNullCollections = true;
        CreateMap<UserViewModel, UserModel>().ReverseMap();
        CreateMap<UserCreateViewModel, UserModel>().ReverseMap();
        CreateMap<UserUpdateViewModel, UserModel>().ReverseMap();
        CreateMap<UserLoginViewModel, UserModel>().ReverseMap();
        CreateMap<UserPasswordUpdateViewModel, UserModel>().ReverseMap();
        CreateMap<TokensViewModel, TokensModel>().ReverseMap();
    }
}