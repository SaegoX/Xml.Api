namespace Xml.Api.Features.ScheduleUpload.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddScheduleUploadFeature(this IServiceCollection services)
        {
            services.AddScoped<ScheduleReader>();
            services.AddScoped<ScheduleMapper>();
            services.AddScoped<ScheduleSaver>();
            services.AddScoped<XmlScheduleParserService>();

            return services;
        }
    }
}
