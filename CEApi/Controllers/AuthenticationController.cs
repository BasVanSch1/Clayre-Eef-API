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

        // GET: api/Authentication/lookup/5
        [HttpGet("lookup/{id}")]
        public async Task<ActionResult<UserAccount>> GetUserAccount(string id)
        {
            var userAccount = await _context.UserAccounts.FindAsync(id);

            if (userAccount == null)
            {
                return NotFound();
            }

            userAccount.passwordHash = null;

            return userAccount;
        }

        [HttpPatch("edit/{id}")]
        public async Task<IActionResult> PatchUserAccount(string id, [FromBody] JsonPatchDocument<UserAccount> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest("Invalid patch document.");
            }

            var user = await _context.UserAccounts.FindAsync(id);

            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            patchDoc.ApplyTo(user);

            if (patchDoc.Operations.Any(op => op.path.Equals("/passwordHash", StringComparison.OrdinalIgnoreCase)))
            {
                user.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);
            }

            if (user.displayName == null)
            {
                user.displayName = user.userName;
            }

            if (!TryValidateModel(user))
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while updating the user account.");
            }


            user.passwordHash = null;
            return Ok(user);
        }

        // POST: api/Authentication/create
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("create")]
        public async Task<ActionResult<UserAccount>> CreateUserAccount(UserAccount userAccount)
        {
            if (userAccount == null || string.IsNullOrEmpty(userAccount.userName) || string.IsNullOrEmpty(userAccount.email) || string.IsNullOrEmpty(userAccount.passwordHash))
            {
                return BadRequest("Invalid user account data.");
            }

            if (UserAccountUsernameExists(userAccount.userName))
            {
                return Conflict(new { code = 409, message = "Username already exists" });
            }

            if (UserAccountEmailExists(userAccount.email))
            {
                return Conflict(new { code = 409, message = "Email already exists" });
            }

            userAccount.userId = Guid.NewGuid().ToString();
            userAccount.passwordHash = BCrypt.Net.BCrypt.HashPassword(userAccount.passwordHash);
            userAccount.displayName ??= userAccount.userName;

            _context.UserAccounts.Add(userAccount);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return StatusCode(500, "An error occurred while updating the user account.");
            }

            userAccount.passwordHash = null;
            return CreatedAtAction("GetUserAccount", new { id = userAccount.userId }, userAccount);
        }

        // DELETE: api/Authentication/delete/5
        [HttpDelete("/delete/{id}")]
        public async Task<IActionResult> DeleteUserAccount(string id)
        {
            var userAccount = await _context.UserAccounts.FindAsync(id);
            if (userAccount == null)
            {
                return NotFound();
            }

            _context.UserAccounts.Remove(userAccount);
            await _context.SaveChangesAsync();

            return NoContent();
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

        private bool UserAccountIdExists(string id)
        {
            return _context.UserAccounts.Any(e => e.userId == id );
        }

        private bool UserAccountUsernameExists(string username)
        {
            return _context.UserAccounts.Any(e => e.userName.ToLower() == username.ToLower());
        }

        private bool UserAccountEmailExists(string email)
        {
            return _context.UserAccounts.Any(e => e.email.ToLower() == email.ToLower());
        }
    }
}
