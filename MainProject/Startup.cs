using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MainProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using MainProject.Hub;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace MainProject
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(@"Data Source=tcp:mainprojectdbserver.database.windows.net,1433;Initial Catalog=MainProject_db;User Id=Asadbek@mainprojectdbserver;Password=adjusttowin1507$$$"));

            services.AddIdentity<User, IdentityRole>(config =>
            {
                config.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication()
                .AddGoogle(g =>
                {
                    g.ClientId = "823724115961-eeqjod1rnn9ufdcbn1ds512d7jsd3oid.apps.googleusercontent.com";
                    g.ClientSecret = "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";
                })
                .AddFacebook(f =>
                {
                    f.AppId = "417052340599214";
                    f.AppSecret = "fcff02e69fe2a545d4b05c2800729cdb";
                });

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddControllersWithViews()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("en"),
                    new CultureInfo("uz")
                };

                options.DefaultRequestCulture = new RequestCulture("uz");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            services.AddSignalR()
                .AddAzureSignalR(config => 
                {
                    config.ConnectionString = "Endpoint=https://mainprojectservice.service.signalr.net;AccessKey=Unb3Awpvj4Bq8g8l8lDzjMMjiRjn7FPwnmCSi6BGtWs=;Version=1.0;";
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseRequestLocalization();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "SigninGoogle",
                    pattern: "signin-google",
                    defaults: new { controller = "Account", action = "GoogleSuccess" });
                endpoints.MapControllerRoute(
                    name: "SigninFacebook",
                    pattern: "signin-facebook",
                    defaults: new { controller = "Account", action = "FaceBookSuccess" });
                endpoints.MapHub<ChatHub>("/chat");
            });

            CreateRoles(serviceProvider);
        }

        private void CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            Task<IdentityResult> roleResult;
            string email = "asadbek.fayziev1507@gmail.com";

            Task<bool> hasAdminRole = roleManager.RoleExistsAsync("admin");
            hasAdminRole.Wait();

            if (!hasAdminRole.Result)
            {
                roleResult = roleManager.CreateAsync(new IdentityRole("admin"));
                roleResult.Wait();
            }

            Task<User> testUser = userManager.FindByEmailAsync(email);
            testUser.Wait();

            if (testUser.Result == null)
            {
                User administrator = new();
                administrator.Email = email;
                administrator.UserName = email;
                administrator.Name = "Asadbek";
                administrator.EmailConfirmed = true;

                Task<IdentityResult> newUser = userManager.CreateAsync(administrator, "Adjusttowin15$");
                newUser.Wait();

                if (newUser.Result.Succeeded)
                {
                    Task<IdentityResult> newUserRole = userManager.AddToRoleAsync(administrator, "admin");
                    newUserRole.Wait();
                }
            }
        }
    }
}
