using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEApi.Data;
using CEApi.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace CEApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly MsSqlDatabaseContext _context;

        public AuthenticationController(MsSqlDatabaseContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserAccount>> Login(LoginData loginData)
        {
            if (string.IsNullOrEmpty(loginData.userName) || string.IsNullOrEmpty(loginData.password))
            {
                return BadRequest("Invalid login data.");
            }

            // Normalize the username to lower case and trim whitespace
            loginData.userName = loginData.userName.ToLower().Trim();

            var existingUser = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.userName.ToLower() == loginData.userName);

            if (existingUser == null)
            {
                return NotFound();
            }

            if (!BCrypt.Net.BCrypt.Verify(loginData.password, existingUser.passwordHash))
            {
                return Unauthorized();
            }

            existingUser.passwordHash = null;
            return Ok(existingUser);
        }

        [HttpPost("verify/{id}")]
        public async Task<IActionResult> VerifyUserAccount(string id, VerifyAccountData data)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Invalid user ID.");
            }

            var userAccount = await _context.UserAccounts.FindAsync(id);
            if (userAccount == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(data.password) || !BCrypt.Net.BCrypt.Verify(data.password, userAccount.passwordHash))
            {
                return Unauthorized();
            }

            return NoContent();
        }

    }
}
