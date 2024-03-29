﻿using App.Domain.DTOs;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
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
            var response = await authService.ValidateLogin(GetToken());

            if (!response)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }
            return Ok();
        }

        private string GetToken()
        {
            string token = String.Empty;
            string authorizationHeader = Request.Headers["Authorization"];

            if (authorizationHeader != null)
            {
                token = authorizationHeader.Substring("Bearer ".Length);
            }

            return token;
        }
    }
}
