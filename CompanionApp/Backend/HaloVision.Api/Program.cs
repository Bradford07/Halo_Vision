using HaloVision.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

//Set up the web server:: settings, logging, database and logins
var builder = WebApplication.CreateBuilder(args);

//Register the database: SQLite, using the "Default" connection string
//(appsettings.Development.json on our PCs; the tests supply their own).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

//Register guide accounts (ASP.NET Core Identity), stored in our database.
//The login endpoints themselves are added in the athletes step.
builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();

//Build the app from that setup.
var app = builder.Build();

//On our PCs only: build the database from the migrations and add fake data if it's empty.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DevDataSeeder.SeedAsync(scope.ServiceProvider);
}

//Endpoint that answers "server is running".
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

//Start the serer and keep it running until the user stops it.
app.Run();

//Lets the test project start this app in memory (WebApplicationFactory<Program>).
public partial class Program { }
