using CEApi.Data;
using CEApi.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace CEApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly MsSqlDatabaseContext _context;

        public UsersController(MsSqlDatabaseContext context)
        {
            _context = context;
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
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

        // PATCH: api/Users/edit/{id}
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
