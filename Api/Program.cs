using Api.Services;
using Microsoft.EntityFrameworkCore;
using ORM;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Polar verlangt bei allen Requests einen User-Agent-Header
builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
    httpClientBuilder.ConfigureHttpClient(client =>
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PolarHealthCompanion/1.0")));
builder.Services.AddHttpClient<PolarClient>(client =>
    client.BaseAddress = new Uri("https://www.polaraccesslink.com/v3/"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection fehlt in appsettings.json.");

builder.Services.AddDbContext<DbManager>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddCors(options =>
    options.AddPolicy("VueClient", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Legt die Datenbank samt users-Tabelle an, falls sie noch nicht existiert
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DbManager>().Database.EnsureCreated();
}

app.UseCors("VueClient");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
