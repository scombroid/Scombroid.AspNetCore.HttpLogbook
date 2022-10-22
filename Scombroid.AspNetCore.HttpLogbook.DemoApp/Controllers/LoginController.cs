using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {

        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        [HttpPost()]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            return Ok();
        }
    }

    public class LoginRequest
    {
        public string LoginName { get; set; }
        public string Password { get; set; }
    }
}
