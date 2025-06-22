using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Models;

namespace NotesSolution.Core
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<Note, NoteDto>()
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Name).ToList()));
            CreateMap<NoteCreateDto, Note>()
                .ForMember(dest => dest.Tags, opt => opt.Ignore());
            CreateMap<NoteUpdateDto, Note>()
                .ForMember(dest => dest.Tags, opt => opt.Ignore());
            CreateMap<Tag, TagDto>().ReverseMap();
            CreateMap<Tag, TagCreateDto>().ReverseMap();
        }
    }
}
