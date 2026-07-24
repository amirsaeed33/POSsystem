using System.Threading.Tasks;
using SmartPos.Models.TokenAuth;
using SmartPos.Web.Controllers;
using Shouldly;
using Xunit;

namespace SmartPos.Web.Tests.Controllers
{
    public class HomeController_Tests: SmartPosWebTestBase
    {
        [Fact]
        public async Task Index_Test()
        {
            await AuthenticateAsync(null, new AuthenticateModel
            {
                UserNameOrEmailAddress = "admin",
                Password = "123qwe"
            });

            //Act
            var response = await GetResponseAsStringAsync(
                GetUrl<HomeController>(nameof(HomeController.Index))
            );

            //Assert
            response.ShouldNotBeNullOrEmpty();
        }
    }
}