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
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Newtonsoft.Json;

namespace MainProject.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;

        public static ApplicationDbContext dbContext =
            new(new DbContextOptions<ApplicationDbContext>());

        private static Account account = new Account(
          "dpkg7ugzn",
          "819773332465418",
          "svtDnFf_j5JlZBeDsU7iWhBYRjU");

        public static Cloudinary cloudinary = new(account);

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            
            _userManager = userManager;
            _signInManager = signInManager;
            CheckUser();
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
                        return true;
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
            if (TempData["Collections"] == null)
            {
                ViewData["Collections"] = dbContext.Users.First(x => x.Id == userId)
                    .Collections;
            }
            else
            {
                ViewData["Collections"] = JsonConvert.DeserializeObject<List<UserCollections>>(TempData["Collections"] as string);
            }

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
            if (TempData["Items"] == null)
            {
                ViewData["Items"] = dbContext.Users.First(x => x.Id == collections.UserId)
                    .Collections.First(x => x.Id == collections.Id).Items;
            }
            else
            {
                ViewData["Items"] = JsonConvert.DeserializeObject<List<UserItem>>(TempData["Items"] as string);
            }
            return View("UserCollection");
        }

        public IActionResult OpenItem(UserItem item)
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["SelectedUser"] = item.UserId;
            ViewData["SelectedCollection"] = item.UserCollectionsId;
            ViewData["SelectedItem"] = dbContext.Users.First(x => x.Id == item.UserId)
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
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    Task.WaitAll(_signInManager.SignInAsync(user, false));
                    
                    //AddLogin(user);
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
            IFormFile image, List<string> nameOfIntField,
            List<string> nameOfStringField, List<string> nameOfDateField)
        {
            nameOfIntField = nameOfIntField.Where(x => x != null).ToList();
            nameOfStringField = nameOfStringField.Where(x => x != null).ToList();
            nameOfDateField = nameOfDateField.Where(x => x != null).ToList();
            if (!dbContext.Users.First(x => x.Id == userCollections.UserId)
                .Collections.Any(y => y.Name == userCollections.Name))
            {
                if (image != null)
                {
                    var guid = Guid.NewGuid().ToString();
                    userCollections.Image = guid;
                    var upload = new ImageUploadParams();
                    upload.File = new FileDescription("image", image.OpenReadStream());
                    upload.PublicId = guid;
                    cloudinary.Upload(upload);
                }
                for (int i = 0; i < nameOfIntField.Count; i++)
                {
                    userCollections.ExtraFields.Add(new ExtraField
                    { Name = nameOfIntField[i], Type = "int" });
                }
                for (int i = 0; i < nameOfStringField.Count; i++)
                {
                    userCollections.ExtraFields.Add(new ExtraField
                    { Name = nameOfStringField[i], Type = "string" });
                }
                for (int i = 0; i < nameOfDateField.Count; i++)
                {
                    userCollections.ExtraFields.Add(new ExtraField
                    { Name = nameOfDateField[i], Type = "date" });
                }
                dbContext.Users.First(x => x.Id == userCollections.UserId)
                    .Collections.Add(userCollections);
                dbContext.SaveChanges();
            }

            return RedirectToAction("OpenUser", routeValues: new { userId = userCollections.UserId });
        }


        public IActionResult AddItem(UserItem userItem, IFormFile image,
            List<string> tags, List<string> extraFieldId, List<string> value)
        {
            tags = tags.Where(x => x != null).ToList();
            if (image != null)
            {
                var guid = Guid.NewGuid().ToString();
                userItem.Image = guid;
                var upload = new ImageUploadParams();
                upload.File = new FileDescription("image", image.OpenReadStream());
                upload.PublicId = guid;
                cloudinary.Upload(upload);
            }

            foreach (var item in tags)
            {
                userItem.Tags.Add(new Tag { Name = item, UserItemId = userItem.Id });
            }

            for (int i = 0; i < extraFieldId.Count; i++)
            {
                userItem.ExtraFieldValues.Add(
                    new ExtraFieldValue
                    {
                        ExtraFieldName = extraFieldId[i],
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
                var guid = Guid.NewGuid().ToString();
                collection.Image = guid;
                var upload = new ImageUploadParams
                {
                    File = new FileDescription("image", photo.OpenReadStream()),
                    PublicId = guid
                };
                cloudinary.Upload(upload);
            }

            dbContext.SaveChanges();

            return RedirectToAction("OpenUser", routeValues: new { userId = collection.UserId });
        }

        public IActionResult EditItem(UserItem item, List<string> extra,
            List<string> extraId, List<string> tag, IFormFile photo)
        {
            var item1 = GetItemById(item);
            item1.Name = item.Name;
            item1.Description = item.Description;
            for (int i = 0; i < extraId.Count; i++)
            {
                item1.ExtraFieldValues.First(x => x.Id == extraId[i]).Value = extra[i];
            }

            for (int i = 0; i < tag.Count; i++)
            {
                item1.Tags[i].Name = tag[i];
            }

            if (photo != null)
            {
                var guid = Guid.NewGuid().ToString();
                item1.Image = guid;
                var upload = new ImageUploadParams
                {
                    File = new FileDescription("image", photo.OpenReadStream()),
                    PublicId = guid
                };
                cloudinary.Upload(upload);
            }

            dbContext.SaveChanges();

            return RedirectToAction("OpenCollection",
                new UserCollections
                { UserId = item.UserId, Id = item.UserCollectionsId });
        }

        public IActionResult CancelChanges(UserItem item)
        {
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
            return RedirectToAction("OpenItem", new UserItem
            {
                Id = message.UserItemId,
                UserCollectionsId = userCollectionId,
                UserId = userId
            });
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

            return RedirectToAction("OpenItem", new UserItem
            {
                Id = itemId,
                UserCollectionsId = userCollectionId,
                UserId = userId
            });
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
                    if (y.Name.ToUpperInvariant().Contains(prefix.ToUpperInvariant()))
                        vs.Add(y.Name);
                    if (y.Topic.ToUpperInvariant().Contains(prefix.ToUpperInvariant()))
                        vs.Add(y.Topic);
                    y.Items.ForEach(z =>
                    {
                        if (z.Name.ToUpperInvariant().Contains(prefix.ToUpperInvariant()))
                            vs.Add(z.Name);
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

        [HttpPost]
        public JsonResult TagAutoComplete(string prefix)
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

        public ActionResult Download(string userId, string collectionId)
        {
            var coll = GetCollectionById(new UserCollections
            { Id = collectionId, UserId = userId });
            string text = System.Text.Json.JsonSerializer.Serialize(coll, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder
                    .Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            });

            System.IO.File.WriteAllText
                (Environment.CurrentDirectory + @"\wwwroot\Collection.json", text);
            var bytes = System.IO.File.ReadAllBytes(Environment.CurrentDirectory + @"\wwwroot\Collection.json");

            return File(bytes, "text/plain", $"{coll.Name}.json");
        }

        public IActionResult Google()
        {
            var prop = _signInManager
                .ConfigureExternalAuthenticationProperties("Google", "/Account/GoogleSuccess");

            return Challenge(prop, "Google");
        }

        public IActionResult GoogleSuccess()
        {
            var info = _signInManager.GetExternalLoginInfoAsync();
            info.Wait();

            if (info.IsCompletedSuccessfully)
            {
                var email = info.Result.Principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email).Value;

                var curUser = _userManager.Users.FirstOrDefault(x => x.Email == email);

                if (curUser == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var add = _userManager.AddLoginAsync(curUser, info.Result);
                add.Wait();
            }
            

            var result = _signInManager.ExternalLoginSignInAsync(
                info.Result.LoginProvider, info.Result.ProviderKey, false);
            result.Wait();

            return RedirectToAction("Login");
        }

        public IActionResult FaceBook()
        {
            var prop = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", "/Account/FaceBookSuccess");

            return Challenge(prop, "Facebook");
        }

        public IActionResult FaceBookSuccess()
        {
            var info = _signInManager.GetExternalLoginInfoAsync();
            info.Wait();

            if (info.IsCompletedSuccessfully)
            {
                var email = info.Result.Principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email).Value;

                var curUser = _userManager.Users.FirstOrDefault(x => x.Email == email);

                if (curUser == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var add = _userManager.AddLoginAsync(curUser, info.Result);
                add.Wait();
            }

            var result = _signInManager.ExternalLoginSignInAsync(
                info.Result.LoginProvider, info.Result.ProviderKey, false);
            result.Wait();

            return RedirectToAction("Login");
        }

        public IActionResult FilterCollection(string filterBy, string text,
           string userId)
        {
            List<UserCollections> cols = dbContext.Users.First(x => x.Id == userId).Collections;
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
            TempData["Collections"] = JsonConvert.SerializeObject(result);
            return RedirectToAction("OpenUser", routeValues: new { userId = userId });
        }

        public IActionResult SortCollection(string userId)
        {
            List<UserCollections> Collections = dbContext.Users.First(x => x.Id == userId)
                .Collections;
            Collections = Collections.OrderBy(x => x.Name).ToList();
            TempData["Collections"] = JsonConvert.SerializeObject(Collections);
            return RedirectToAction("OpenUser", routeValues: new { userId = userId});
        }

        public IActionResult FilterItem(string filterBy, string text,
            string userId, string collectionId)
        {
            List<UserItem> cols = dbContext.Users.First(x => x.Id == userId)
                .Collections.First(x => x.Id == collectionId).Items;
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

            TempData["Items"] = JsonConvert.SerializeObject(result);
            return RedirectToAction("OpenCollection", new UserCollections
            {
                Id = collectionId,
                UserId = userId
            });
        }

        public IActionResult SortItem(string userId, string collectionId)
        {
            List<UserItem> Items = dbContext.Users.First(x => x.Id == userId)
                .Collections.First(x => x.Id == collectionId).Items;

            Items = Items.OrderBy(x => x.Name).ToList();

            TempData["Items"] = JsonConvert.SerializeObject(Items);

            return RedirectToAction("OpenCollection", new UserCollections
            {
                Id = collectionId,
                UserId = userId
            });
        }
    }
}
