using SciTransNet.Services;
using SciTransNet.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from appsettings.json and appsettings.Development.json
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();

// Register HTTP client for TranslationService
builder.Services.AddHttpClient<ITranslationService, TranslationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers(); // This is critical!

app.Run();