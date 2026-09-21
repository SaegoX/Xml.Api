using Xml.Api.Features.ScheduleUpload.Infrastructure;

namespace Xml.Api.Features.ScheduleUpload.Extensions
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

            services.AddHostedService<ScheduleBackgroundWorker>();

            services.AddScoped<ScheduleReader>();
            services.AddScoped<ScheduleMapper>();
            services.AddScoped<ScheduleSaver>();
            services.AddScoped<XmlScheduleParserService>();

            return services;
        }
    }
}
