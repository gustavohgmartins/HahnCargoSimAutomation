﻿using App.Core;
using App.Core.Services;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SimulationController : ControllerBase
    {
        private readonly ILogger<SimulationController> _logger;
        private readonly ISimulationService simulationService;

        public SimulationController(ILogger<SimulationController> logger, ISimulationService simulationService)
        {
            _logger = logger;
            this.simulationService = simulationService;
        }


        [HttpPost]
        [Route("Start")]
        public async Task<IActionResult> Start()
        {
            var token = GetToken(Request);
            var username = GetUsername(Request);

            var response = await simulationService.Start(token, username);

            if (!response)
            {
                return Unauthorized(new { message = "Failed to start simulation" });
            }

            return Ok(new { message = "Failed to start simulation" });
        }

        [HttpPost]
        [Route("Stop")]

        public async Task<IActionResult> Stop()
        {
            var token = GetToken(Request);
            var response = await simulationService.Stop(token);

            if (!response)
            {
                return Unauthorized(new { message = "Failed to start simulation" });
            }

            return Ok(new { message = "Failed to start simulation" });
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

        private string GetUsername(HttpRequest request)
        {
            return request.Headers["Username"];
        }

    }
}
