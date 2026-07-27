using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Users.Dto
{
    public class UpdateUserPermissionsDto
    {
        [Range(1, long.MaxValue)]
        public long Id { get; set; }

        [Required]
        public List<string> GrantedPermissionNames { get; set; }
    }
}
