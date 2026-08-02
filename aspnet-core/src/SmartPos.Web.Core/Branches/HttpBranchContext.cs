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
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    return null;
                }

                if (!httpContext.Request.Headers.TryGetValue(BranchConsts.BranchIdHeaderName, out var values))
                {
                    return null;
                }

                var raw = values.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                return int.TryParse(raw, out var branchId) && branchId > 0 ? branchId : (int?)null;
            }
        }
    }
}
