using Microsoft.AspNetCore.Mvc;
using FitnessTrackerBack.Services;
using FitnessTrackerBack.DTO;
using FitnessTrackerBack.Services.Interfaces;

namespace FitnessTrackerBack.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService) // Сюда DI передает экземпляр класса UserService (interface = class), далее конструктор
        {
            _userService = userService;
        }

        // api/user/profile/{username}

        [HttpGet("profile/{username}")]
        public async Task<IActionResult> GetProfile(string username)
        {
            var profile = await _userService.GetProfileAsync(username);

            if (profile == null) return NotFound("User not found");

            return Ok(profile);


        }

        //api/user/register(JSON body)

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(LoginRequest loginRequestJSON)
        {
            var result = await _userService.LoginAsync(loginRequestJSON);

            if (!result.Success) return NotFound(result.Message);

            return Ok(result.Message);
        }

        //api/user/register(JSON body)

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(RegisterRequest registerRequestJSON)
        {
            var result = await _userService.RegisterAsync(registerRequestJSON);

            if (!result.Success) return NotFound(result.Message);

            return Ok(result.Message);


        }
    }


}
