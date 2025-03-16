using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Repository.Services;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Backend.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<UserManager<ApplicationUser>>();
            services.AddScoped<RoleManager<IdentityRole>>();

            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IBetService, BetService>();
            services.AddScoped<IMatchService, MatchService>();
            services.AddScoped<ICustomTournamentService, CustomTournamentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPredefinedTournamentService, PredefinedTournamentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IPushNotificationService, PushNotificationService>();

            services.AddHostedService<MatchUpdateHostedService>();
            services.AddHostedService<BetUpdateHostedService>();

            return services;
        }
    }
}
