using App.Core.Clients;
using App.Core.Services;
using App.Domain.DTO;
using App.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly AuthService authService;

        public AuthController(ILogger<AuthController> logger, AuthService authService)
        {
            _logger = logger;
            this.authService = authService;
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] AuthDto auth )
        {
            var response = await authService.Login(auth);

            if (response == default)
            {
                return Unauthorized(new { message = "Username or password is incorrect" });
            }
            return  Ok(response);
        }

        [HttpGet]
        [Route("VerifyLogin")]

        public async Task<IActionResult> VerifyLogin()
        {
            var response = await authService.VerifyLogin();

            if (response == default)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }
            return Ok(authService.Authinfo);
        }
    }
}
