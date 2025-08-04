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

        // PUT: api/Roles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserRole(string id, UserRole userRole)
        {
            if (id != userRole.Id)
            {
                return BadRequest();
            }

            _context.Entry(userRole).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
                if (RoleExists(userRole.Id, userRole.Name))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetRole", new { id = userRole.Id }, userRole);
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
                if (PermissionExists(rolePermission.Id, rolePermission.Name))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
            return CreatedAtAction("GetRole", new { id = rolePermission.Id }, rolePermission);
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

        private bool RoleExists(string id, string? name = null)
        {
            if (name != null)
            {
                name = name.ToLower();
            }

            return _context.UserRoles.Any(e => e.Id == id || (name != null && e.Name == name));
        }

        private bool PermissionExists(string id, string? name = null)
        {
            return _context.RolePermissions.Any(e => e.Id == id || (name != null && e.Name == name));
        }
    }
}
