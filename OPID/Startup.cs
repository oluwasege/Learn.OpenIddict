using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OPID.Extensions;
using System;
using System.Text;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OPID
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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OPID", Version = "v1" });
            });

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Configure the context to use Microsoft SQL Server.
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")).UseOpenIddict();

                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                //options.UseOpenIddict();
            });
            services.Configure<OpenIddictServerConfig>(Configuration.GetSection("OpenIddictServerConfig"));
            services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(option =>
            {
                option.Password.RequireDigit = false;
                option.Password.RequireUppercase = false;
                option.Password.RequireNonAlphanumeric = false;
                option.Password.RequiredLength = 4;
                option.Password.RequireLowercase = false;
                option.SignIn.RequireConfirmedEmail = false;
                option.SignIn.RequireConfirmedPhoneNumber = false;


            });
           

            services.AddOpenIddict()
        // Register the OpenIddict core components.
                    .AddCore(options =>
                    {
                        // Configure OpenIddict to use the Entity Framework Core stores and models.
                        // Note: call ReplaceDefaultEntities() to replace the default entities.
                        options.UseEntityFrameworkCore()
                               .UseDbContext<ApplicationDbContext>();
                    })

                    // Register the OpenIddict server components.
                    .AddServer(options =>
                    {
                        //options.AcceptAnonymousClients();
                        // Enable the token endpoint.
                        options.SetTokenEndpointUris("/connect/token");
                        options.SetUserinfoEndpointUris("/connect/userinfo");
                        // Enable the client credentials flow.
                        //options.AllowClientCredentialsFlow();

                        //Enable the password flow
                         options.AllowPasswordFlow();

                        // Add all auth flows you want to support
                        // Supported flows are:
                        //      - Authorization code flow
                        //      - Client credentials flow
                        //      - Device code flow
                        //      - Implicit flow
                        //      - Password flow
                        //      - Refresh token flow

                        // Register your scopes - Scopes are a list of identifiers used to specify
                        // what access privileges are requested.
                        options.RegisterScopes(Scopes.Email,
                                                    Scopes.Profile,
                                                    Scopes.Roles);

                        // Set the lifetime of your tokens
                        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
                        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

                        // Register signing and encryption details
                        options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();

                        // Register ASP.NET Core host and configuration options
                        options.UseAspNetCore().EnableTokenEndpointPassthrough();
                    })
                // Register the OpenIddict validation components.
                .AddValidation(options =>
                {
                    // Import the configuration from the local OpenIddict server instance.
                    options.UseLocalServer();
                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

                        // Register the worker responsible of seeding the database with the sample clients.
                        // Note: in a real world application, this step should be part of a setup script.
                        //services.AddHostedService<Worker>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = Schemes.Bearer;
                options.DefaultChallengeScheme = Schemes.Bearer;
            }).AddJwtBearer("Bearer", options =>
            {
                options.Authority = options.Authority = Configuration["OpenIddictServerConfig:Authority"];
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = Claims.Name,
                    RoleClaimType = Claims.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["OpenIddictServerConfig:SecretKey"])),
                    ValidateAudience = false,
                    ValidateIssuer = false,
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            context.Response.Headers.Add("Token-Expired", "true");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OPID v1"));
            }
            //var scope = app.ApplicationServices.CreateScope();
            //var context = scope.ServiceProvider.GetService<ApplicationDbContext>();

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //Create OpenID Connect client application
            using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var existingClientApp = manager.FindByClientIdAsync("default-client").GetAwaiter().GetResult();
            if (existingClientApp == null)
            {
                manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "default-client",
                    ClientSecret = "499D56FA-B47B-5199-BA61-B298D431C318",
                    //Type=OpenIddictConstants.ClientTypes.Public,
                    DisplayName = "Default client application",
                    Permissions =
                    {

                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Roles,
                        //OpenIddictConstants.Permissions.ResponseTypes.IdToken
                    }
                }).GetAwaiter().GetResult();
            }
        }
    }
}
