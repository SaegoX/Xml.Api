using Microsoft.EntityFrameworkCore;
using Xml.Api.Data;
using Xml.Api.Features.ScheduleApi;
using Xml.Api.Features.ScheduleUpload.Extensions;
using Xml.Api.Features.ScheduleUpload.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDbConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddSignalR();

builder.Services.AddScheduleUploadFeature();

builder.Services.AddControllersWithViews();

builder.Services.Configure<RaspOptions>(builder.Configuration.GetSection("Rasp"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при создании бд");
    }
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapStaticAssets();
app.MapControllers().WithStaticAssets();

app.MapHub<ScheduleHub>("/schedule-hub");

app.MapDefaultControllerRoute();

app.Run();
