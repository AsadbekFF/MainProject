using MainProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MainProject.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace MainProject.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;

        public static ApplicationDbContext dbContext =
            new(new DbContextOptions<ApplicationDbContext>());

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        private bool CheckUser()
        {
            if (User != null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (_userManager.GetUserAsync(User).Result.Blocked)
                    {
                        _ = _signInManager.SignOutAsync();
                        return false;
                    }
                }
            }

            return false;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            return OpenUser(user.Id);
        }

        public IActionResult OpenUser(string userId)
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["Collections"] = dbContext.Users.First(x => x.Id == userId)
                .Collections;
            ViewData["SelectedUser"] = userId;
            return View("Index");
        }

        public IActionResult OpenCollection(UserCollections collections)
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["SelectedUser"] = collections.UserId;
            ViewData["SelectedCollection"] = collections.Id;
            ViewData["Items"] = dbContext.Users.First(x => x.Id == collections.UserId)
                .Collections.First(x => x.Id == collections.Id).Items;
            Download(collections.UserId, collections.Id);
            return View("UserCollection");
        }

        public IActionResult OpenItem(UserItem item, string userId)
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["SelectedUser"] = userId;
            ViewData["SelectedCollection"] = item.UserCollectionsId;
            ViewData["SelectedItem"] = dbContext.Users.First(x => x.Id == userId)
                .Collections.First(x => x.Id == item.UserCollectionsId)
                .Items.First(x => x.Id == item.Id);
            return View("UserItem");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            if (_signInManager.IsSignedIn(User))
            {
                var user = _userManager.GetUserAsync(User);
                if (user.Result.Blocked)
                {
                    _signInManager.SignOutAsync();
                    return RedirectToAction("Index", "Home");
                }
            }
            return View(new LoginViewModel { });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var users = _userManager.Users.ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    User user = users.First(x => x.Email == model.Email);
                    user.LastLogin = DateTime.Now;
                    _ = _userManager.UpdateAsync(user);

                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Неправильный логин и (или) пароль");
                }
            }
            ViewData["ValidateAccount"] = "Not validated";
            ViewData["Users"] = users;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = new()
                {
                    Email = model.Email,
                    UserName = model.Email,
                    Name = model.Name,
                    Password = model.Password,
                    Blocked = false,
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    Task.WaitAll(_signInManager.SignInAsync(user, false));

                    ViewData["Users"] = _userManager.Users.ToList();
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
            return View(model);
        }

        public IActionResult Register()
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            if (_signInManager.IsSignedIn(User))
            {
                var user = _userManager.GetUserAsync(User);
                if (user.Result.Blocked)
                {
                    _signInManager.SignOutAsync();
                    RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        public IActionResult AddCollection(UserCollections userCollections,
            IFormFile image, List<string> type, List<string> nameOfField)
        {
            if (!dbContext.Users.First(x => x.Id == userCollections.UserId)
                .Collections.Any(y => y.Name == userCollections.Name))
            {
                if (image != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        image.CopyTo(ms);
                        userCollections.Image = ms.ToArray();
                    }
                }
                for (int i = 0; i < type.Count; i++)
                {
                    userCollections.ExtraFields.Add(new ExtraField
                    { Name = nameOfField[i], Type = type[i] });
                }
                dbContext.Users.First(x => x.Id == userCollections.UserId)
                    .Collections.Add(userCollections);
                dbContext.SaveChanges();
            }

            return RedirectToAction("OpenUser", routeValues: new { userId = userCollections.UserId });
        }


        public IActionResult AddItem(UserItem userItem, IFormFile image,
            string tags, List<string> extraFieldId, List<string> value)
        {
            if (image != null)
            {
                using (var ms = new MemoryStream())
                {
                    image.CopyTo(ms);
                    userItem.Image = ms.ToArray();
                }
            }

            foreach (var item in tags.Split(" "))
            {
                userItem.Tags.Add(new Tag { Name = item, UserItemId = userItem.Id });
            }

            for (int i = 0; i < extraFieldId.Count; i++)
            {
                userItem.ExtraFieldValues.Add(
                    new ExtraFieldValue
                    {
                        ExtraFieldId = extraFieldId[i],
                        Value = value[i]
                    });
            }

            dbContext.Users.First(x => x.Id == userItem.UserId)
                .Collections.First(x => x.Id == userItem.UserCollectionsId)
                .Items.Add(userItem);
            dbContext.SaveChanges();

            return RedirectToAction("OpenCollection",
                new UserCollections
                { UserId = userItem.UserId, Id = userItem.UserCollectionsId });
        }


        [HttpPost]
        public IActionResult OpenEditCollection(UserCollections collections)
        {
            return View("EditCollection", dbContext
                .Users.First(x => x.Id == collections.UserId)
                .Collections.First(x => x.Id == collections.Id));
        }


        [HttpPost]
        public IActionResult OpenEditItem(UserItem item, string userId)
        {
            return View("EditItem", dbContext
                .Users.First(x => x.Id == userId)
                .Collections.First(x => x.Id == item.UserCollectionsId)
                .Items.First(x => x.Id == item.Id));
        }


        public IActionResult EditCollection(UserCollections collections, IFormFile photo)
        {
            var collection = GetCollectionById(collections);

            collection.Name = collections.Name;

            collection.Description = collections.Description;

            if (photo != null)
            {
                using (var ms = new MemoryStream())
                {
                    photo.CopyTo(ms);
                    collections.Image = ms.ToArray();
                }
                collection.Image = collections.Image;
            }

            dbContext.SaveChanges();

            return RedirectToAction("OpenUser", routeValues: new { userId = collection.UserId });
        }

        public IActionResult EditItem(UserItem item, string extra,
            string extraId, List<string> tag, IFormFile photo)
        {
            var item1 = GetItemById(item);
            item1.Name = item.Name;
            item1.Description = item.Description;
            item1.ExtraFieldValues.First(x => x.Id == extraId).Value = extra;

            for (int i = 0; i < tag.Count; i++)
            {
                item1.Tags[i].Name = tag[i];
            }

            if (photo != null)
            {
                using (var ms = new MemoryStream())
                {
                    photo.CopyTo(ms);
                    item1.Image = ms.ToArray();
                }
            }

            dbContext.SaveChanges();

            return RedirectToAction("OpenCollection",
                new UserCollections
                { UserId = item.UserId, Id = item.UserCollectionsId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        public IActionResult SendMessage(Message message,
            string userId, string userCollectionId)
        {
            dbContext.Users.First(x => x.Id == message.RecieverId)
                .Collections.First(x => x.Id == userCollectionId)
                .Items.First(x => x.Id == message.UserItemId)
                .Chat.Add(message);
            dbContext.SaveChanges();
            return OpenItem(new UserItem
            {
                Id = message.UserItemId,
                UserCollectionsId = userCollectionId
            }, userId);
        }


        public IActionResult LikeItem(string userId, string userCollectionId,
            string itemId)
        {
            var like = GetItemById(new UserItem
            {
                Id = itemId,
                UserCollectionsId = userCollectionId,
                UserId = userId
            });

            if (like.LikedUsers.Any(x => x.UserId == userId))
            {
                if (like.LikedUsers.First(x => x.UserId == userId).IsLiked)
                {
                    like.LikedUsers.First(x => x.UserId == userId)
                        .IsLiked = false;
                    like.LikeCount--;
                }
                else
                {
                    like.LikedUsers.First(x => x.UserId == userId)
                        .IsLiked = true;
                    like.LikeCount++;
                }
            }
            else
            {
                LikedUser user = new() { IsLiked = true, UserId = userId };
                like.LikedUsers.Add(new LikedUser { IsLiked = true, UserId = userId });
                like.LikeCount++;
            }

            dbContext.SaveChanges();

            return OpenItem(new UserItem()
            {
                Id = itemId,
                UserCollectionsId = userCollectionId
            }, userId);
        }

        public IActionResult Search(string CustomerName)
        {
            List<UserItem> items = new();
            List<UserCollections> collections = new();
            dbContext.Users.ForEachAsync(x =>
            {
                x.Collections.ForEach(y =>
                {
                    if (y.Name.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                        collections.Add(y);
                    if (y.Description.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                        collections.Add(y);
                    if (y.Topic.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                        collections.Add(y);
                    y.Items.ForEach(z =>
                    {
                        if (z.Name.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                            items.Add(z);
                        if (z.Description.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                            items.Add(z);
                        z.Chat.ForEach(w =>
                        {
                            if (w.Text.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                                items.Add(z);
                        });
                        z.ExtraFieldValues.ForEach(u =>
                        {
                            if (u.Value.ToUpperInvariant().Contains(CustomerName.ToUpperInvariant()))
                                items.Add(z);
                        });
                    });
                });
            });
            ViewData["Collections"] = collections;
            ViewData["Items"] = items;
            return View("SearchResult");
        }

        public IActionResult SearchByTag(string tag)
        {
            List<UserItem> items = new();

            dbContext.Users.ForEachAsync(x =>
            {
                x.Collections.ForEach(y =>
                {
                    y.Items.ForEach(z =>
                    {
                        if (z.Tags.Any(w => w.Name.ToUpperInvariant() == tag.ToUpperInvariant()))
                            items.Add(z);
                    });
                });
            });

            ViewData["Items"] = items;
            ViewData["Collections"] = new List<UserCollections>();
            return View("SearchResult");
        }


        public IActionResult DeleteCollection(UserCollections collection)
        {
            dbContext.Users.First(x => x.Id == collection.UserId)
                .Collections.Remove(GetCollectionById(collection));
            dbContext.SaveChanges();

            return OpenUser(collection.UserId);
        }


        public IActionResult DeleteItem(UserItem item)
        {
            dbContext.Users.First(x => x.Id == item.UserId)
                .Collections.First(x => x.Id == item.UserCollectionsId)
                .Items.Remove(GetItemById(item));

            dbContext.SaveChanges();

            return OpenCollection(new UserCollections
            { Id = item.UserCollectionsId, UserId = item.UserId });
        }

        public UserCollections GetCollectionById(UserCollections collections)
        {
            return dbContext.Users.First(x => x.Id == collections.UserId)
                .Collections.First(x => x.Id == collections.Id);
        }

        public UserItem GetItemById(UserItem collections)
        {
            return dbContext.Users.First(x => x.Id == collections.UserId)
                .Collections.First(x => x.Id == collections.UserCollectionsId)
                .Items.First(x => x.Id == collections.Id);
        }

        [HttpPost]
        public JsonResult AutoComplete(string prefix)
        {
            List<string> vs = new();
            dbContext.Users.ForEachAsync(x =>
            {
                x.Collections.ForEach(y =>
                {
                    y.Items.ForEach(z =>
                    {
                        z.Tags.ForEach(w =>
                        {
                            if (w.Name.ToUpperInvariant().Contains(prefix.ToUpperInvariant()))
                                vs.Add(w.Name);
                        });
                    });
                });
            }).Wait();

            return Json(vs);
        }

        public void Download(string userId, string collectionId)
        {
            var coll = GetCollectionById(new UserCollections
            { Id = collectionId, UserId = userId });
            string text = JsonSerializer.Serialize(coll, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder
                    .Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            });

            System.IO.File.WriteAllText
                (Environment.CurrentDirectory + @"\wwwroot\Collection.json", text);
        }
    }
}
