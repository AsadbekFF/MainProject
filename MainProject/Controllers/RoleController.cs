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
        RoleManager<IdentityRole> _roleManager;
        UserManager<User> _userManager;
        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public IActionResult Index() => View(_roleManager.Roles.ToList());

        public IActionResult Create() => View();
        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(name));
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(name);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            IdentityRole role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction("Index");
        }

        public IActionResult UserList() => View(_userManager.Users.ToList());

        public IActionResult Block(List<string> checkbox)
        {
            foreach (var item in checkbox)
            {
                AccountController.dbContext.Users.First(x => x.Id == item).Blocked = true;
                AccountController.dbContext.SaveChanges();
            }
            return View("UserList", _userManager.Users.ToList());
        }

        public IActionResult Unblock(List<string> checkbox)
        {
            foreach (var item in checkbox)
            {
                AccountController.dbContext.Users.First(x => x.Id == item).Blocked = false;
                AccountController.dbContext.SaveChanges();
            }
            return View("UserList", _userManager.Users.ToList());
        }

        [HttpPost]
        public IActionResult DeleteUsers(List<string> checkbox)
        {
            foreach (var item in checkbox)
            {
                AccountController.dbContext.Users.Remove(AccountController.dbContext.Users.First(x => x.Id == item));
                AccountController.dbContext.SaveChanges();
            }
            return View("UserList", _userManager.Users.ToList());
        }

        public async Task<IActionResult> Promote(List<string> checkbox)
        {
            foreach (var userId in checkbox)
            {
                var user = await _userManager.FindByIdAsync(userId);
                await _userManager.AddToRoleAsync(user, "admin");
            }

            return RedirectToAction("UserList");
        }

        public async Task<IActionResult> Downgrade(List<string> checkbox)
        {
            foreach (var userId in checkbox)
            {
                var user = await _userManager.FindByIdAsync(userId);
                await _userManager.RemoveFromRoleAsync(user, "admin");
            }

            return RedirectToAction("UserList");
        }
    }
}
