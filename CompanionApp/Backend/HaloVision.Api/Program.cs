//Set up the web server:: settings, logging, database and logins
var builder = WebApplication.CreateBuilder(args);

//Build the app from that setup.
var app = builder.Build();


//Endpoint that answers "server is running".
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

//Start the serer and keep it running until the user stops it.
app.Run();
