using ASDPRS_SEP490.Middlewares;
using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using Repository;
using Repository.BaseRepository;
using Repository.IBaseRepository;
using Service;
using Service.BackgroundJobs;
using Service.Hubs;
using Service.Interface;
using Service.IService;
using Service.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "ASDPRS API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });

});

// Configure DbContext
builder.Services.AddDbContext<ASDPRSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"),
        sqlOptions => sqlOptions.MigrationsAssembly("DataAccessLayer")));

// Keep claim mapping cleared (recommended)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Add Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ASDPRSContext>()
    .AddDefaultTokenProviders();

// Application cookie (Identity)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/swagger"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/swagger"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // TEMP DEBUG
});
// External cookie used by Identity when handling external logins
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // TEMP DEBUG
});

// Register DAO/Repository
builder.Services.AddScoped(typeof(BaseDAO<>), typeof(EfBaseDAO<>));
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// Add JSON serializer settings to handle reference loops
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

// Register Repository + Service
builder.Services.ConfigureRepositoryService(builder.Configuration);
builder.Services.AddScoped<IFileStorageService, SupabaseFileStorageService>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IGenAIService, GeminiAiService>();
builder.Services.ConfigureServiceService(builder.Configuration);
builder.Services.AddHostedService<AssignmentStatusUpdater>(); // Background job to update assignment statuses
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddSignalR();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "https://localhost:5173",
                "http://160.25.232.199",
                "https://160.25.232.199"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Authentication config
builder.Services.AddAuthentication(options =>
{
    // Use JWT as the default authentication/challenge for APIs,
    // but keep Cookie scheme available for external sign-in / browser sessions.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // used by Identity external login
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        // ensure the claim that contains user id is mapped to NameIdentifier
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };

    // allow reading token from Authorization header OR a cookie (ASDPRS_Access)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // 1) Prefer Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                return Task.CompletedTask;
            }

            // 2) Fallback: read from ASDPRS_Access cookie
            if (context.Request.Cookies.TryGetValue("ASDPRS_Access", out var tokenFromCookie))
            {
                context.Token = tokenFromCookie;
            }

            return Task.CompletedTask;
        }
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/api/account/google-login";
    options.LogoutPath = "/api/account/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.CallbackPath = "/signin-google";

    googleOptions.Scope.Add("profile");
    googleOptions.Scope.Add("email");

    googleOptions.SaveTokens = true;
    googleOptions.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
        return Task.CompletedTask;
    };
    // Map extras if needed
    googleOptions.ClaimActions.MapJsonKey("picture", "picture");
    googleOptions.ClaimActions.MapJsonKey("email_verified", "email_verified");

    // Use Identity external sign-in scheme so Identity stores external auth in Identity.External
    googleOptions.SignInScheme = IdentityConstants.ExternalScheme;

    googleOptions.CorrelationCookie.SameSite = SameSiteMode.None;
    googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always; // TEMP DEBUG
});

// Configure Swagger / annotations already configured earlier
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
});
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<EnrollmentCheckMiddleware>();
app.MapHub<NotificationHub>("/notificationHub");


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASDPRS");
        c.RoutePrefix = "swagger";
    });
}

// Ensure CORS for frontend is applied before auth
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Default route
app.MapGet("/", () => "ASDPRS API is running! Visit /swagger for API documentation.");

ExcelPackage.License.SetNonCommercialOrganization("ASDPRS Project");


// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ASDPRSContext>();
        // Check if there are any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
            Console.WriteLine("Database migrated successfully.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
