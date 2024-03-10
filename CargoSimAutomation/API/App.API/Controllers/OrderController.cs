using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly IOrderService orderService;

        public OrderController(IAuthService authService, IOrderService orderService)
        {
            this.authService = authService;
            this.orderService = orderService;
        }

        [HttpPost]
        [Route("Generate")]
        public async Task<IActionResult> Generate()
        {
            var response = await orderService.Generate(GetToken(Request));

            if (!response)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }
            return Ok(response);
        }

        private string GetToken(HttpRequest request)
        {
            string token = String.Empty;
            string authorizationHeader = request.Headers["Authorization"];

            if (authorizationHeader != null)
            {
                token = authorizationHeader.Substring("Bearer ".Length);
            }

            return token;
        }
    }
}
