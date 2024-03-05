using App.Domain.DTO;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService authService;

        public AuthController(ILogger<AuthController> logger, IAuthService authService)
        {
            _logger = logger;
            this.authService = authService;
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] AuthDto auth)
        {
            var response = await authService.Login(auth);

            if (response == default)
            {
                return Unauthorized(new { message = "Incorrect password" });
            }
            return Ok(response);
        }

        [HttpGet]
        [Route("ValidateLogin")]

        public async Task<IActionResult> ValidateLogin()
        {
            string token = String.Empty;
            string authorizationHeader = Request.Headers["Authorization"];

            if (authorizationHeader != null)
            {
                token = authorizationHeader.Substring("Bearer ".Length);
            }


            var response = await authService.ValidateLogin(token);

            if (!response)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }
            return Ok();
        }
    }
}
