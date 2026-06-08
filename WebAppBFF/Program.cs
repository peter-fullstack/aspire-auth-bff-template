
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add YARP to the service collection and load configuration from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // 1. Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // CRITICAL SECURITY FLAGS
        options.Cookie.HttpOnly = true; 
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Only send over HTTPS
        options.Cookie.SameSite = SameSiteMode.Strict;          // Mitigate CSRF
        options.Cookie.Name = "__Host-ReactBff"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    })
    .AddOpenIdConnect(options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = builder.Configuration["AzureAd:Instance"] + builder.Configuration["AzureAd:TenantId"];
        options.ClientId = builder.Configuration["AzureAd:ClientId"];
        options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
        options.CallbackPath = builder.Configuration["AzureAd:CallbackPath"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection(); // Recommended for production

// Map YARP's reverse proxy functionality
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();

// 1. Endpoint for the React app to determine if the user is logged in
app.MapGet("/bff/user/status", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated ?? false)
    {
        return Results.Ok(new
        {
            isAuthenticated = true,
            username = user.Identity.Name,
            roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
        });
    }

    // Return a clean payload instead of a 401, so the frontend can handle it smoothly
    return Results.Ok(new { isAuthenticated = false });
})
.AllowAnonymous();

app.MapGet("/bff/login", () => 
{
    // Challenge forces ASP.NET Core to redirect the user's browser to Entra ID
    return Results.Challenge(
        properties: new AuthenticationProperties { RedirectUri = "/" }, 
        authenticationSchemes: new[] { OpenIdConnectDefaults.AuthenticationScheme }
    );
});