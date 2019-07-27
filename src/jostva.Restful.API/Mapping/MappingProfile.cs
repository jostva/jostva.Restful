#region usings

using AutoMapper;
using jostva.Restful.API.Entities;
using jostva.Restful.API.Helpers;
using jostva.Restful.API.Models;

#endregion

namespace jostva.Restful.API.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Author, AuthorDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.GetCurrentAge()));

            CreateMap<Book, BookDto>();

            CreateMap<AuthorForCreationDto, Author>();

            CreateMap<BookForCreationDto, Book>();

            CreateMap<BookForUpdateDto, Book>();

            CreateMap<Book, BookForUpdateDto>();
        }
    }
}