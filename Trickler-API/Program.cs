using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Threading.RateLimiting;
using Trickler_API.Data;
using Trickler_API.Data.Seeders;
using Trickler_API.Middleware;
using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API
{
    public class Program
    {
        private const int MaxRequestBodySizeBytes = 10 * 1024 * 1024; // 10 MB

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var isDevelopment = builder.Environment.IsDevelopment();

            builder.Services.AddDbContext<TricklerDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 12;
            })
            .AddEntityFrameworkStores<TricklerDbContext>()
            .AddDefaultTokenProviders();

            // var jwtKey = builder.Configuration["Jwt:Key"]; // may add jwt later ? 
            // var key = Encoding.ASCII.GetBytes(jwtKey!);

            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;

                // return status codes instead of redirecting to non-existent MVC pages
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogout = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                    return Task.CompletedTask;
                };
            });

            // Only add OIDC if properly configured (not using placeholder values) because it is a wip for now
            var oidcAuthority = builder.Configuration["OpenIDConnectSettings:Authority"];

            if (!string.IsNullOrEmpty(oidcAuthority) && !oidcAuthority.Contains("your-oidc-provider"))
            {
                authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    var oidcConfig = builder.Configuration.GetSection("OpenIDConnectSettings");

                    options.Authority = oidcConfig["Authority"];
                    options.ClientId = oidcConfig["ClientId"];
                    options.ClientSecret = oidcConfig["ClientSecret"];

                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.MapInboundClaims = false;
                    options.TokenValidationParameters.NameClaimType = "name";
                    options.TokenValidationParameters.RoleClaimType = "roles";

                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                });
            }
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", limiterOptions =>
                {
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 10;
                });
                options.AddSlidingWindowLimiter("sliding", limiterOptions =>
                {
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.PermitLimit = 50;
                    limiterOptions.SegmentsPerWindow = 6;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 5;
                });
                options.AddTokenBucketLimiter("token", limiterOptions =>
                {
                    limiterOptions.TokenLimit = 20;
                    limiterOptions.TokensPerPeriod = 5;
                    limiterOptions.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 3;
                });
                options.AddConcurrencyLimiter("concurrency", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 10;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 5;
                });
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // janky work around so we have a separate rate limiter for logout
                    // TODO: fix this properly
                    var requestPath = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                    if (requestPath == "/api/v1/account/logout")
                    {
                        return RateLimitPartition.GetTokenBucketLimiter(
                            partitionKey: "logout",
                            factory: partition => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 20,
                                TokensPerPeriod = 10,
                                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", policy =>
                {
                    policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = MaxRequestBodySizeBytes;
            });
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = MaxRequestBodySizeBytes;
            });

            // Register services
            builder.Services.AddScoped<TricklerService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<UserDetailsService>();
            builder.Services.AddScoped<LeaderboardService>();
            builder.Services.AddScoped<UserTricklesService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<AnswersService>();
            builder.Services.AddSingleton<AvailabilityService>();
            builder.Services.AddSingleton<ScoringService>();

            builder.Services.AddSingleton(TimeProvider.System);

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            var app = builder.Build();

            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.EnablePersistAuthorization();
                    c.EnableTryItOutByDefault();
                });
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("DefaultPolicy");

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            await UserRolesSeeder.Initialize(app.Services);

            if (app.Environment.IsDevelopment())
            {
                await TrickleSeeder.Initialize(app.Services);
            }

            app.Run();
        }
    }
}
