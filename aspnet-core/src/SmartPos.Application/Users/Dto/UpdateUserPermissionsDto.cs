using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Users.Dto
{
    public class UpdateUserPermissionsDto
    {
        [Range(1, long.MaxValue)]
        public long Id { get; set; }

        public List<string> GrantedPermissionNames { get; set; }

        public UpdateUserPermissionsDto()
        {
            GrantedPermissionNames = new List<string>();
        }
    }
}
