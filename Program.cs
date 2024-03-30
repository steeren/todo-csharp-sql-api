using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleTodo.Api;

var builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();
string keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
Console.WriteLine($"keyVaultEndpoint: {keyVaultEndpoint}");
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultEndpoint), credential);

builder.Services.AddScoped<ListsRepository>();
builder.Services.AddDbContext<TodoDb>(options =>
{
    var connectionString = builder.Configuration[builder.Configuration["AZURE_SQL_CONNECTION_STRING_KEY"]];
    //var connectionString = "Server=tcp:todo-charp-sql-api-server.database.windows.net,1433;Initial Catalog=todo-charp-sql-api-database;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=¸\"Active Directory Default\";";
    //var connectionString = "Server=tcp:todo-charp-sql-api-server.database.windows.net,1433;Initial Catalog=todo-charp-sql-api-database;Persist Security Info=False;User ID=todo-charp-sql-api-server-admin;Password=38OJZN3FIFUP536V$;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    Console.WriteLine($"connectionString: {connectionString}");
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});

// Swagger UI
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("./openapi.yaml", "v1");
    options.RoutePrefix = "";
});

app.UseStaticFiles(new StaticFileOptions
{
    // Serve openapi.yaml file
    ServeUnknownFileTypes = true,
});


app.MapGroup("/lists")
    .MapTodoApi()
    .WithOpenApi();
app.Run();