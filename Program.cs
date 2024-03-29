using Azure.Identity;
using DotNetCoreSqlDb.Data;
using Microsoft.EntityFrameworkCore;
using SimpleTodo.Api;

var builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();
string keyVaultEndpoint = builder.Configuration.GetConnectionString("AZURE_KEY_VAULT_ENDPOINT");
Console.WriteLine($"keyVaultEndpoint: {keyVaultEndpoint}");
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultEndpoint), credential);

builder.Services.AddScoped<ListsRepository>();
builder.Services.AddDbContext<TodoDb>(options =>
{
    builder.Services.AddDbContext<MyDatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING"), sqlOptions => sqlOptions.EnableRetryOnFailure()));
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