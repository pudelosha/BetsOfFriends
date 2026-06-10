using Backend.Model.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Extensions
{
    public static class AppConfigExtensions
    {
        public static IServiceCollection AddDatabaseConfig(this IServiceCollection services, IConfiguration config, string env)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString(env),
                    sql =>
                    {
                        sql.MigrationsHistoryTable("__EFMigrationsHistory", "betsoffriends_db_admin");
                        sql.UseCompatibilityLevel(120);
                    }
                ));

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                              "http://localhost:8100",
                              "http://localhost:3000",
                              "http://app.betsoffriends.com",
                              "https://app.betsoffriends.com",
                              "http://api.betsoffriends.com",
                              "https://api.betsoffriends.com"
                            )
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }

        public static IServiceCollection ConfigureRouting(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = false;
            });

            return services;
        }
    }
}
