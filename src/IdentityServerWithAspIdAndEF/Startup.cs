using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityServerWithAspNetIdentity.Data;
using IdentityServerWithAspNetIdentity.Models;
using IdentityServerWithAspNetIdentity.Services;
using System.Reflection;
using System.Security.Claims;

namespace IdentityServerWithAspNetIdentity
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

            string migrationsAssemblyName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssemblyName)));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();


            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            // configure identity server with in-memory stores, keys, clients and scopes
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>()
                .AddProfileService<ProfileService>()
                // this adds the config data from DB (clients, resources)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30;
                });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            CreateRoles(serviceProvider).Wait();
        }

        private async Task CreateRoles(IServiceProvider serviceProvider)
        {
            //adding custom roles
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            

            string[] roleNames = { "Administrator", "Internal", "Customer" };

            foreach (var roleName in roleNames)
            {
                //creating the roles and seeding them to the database
                var roleExist = await roleManager.RoleExistsAsync(roleName);

                if (roleExist)
                    await roleManager.DeleteAsync(await roleManager.FindByNameAsync(roleName));

                var newRole = new IdentityRole(roleName);
                await roleManager.CreateAsync(newRole);
            }


            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = Configuration.GetSection("SuperUserSettings")["UserEmail"],
                Email = Configuration.GetSection("SuperUserSettings")["UserEmail"]
            };

            string UserPassword = Configuration.GetSection("SuperUserSettings")["UserPassword"];

            var userEmail = Configuration.GetSection("SuperUserSettings")["UserEmail"];

            ApplicationUser _user = new ApplicationUser();
            _user = await userManager.FindByEmailAsync("superuser@mail.com");

            if (_user != null)
                await userManager.DeleteAsync(await userManager.FindByEmailAsync(Configuration.GetSection("SuperUserSettings")["UserEmail"]));

            var createPowerUser = await userManager.CreateAsync(user, UserPassword);
            if (createPowerUser.Succeeded)
            {

                await userManager.AddClaimAsync(user, new Claim("adminpermission", "Create"));
                await userManager.AddClaimAsync(user, new Claim("adminpermission", "Read"));
                await userManager.AddClaimAsync(user, new Claim("adminpermission", "Update"));
                await userManager.AddClaimAsync(user, new Claim("adminpermission", "Delete"));


                await userManager.AddToRoleAsync(user, "Administrator");
            }



            user = new ApplicationUser
            {
                UserName = Configuration.GetSection("InternalUserSettings")["UserEmail"],
                Email = Configuration.GetSection("InternalUserSettings")["UserEmail"]
            };

            UserPassword = Configuration.GetSection("InternalUserSettings")["UserPassword"];

            userEmail = Configuration.GetSection("InternalUserSettings")["UserEmail"];

            _user = new ApplicationUser();
            _user = await userManager.FindByEmailAsync("internaluser@mail.com");

            if (_user != null)
                await userManager.DeleteAsync(await userManager.FindByEmailAsync(Configuration.GetSection("InternalUserSettings")["UserEmail"]));

            createPowerUser = await userManager.CreateAsync(user, UserPassword);
            if (createPowerUser.Succeeded)
            {

                await userManager.AddClaimAsync(user, new Claim("internalpermission", "Create"));
                await userManager.AddClaimAsync(user, new Claim("internalpermission", "Read"));
                await userManager.AddClaimAsync(user, new Claim("internalpermission", "Update"));
                await userManager.AddClaimAsync(user, new Claim("internalpermission", "Delete"));


                await userManager.AddToRoleAsync(user, "Internal");
            }





            //SeedUsers(serviceProvider);
        }

        private async void CreateUser(IServiceProvider serviceProvider, string userSettings, Claim[] claims, string[] roles)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = Configuration.GetSection(userSettings)["UserEmail"],
                Email = Configuration.GetSection(userSettings)["UserEmail"]
            };

            string UserPassword = Configuration.GetSection(userSettings)["UserPassword"];

            var userEmail = Configuration.GetSection(userSettings)["UserEmail"];

            ApplicationUser _user = new ApplicationUser();
            _user = await userManager.FindByEmailAsync(userEmail);

            if (_user != null)
                await userManager.DeleteAsync(await userManager.FindByEmailAsync(Configuration.GetSection(userSettings)["UserEmail"]));

            var createPowerUser = await userManager.CreateAsync(user, UserPassword);
            if (createPowerUser.Succeeded)
            {
                foreach (Claim c in claims)
                    await userManager.AddClaimAsync(user, c);

                foreach (string r in roles)
                    await userManager.AddToRoleAsync(user, r);
            }
        }

        private void SeedUsers(IServiceProvider serviceProvider)
        {
            //CreateUser
            //(
            //    serviceProvider,
            //    "SuperUserSettings",
            //    new Claim[]
            //    {
            //        new Claim("adminpermission", "Create"),
            //        new Claim("adminpermission", "Read"),
            //        new Claim("adminpermission", "Update"),
            //        new Claim("adminpermission", "Delete"),
            //    },
            //    new string[]
            //    {
            //        "Administrator"
            //    }
            //);

            CreateUser
            (
                serviceProvider,
                "InternalUserSettings",
                new Claim[]
                {
                    new Claim("internalpermission", "Create"),
                    new Claim("internalpermission", "Read"),
                    new Claim("internalpermission", "Update"),
                    new Claim("internalpermission", "Delete"),
                },
                new string[]
                {
                    "Internal"
                }
            );

            CreateUser
            (
                serviceProvider,
                "CustomerUserSettings",
                new Claim[]
                {
                    new Claim("customerpermission", "Create"),
                    new Claim("customerpermission", "Read"),
                    new Claim("customerpermission", "Update"),
                    new Claim("customerpermission", "Delete"),
                },
                new string[]
                {
                    "Customer"
                }
            );
        }
    }
}