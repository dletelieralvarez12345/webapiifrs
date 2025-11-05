using Microsoft.EntityFrameworkCore;
using webApiIFRS.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ConnContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaContext"))
        .EnableSensitiveDataLogging()
        .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddDbContext<ConnContextCTACTE>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaContextCTATE")));

builder.Services.AddDbContext<ConnContextSICM>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaContextSICM")));

builder.Services.AddDbContext<ConnContextSEPULTA>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaContextSEPULTA")));

builder.Services.AddDbContext<ConnContextSICMPBI>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaContextSICMPBI")));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
