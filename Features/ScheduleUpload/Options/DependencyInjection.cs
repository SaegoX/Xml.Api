using Xml.Api.Features.ScheduleUpload.Infrastructure;

namespace Xml.Api.Features.ScheduleUpload.Options
{
    /// <summary>
    /// Класс расширения для лаконичной регистрации всех модулей фичи загрузки расписания(фасад)
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddScheduleUploadFeature(this IServiceCollection services)
        {
            services.AddSingleton<UploadProgressTracker>();
            services.AddSingleton<BackgroundQueue>();

            services.AddHostedService<BackgroundWorker>();

            services.AddScoped<Reader>();
            services.AddScoped<Mapper>();
            services.AddScoped<Saver>();
            services.AddScoped<ParserOrecstratorService>();

            return services;
        }
    }
}
