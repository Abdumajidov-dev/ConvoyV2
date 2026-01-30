using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Convoy.Service.Services.FireBaseService;

public static class FirebaseServiceExtensions
{
    /// <summary>
    /// Firebase service ni DI container ga qo'shish
    /// </summary>
    public static IServiceCollection AddFirebaseNotification(this IServiceCollection services, IConfiguration configuration)
    {
        // Firebase service ni Singleton qilib register qilish
        services.AddSingleton<DirectFirebaseService>();

        return services;
    }

    /// <summary>
    /// Firebase service ni custom path bilan qo'shish
    /// </summary>
    public static IServiceCollection AddFirebaseNotification(this IServiceCollection services, string firebaseConfigPath)
    {
        services.AddSingleton<DirectFirebaseService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();

            // Custom path ni configuration ga qo'shish
            configuration["Firebase:ConfigPath"] = firebaseConfigPath;

            var environment = provider.GetRequiredService<IHostEnvironment>();
            return new DirectFirebaseService(configuration, environment);
        });

        //services.AddSingleton<SimpleFirebaseHelper>();

        return services;
    }
}