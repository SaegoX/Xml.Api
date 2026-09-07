using Microsoft.EntityFrameworkCore;
using Xml.Api.Data;
using Xml.Api.Features.ScheduleUpload;
using Xml.Api.Features.ScheduleUpload.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDbConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddScheduleUploadFeature();

builder.Services.AddControllersWithViews();

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
app.MapDefaultControllerRoute();

app.Run();
