using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using NotesSolution.Application.Dtos;
using NotesSolution.Core.Models;

namespace NotesSolution.Application
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            // Note mappings
            CreateMap<Note, NoteDto>()
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Name).ToList()));

            CreateMap<NoteCreateDto, Note>()
                .ForMember(dest => dest.Tags, opt => opt.Ignore());

            CreateMap<NoteUpdateDto, Note>()
                .ForMember(dest => dest.Tags, opt => opt.Ignore());

            CreateMap<Tag, TagDto>().ReverseMap();
            CreateMap<TagRequestDto, Tag>();

            CreateMap<IdentityUser, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.UserName));
        }
    }
}
