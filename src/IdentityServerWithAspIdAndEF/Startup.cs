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
using Microsoft.IdentityModel.Tokens;
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();

            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            
            // configure identity server with in-memory stores, keys, clients and scopes
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>()
                // this adds the config data from DB (clients, resources)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30;
                });

            services.AddAuthentication()
                .AddGoogle("Google", options =>
                {
                    options.ClientId = "434483408261-55tc8n0cs4ff1fe21ea8df2o443v2iuc.apps.googleusercontent.com";
                    options.ClientSecret = "3gcoTrEDPPJ0ukn_aYYT6PWo";
                })
                .AddOpenIdConnect("oidc", "OpenID Connect", options =>
                {
                    options.Authority = "https://demo.identityserver.io/";
                    options.ClientId = "implicit";
                    options.SaveTokens = true;

                 // options.GetClaimsFromUserInfoEndpoint = true; // NEW CHANGE
                 // options.ResponseType = "code id_token token";  // NEW CHANGE

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
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

            // app.UseAuthentication(); // not needed, since UseIdentityServer adds the authentication middleware
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
            var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Administrator", "Internal", "Customer" };

            foreach (var roleName in roleNames)
            {
                //creating the roles and seeding them to the database
                var roleExist = await RoleManager.RoleExistsAsync(roleName);

                if (roleExist)
                    await RoleManager.DeleteAsync(await RoleManager.FindByNameAsync(roleName));

                var newRole = new IdentityRole(roleName);
                await RoleManager.CreateAsync(newRole);

                if (roleName == "Administrator")
                    await RoleManager.AddClaimAsync(newRole, new Claim("AdminPermission", "Read"));
            }

            //creating a super user who could maintain the web app
            var poweruser = new ApplicationUser
            {
                UserName = Configuration.GetSection("UserSettings")["UserEmail"],
                Email = Configuration.GetSection("UserSettings")["UserEmail"]
            };

            string UserPassword = Configuration.GetSection("UserSettings")["UserPassword"];

            var userEmail = Configuration.GetSection("UserSettings")["UserEmail"];

            var _user = await UserManager.FindByEmailAsync(userEmail);

            if (_user != null)
                await UserManager.DeleteAsync(await UserManager.FindByEmailAsync(Configuration.GetSection("UserSettings")["UserEmail"]));

            var createPowerUser = await UserManager.CreateAsync(poweruser, UserPassword);
            if (createPowerUser.Succeeded)
            {
                //here we tie the new user to the "Admin" role 
                await UserManager.AddToRoleAsync(poweruser, "Administrator");
                await UserManager.AddClaimAsync(poweruser, new Claim("AdminPermission", "Create"));
                await UserManager.AddClaimAsync(poweruser, new Claim("AdminPermission", "Update"));
                await UserManager.AddClaimAsync(poweruser, new Claim("AdminPermission", "Delete"));
            }
        }
    }
}
