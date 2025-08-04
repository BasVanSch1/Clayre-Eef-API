using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEApi.Data;
using CEApi.Models;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using Microsoft.AspNetCore.JsonPatch;

namespace CEApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly MsSqlDatabaseContext _context;

        public RolesController(MsSqlDatabaseContext context)
        {
            _context = context;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRole>>> GetRoles()
        {
            return await _context.UserRoles
                .Include(r => r.Permissions)
                .ToListAsync();
        }

        // GET: api/Roles/{search}
        [HttpGet("{search}")]
        public async Task<ActionResult<UserRole>> GetRole(string search)
        {
            search = search.Trim().ToLower();

            var userRole = await _context.UserRoles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == search || r.Name.ToLower() == search);

            if (userRole == null)
            {
                return NotFound();
            }

            return userRole;
        }

        // PATCH: api/Roles/{search}
        [HttpPatch("{search}")]
        public async Task<ActionResult<UserRole>> PatchRole(string search, [FromBody] JsonPatchDocument<UserRole> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest("Invalid patch document.");
            }

            search = search.Trim().ToLower();
            var userRole = await _context.UserRoles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == search || r.Name.ToLower() == search);

            if (userRole == null)
            {
                return NotFound();
            }

            var currentPermissions = userRole.Permissions ?? [];

            patchDoc.ApplyTo(userRole, ModelState);

            if (patchDoc.Operations.Any(op => op.path.Equals("/Id", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { code = 400, message = "Role ID cannot be changed." });
            }

            if (patchDoc.Operations.Any(op => op.path.Equals("/Permissions", StringComparison.OrdinalIgnoreCase))) {
                IList<string> invalidPermissions = [];
                IList<RolePermission> validPermissions = currentPermissions;

                foreach (var op in patchDoc.Operations)
                {
                    if (op.op.Equals("add", StringComparison.OrdinalIgnoreCase) && op.path.Equals("/Permissions", StringComparison.OrdinalIgnoreCase))
                    {
                        (validPermissions, invalidPermissions) = await AddPermissionsToList(currentPermissions, userRole.Permissions ?? []);
                    } 
                    else if (op.op.Equals("remove", StringComparison.OrdinalIgnoreCase) && op.path.Equals("/Permissions", StringComparison.OrdinalIgnoreCase))
                    {
                        IList<RolePermission> permissionsToRemove = [];

                        // convert the permissions to remove from the patch document to RolePermission objects
                        foreach (var obj in op.value as IEnumerable<object> ?? [])
                        {
                            if (obj is Newtonsoft.Json.Linq.JObject jObj)
                            {
                                var permName = jObj["name"]?.ToString();
                                if (!string.IsNullOrEmpty(permName))
                                {
                                    permissionsToRemove.Add(new RolePermission { Name = permName });
                                }
                            }
                        }

                        (validPermissions, invalidPermissions) = RemovePermissionsFromList(validPermissions, permissionsToRemove);
                    } 
                    else if (op.op.Equals("replace", StringComparison.OrdinalIgnoreCase) && op.path.Equals("/Permissions", StringComparison.OrdinalIgnoreCase))
                    {
                        (validPermissions, invalidPermissions) = await AddPermissionsToList([], userRole.Permissions ?? []);
                    }
                }

                if (invalidPermissions.Count > 0)
                {
                    return BadRequest(new { code = 400, message = "Invalid permissions", details = invalidPermissions });
                }

                userRole.Permissions = validPermissions;
            }

            if (!TryValidateModel(userRole))
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(userRole.Id!) || RoleExists(userRole.Name))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return CreatedAtAction("GetRole", new { search = userRole.Id }, userRole);
        }

        // POST: api/Roles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserRole>> PostRole(UserRole userRole)
        {
            if (userRole.Id != null || userRole.Name == null)
            {
                return BadRequest("Invalid role data.");
            }

            if (userRole.Description.IsNullOrEmpty())
            {
                userRole.Description = userRole.Name;
            }

            if (userRole.Permissions != null)
            {
                IList<string> invalidPermissions = [];
                IList<RolePermission> permissions = [];

                foreach (var permission in userRole.Permissions)
                {
                    if (permission.Name == null)
                    {
                        return BadRequest("Invalid permission data in role.");
                    }

                    permission.Name = permission.Name.ToLower();
                    var existingPermission = await _context.RolePermissions
                        .FirstOrDefaultAsync(p => p.Name == permission.Name);

                    if (existingPermission != null)
                    {
                        if (!permissions.Any(p => p.Name == existingPermission.Name))
                        {
                            permissions.Add(existingPermission);
                        }
                        else
                        {
                            invalidPermissions.Add($"Duplicate permission '{permission.Name}' in role.");
                        }
                    } 
                    else
                    {
                        invalidPermissions.Add($"Permission '{permission.Name}' does not exist.");
                    }
                }

                if (invalidPermissions.Count > 0)
                {
                    return BadRequest(new { message = "Invalid permissions in role.", details = invalidPermissions });
                }

                userRole.Permissions = permissions;
            }

            userRole.Id = Guid.NewGuid().ToString();

            _context.UserRoles.Add(userRole);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (RoleExists(userRole.Id) || RoleExists(userRole.Name))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetRole", new { search = userRole.Id }, userRole);
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var userRole = await _context.UserRoles.FindAsync(id);
            if (userRole == null)
            {
                return NotFound();
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("Permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            var permissions = await _context.RolePermissions.ToListAsync();
            return Ok(permissions);
        }

        [HttpGet("Permissions/{search}")]
        public async Task<IActionResult> GetPermission(string search)
        {
            search = search.Trim().ToLower();
            var rolePermission = await _context.RolePermissions.FirstOrDefaultAsync(perm => perm.Id == search || perm.Name.ToLower() == search);
            if (rolePermission == null)
            {
                return NotFound();
            }

            return Ok(rolePermission);
        }

        // POST: api/Roles/Permissions
        [HttpPost("Permissions")]
        public async Task<IActionResult> PostPermission(RolePermission rolePermission)
        {
            if (rolePermission.Description == null)
            {
                rolePermission.Description = rolePermission.Name;
            }

            rolePermission.Name = rolePermission.Name.ToLower();
            rolePermission.Id = Guid.NewGuid().ToString();

            _context.RolePermissions.Add(rolePermission);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (PermissionExists(rolePermission.Name) || PermissionExists(rolePermission.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
            return CreatedAtAction("GetRole", new { search = rolePermission.Id }, rolePermission);
        }

        // DELETE: api/Roles/Permissions/5
        [HttpDelete("Permissions/{id}")]
        public async Task<IActionResult> DeletePermission(string id)
        {
            var rolePermission = await _context.RolePermissions.FindAsync(id);
            if (rolePermission == null)
            {
                return NotFound();
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<(IList<RolePermission>, IList<string>)> AddPermissionsToList(IList<RolePermission> currentPermissions, IList<RolePermission> newPermissions)
        {
            IList<string> invalidPermissions = [];

            foreach (var perm in newPermissions)
            {
                if (!currentPermissions.Any(r => r.Name.Equals(perm.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    // Check if the permission exists in the database
                    var existingPermission = await _context.RolePermissions.FirstOrDefaultAsync(r => r.Name.ToLower() == perm.Name.ToLower());
                    if (existingPermission != null)
                    {
                        currentPermissions.Add(existingPermission);
                    }
                    else
                    {
                        invalidPermissions.Add($"Permission with the name {perm.Name} does not exist.");
                    }
                }
            }

            return (currentPermissions, invalidPermissions);
        }

        private (IList<RolePermission>, IList<string>) RemovePermissionsFromList(IList<RolePermission> currentPermissions, IList<RolePermission> permissionsToRemove)
        {
            IList<string> invalidPermissions = [];

            foreach (var perm in permissionsToRemove)
            {
                var existingPermission = currentPermissions.FirstOrDefault(r => r.Name.Equals(perm.Name, StringComparison.OrdinalIgnoreCase));
                if (existingPermission != null)
                {
                    currentPermissions.Remove(existingPermission);
                }
                else
                {
                    invalidPermissions.Add($"The role does not have any permissions with the name {perm.Name} ");
                }
            }

            return (currentPermissions, invalidPermissions);
        }

        private bool RoleExists(string search)
        {
            search = search.Trim().ToLower();

            return _context.UserRoles.Any(e => e.Id == search || e.Name.ToLower() == search);
        }

        private bool PermissionExists(string search)
        {
            search = search.Trim().ToLower();

            return _context.RolePermissions.Any(e => e.Id == search || e.Name.ToLower() == search);
        }
    }
}
