using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Authorization.Users;

namespace SmartPos.Sessions.Dto
{
    [AutoMapFrom(typeof(User))]
    public class UserLoginInfoDto : EntityDto<long>
    {
        public string Name { get; set; }

        public string Surname { get; set; }

        public string UserName { get; set; }

        public string EmailAddress { get; set; }

        public string UserImageUrl { get; set; }

        public int? BranchId { get; set; }

        public string BranchName { get; set; }
    }
}
