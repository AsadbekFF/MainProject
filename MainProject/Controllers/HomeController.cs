using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MainProject.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MainProject.Controllers;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace MiddlePartProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        UserManager<User> _userManager;

        SignInManager<User> _signInManager;

        public HomeController(ILogger<HomeController> logger,
            UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        private void CheckUser(out bool isLegal)
        {
            isLegal = false;
            if (User != null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (_userManager.GetUserAsync(User).Result.Blocked)
                    {
                        _ = _signInManager.SignOutAsync();
                    }
                    else
                    {
                        isLegal = true;
                    }
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            List<UserCollections> collections = new();
            List<UserItem> items = new();
            await AccountController.dbContext.Users
                .ForEachAsync(x => collections.AddRange(x.Collections));
            await AccountController.dbContext.Users
                .ForEachAsync(x =>
                {
                    x.Collections.ForEach(
                    y => items.AddRange(y.Items));
                });
            ViewData["Items"] = items.OrderBy(x => x.DateOfEntered).Take(10).ToList();
            ViewData["Collections"] = collections.OrderBy(x => x.Items.Count).Take(5).ToList();

            return View();
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> FilterCollection(string filterBy, string text,
           List<string> listOfCollections)
        {
            List<UserCollections> cols = await GetUserCollectionsById(listOfCollections);
            List<UserCollections> result = new();

            switch (filterBy)
            {
                case "Name":
                    cols.ForEach(x => {
                        if (x.Name.ToUpperInvariant()
                            .Contains(text.ToUpperInvariant())) result.Add(x);
                    });
                    break;
                case "Topic":
                    cols.ForEach(x => {
                        if (x.Topic.ToUpperInvariant()
                            .Contains(text.ToUpperInvariant())) result.Add(x);
                    });
                    break;
            }

            ViewData["Collections"] = result;
            ViewData["Items"] = new List<UserItem>();
            return View("Index");
        }

        public async Task<IActionResult> SortCollection(List<string> listOfCollections)
        {
            List<UserCollections> Collections = await GetUserCollectionsById(listOfCollections);
            Collections = Collections.OrderBy(x => x.Name).ToList();
            ViewData["Collections"] = Collections;
            ViewData["Items"] = new List<UserItem>();
            return View("Index");
        }

        public async Task<IActionResult> FilterItem(string filterBy, string text,
            List<string> items)
        {
            List<UserItem> cols = await GetUserItemsByIdAsync(items);
            List<UserItem> result = new();

            switch (filterBy)
            {
                case "Name":
                    cols
                        .ForEach(x => {
                            if (x.Name.ToUpperInvariant()
                .Contains(text.ToUpperInvariant())) result.Add(x);
                        });
                    break;
                case "Description":
                    cols
                        .ForEach(x => {
                            if (x.Description.ToUpperInvariant()
                .Contains(text.ToUpperInvariant())) result.Add(x);
                        });
                    break;
                case "Tag":
                    cols
                        .ForEach(x => {
                            if (x.Tags.Any(y => y.Name.ToUpperInvariant().Contains(text.ToUpperInvariant())))
                                result.Add(x);
                        });
                    break;
            }

            ViewData["Collections"] = new List<UserCollections>();
            ViewData["Items"] = result;
            return View("Index");
        }

        public async Task<IActionResult> SortItem(List<string> items)
        {
            List<UserItem> listOfCollections = await GetUserItemsByIdAsync(items);

            listOfCollections = listOfCollections.OrderBy(x => x.Name).ToList();
            ViewData["Collections"] = listOfCollections;
            ViewData["Items"] = new List<UserItem>();
            return View("Index");
        }

        public async Task<List<UserCollections>> GetUserCollectionsById(List<string> id)
        {
            List<UserCollections> cols = new();

            await AccountController.dbContext.Users.ForEachAsync(x =>
            {
                x.Collections.ForEach(y =>
                {
                    id.ForEach(z =>
                    {
                        if (y.Id == z)
                            cols.Add(y);
                    });
                });
            });

            return cols;
        }

        public async Task<List<UserItem>> GetUserItemsByIdAsync(List<string> id)
        {
            List<UserItem> cols = new();

            await AccountController.dbContext.Users.ForEachAsync(x =>
            {
                x.Collections.ForEach(y =>
                {
                    y.Items.ForEach(z =>
                    {
                        id.ForEach(c =>
                        {
                            if (c == z.Id)
                                cols.Add(z);
                        });
                    });
                });
            });

            return cols;
        }
    }
}
