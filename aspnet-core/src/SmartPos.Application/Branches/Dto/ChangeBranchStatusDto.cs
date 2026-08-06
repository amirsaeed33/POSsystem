using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Branches.Dto
{
    public class ChangeBranchStatusDto : EntityDto
    {
        [Range(1, int.MaxValue)]
        public int StatusId { get; set; }
    }
}
