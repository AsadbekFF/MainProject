using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MainProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainProject.Controllers;

namespace MiddlePartProject.Controllers
{
    [Authorize(Roles = "admin")]
    public class RoleController : Controller
    {
        SignInManager<User> _signinManager;
        RoleManager<IdentityRole> _roleManager;
        UserManager<User> _userManager;
        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _signinManager = signInManager;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public IActionResult Index() => View(_userManager.Users.ToList());

        public IActionResult Block(List<string> checkbox)
        {
            foreach (var item in checkbox)
            {
                AccountController.dbContext.Users.First(x => x.Id == item).Blocked = true;
                AccountController.dbContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Unblock(List<string> checkbox)
        {
            foreach (var item in checkbox)
            {
                AccountController.dbContext.Users.First(x => x.Id == item).Blocked = false;
                AccountController.dbContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteUsers(List<string> checkbox)
        {
            foreach (var item in checkbox)
            {
                AccountController.dbContext.Users.Remove(AccountController.dbContext.Users.First(x => x.Id == item));
                AccountController.dbContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Promote(List<string> checkbox)
        {
            foreach (var userId in checkbox)
            {
                var user = await _userManager.FindByIdAsync(userId);
                await _userManager.AddToRoleAsync(user, "admin");
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Downgrade(List<string> checkbox)
        {
            foreach (var userId in checkbox)
            {
                var user = await _userManager.FindByIdAsync(userId);
                await _userManager.RemoveFromRoleAsync(user, "admin");
            }

            return RedirectToAction("Index");
        }
    }
}
