using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreditService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CreditService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClaimController : ControllerBase
    {

        private readonly ILogger<ClaimController> _logger;

        public ClaimController(ILogger<ClaimController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Claim> ListActiveClaims()
        {
            var result = new List<Claim>();
            return result;
        }
    }
}
