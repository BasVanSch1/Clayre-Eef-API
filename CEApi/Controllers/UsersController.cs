using CEApi.Data;
using CEApi.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet]
        public async Task<IActionResult> GetUserAccounts()
        {
            var userAccounts = await _context.UserAccounts
                .Include(u => u.Roles)
                .IgnoreAutoIncludes()
                .ToListAsync();

            foreach (var user in userAccounts)
            {
                user.passwordHash = null; // Remove password hash before returning
            }

            return Ok(userAccounts);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAccount>> GetUserAccount(string id)
        {
            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Roles)
                .FirstOrDefaultAsync(ua => ua.userId == id);

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

            var user = await _context.UserAccounts.Include(ua => ua.Roles).FirstOrDefaultAsync(ua => ua.userId == id);

            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            var currentRoles = user.Roles ?? []; // since the Patchdoc overwrites the roles, we need to get the user's current roles first

            patchDoc.ApplyTo(user);

            if (patchDoc.Operations.Any(op => op.path.Equals("/passwordHash", StringComparison.OrdinalIgnoreCase)))
            {
                user.passwordHash = BCrypt.Net.BCrypt.HashPassword(user.passwordHash);
            }

            if (patchDoc.Operations.Any(op => op.path.Equals("/roles", StringComparison.OrdinalIgnoreCase)))
            {
                IList<string> invalidRoles = [];
                IList<UserRole> validRoles = currentRoles;

                foreach (var op in patchDoc.Operations)
                {
                    if (op.op == "add" && op.path == "/roles")
                    {
                        (validRoles, invalidRoles) = await AddRolesToList(validRoles, user.Roles ?? []);
                    }
                    else if (op.op == "remove" && op.path == "/roles")
                    {
                        IList<UserRole> rolesToRemove = [];

                        // convert the roles to remove from the patch document to UserRole objects
                        foreach (var obj in op.value as IEnumerable<object> ?? [])
                        {
                            if (obj is Newtonsoft.Json.Linq.JObject jObj)
                            {
                                var roleName = jObj["name"]?.ToString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    rolesToRemove.Add(new UserRole { Name = roleName });
                                }
                            }
                        }

                        (validRoles, invalidRoles) = RemoveRolesFromList(validRoles, rolesToRemove);
                    }
                    else if (op.op == "replace" && op.path == "/roles")
                    {
                        (validRoles, invalidRoles) = await AddRolesToList([], user.Roles ?? []);
                    }
                }

                if (invalidRoles.Count > 0)
                {
                    return BadRequest(new { code = 400, message = "Invalid roles", details = invalidRoles });
                }                

                user.Roles = validRoles;
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

        // POST: api/Users/create
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

            if (userAccount.Roles != null && userAccount.Roles.Count > 0)
            {
                IList<string> invalidRoles = [];
                IList<UserRole> validRoles = [];
                foreach (var role in userAccount.Roles)
                {
                    if (role.Name == null)
                    {
                        return BadRequest("Invalid role data in user account.");
                    }

                    var existingRole = await _context.UserRoles.FirstOrDefaultAsync(r => r.Name.ToLower() == role.Name.ToLower());
                    if (existingRole != null)
                    {
                        validRoles.Add(existingRole);
                    }
                    else
                    {
                        invalidRoles.Add($"Role with the name {role.Name} does not exist.");
                    }
                }

                if (invalidRoles.Count > 0)
                {
                    return BadRequest(new { code = 400, message = "Invalid roles", details = invalidRoles });
                }

                userAccount.Roles = validRoles;
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

        // DELETE: api/Users/delete/5
        [HttpDelete("delete/{id}")]
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

        // GET: api/Users/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetUserPermissions(string id)
        {
            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Roles!)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(ua => ua.userId == id);
            if (userAccount == null)
            {
                return NotFound();
            }

            var permissions = userAccount.Roles?
                .SelectMany(r => r.Permissions!)
                .Distinct()
                .ToList() ?? [];

            return Ok(permissions);
        }

        private async Task<(IList<UserRole>, IList<string>)> AddRolesToList(IList<UserRole> currentRoles, IList<UserRole> newRoles)
        {
            IList<string> invalidRoles = [];

            foreach (var role in newRoles)
            {
                if (!currentRoles.Any(r => r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    // Check if the role exists in the database
                    var existingRole = await _context.UserRoles.FirstOrDefaultAsync(r => r.Name.ToLower() == role.Name.ToLower());
                    if (existingRole != null)
                    { 
                        currentRoles.Add(existingRole);
                    } else
                    {
                        invalidRoles.Add($"Role with the name {role.Name} does not exist.");
                    }
                }
            }

            return ( currentRoles, invalidRoles );
        }

        private (IList<UserRole>, IList<string>) RemoveRolesFromList(IList<UserRole> currentRoles, IList<UserRole> rolesToRemove)
        {
            IList<string> invalidRoles = [];

            foreach (var role in rolesToRemove)
            {
                var existingRole = currentRoles.FirstOrDefault(r => r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase));
                if (existingRole != null)
                {
                    currentRoles.Remove(existingRole);
                } else
                {
                    invalidRoles.Add($"Role with the name {role.Name} does not exist in the current user roles.");
                }
            }

            return (currentRoles, invalidRoles);
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
