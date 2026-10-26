using AutoMapper;
using SmartPos.Authorization.Users;
using SmartPos.MultiTenancy;
using SmartPos.Sessions.Dto;

namespace SmartPos.Sessions
{
    public class SessionMapProfile : Profile
    {
        public SessionMapProfile()
        {
            CreateMap<User, UserLoginInfoDto>();
            CreateMap<Tenant, TenantLoginInfoDto>();
        }
    }
}
