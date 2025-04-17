using SciTransNet.Services;
using SciTransNet.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Load config files
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

// Register services
builder.Services.AddControllers();
builder.Services.AddHttpClient<ITranslationService, TranslationService>();
builder.Services.AddScoped<IFileParserService, FileParserService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();