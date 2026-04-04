using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Trickler_API.Constants;
using Trickler_API.Controllers;
using Trickler_API.Data;
using Trickler_API.DTO;
using Trickler_API.Services;

namespace Trickler_API.Tests.Controllers
{
    public class UserDetailsControllerTests : IDisposable
    {
        private readonly TricklerDbContext _context;
        private readonly UserDetailsService _service;

        public UserDetailsControllerTests()
        {
            var options = new DbContextOptionsBuilder<TricklerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TricklerDbContext(options);
            _service = new UserDetailsService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        // GET endpoint removed; tests for it have been removed.

        [Fact]
        public async Task Post_GetOrCreate_MissingNameIdentifier_ReturnsNotFound()
        {
            var controller = new UserDetailsController(_service, Mock.Of<ILogger<UserDetailsController>>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } }
            };

            var result = await controller.GetOrCreateUserDetails("owner-id");

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var msg = Assert.IsType<MessageResponse>(notFound.Value);
            Assert.Equal(MessageConstants.Account.UserNotFound, msg.Message);
        }

        [Fact]
        public async Task Post_GetOrCreate_AdminRole_AllowsAccess_ReturnsOk()
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "other-id"),
                new Claim(ClaimTypes.Role, RoleConstants.Admin)
            ], "Test");
            var controller = new UserDetailsController(_service, Mock.Of<ILogger<UserDetailsController>>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } }
            };

            var result = await controller.GetOrCreateUserDetails("owner-id");

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UserDetailsResponseDto>(ok.Value);
            Assert.Equal("owner-id", dto.UserId);
        }

        [Fact]
        public async Task Post_GetOrCreate_NonAdminDifferentUserId_ReturnsNotFound()
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "other-id")
            ], "Test");
            var controller = new UserDetailsController(_service, Mock.Of<ILogger<UserDetailsController>>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } }
            };

            var result = await controller.GetOrCreateUserDetails("owner-id");

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var msg = Assert.IsType<MessageResponse>(notFound.Value);
            Assert.Equal(MessageConstants.Account.UserNotFound, msg.Message);
        }

        [Fact]
        public async Task Post_GetOrCreate_OwnerSameUserId_ReturnsOk()
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "user-a")
            ], "Test");
            var controller = new UserDetailsController(_service, Mock.Of<ILogger<UserDetailsController>>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } }
            };

            var result = await controller.GetOrCreateUserDetails("user-a");

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UserDetailsResponseDto>(ok.Value);
            Assert.Equal("user-a", dto.UserId);
        }
    }
}
