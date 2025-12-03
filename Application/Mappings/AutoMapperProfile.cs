using AutoMapper;
using WebScraping.Application.DTOs;
using WebScraping.Domain.Entities;

namespace WebScraping.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ScreeningHit, HitDto>()
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString()));
        }
    }
}
