using AutoMapper;

using Disunity.Client.v1.Models;


namespace Disunity.Store.Entities {

    public class OrgMemberProfile : Profile {

        public OrgMemberProfile() {
            CreateMap<OrgMember, OrgMemberDto>()
                .ForMember(d => d.UserName,
                           m => m.MapFrom(o => o.User.UserName));

        }

    }

}