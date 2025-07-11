using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEApi.Data;
using CEApi.Models;

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

            return userAccount;
        }

        // PUT: api/Authentication/edit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("/edit/{id}")]
        public async Task<IActionResult> PutUserAccount(string id, UserAccount userAccount)
        {
            if (id != userAccount.userId)
            {
                return BadRequest();
            }

            _context.Entry(userAccount).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserAccountExists(id))
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

        // POST: api/Authentication/create
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("/create")]
        public async Task<ActionResult<UserAccount>> PostUserAccount(UserAccount userAccount)
        {
            _context.UserAccounts.Add(userAccount);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserAccountExists(userAccount.userId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

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

        private bool UserAccountExists(string id)
        {
            return _context.UserAccounts.Any(e => e.userId == id);
        }
    }
}
