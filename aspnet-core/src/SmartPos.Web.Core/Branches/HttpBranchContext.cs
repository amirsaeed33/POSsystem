using Abp.Dependency;
using Microsoft.AspNetCore.Http;
using SmartPos.Branches;

namespace SmartPos.Web.Branches
{
    public class HttpBranchContext : IBranchContext, ITransientDependency
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpBranchContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? BranchId
        {
            get
            {
                var header = _httpContextAccessor.HttpContext?.Request?.Headers[BranchConsts.BranchIdHeaderName];
                if (string.IsNullOrWhiteSpace(header))
                {
                    return null;
                }

                return int.TryParse(header, out var branchId) ? branchId : (int?)null;
            }
        }
    }
}
