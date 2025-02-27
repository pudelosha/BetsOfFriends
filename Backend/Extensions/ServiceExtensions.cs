using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Backend.Repository.Services;
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
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IBetService, BetService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<ICustomTournamentService, CustomTournamentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPredefinedTournamentService, PredefinedTournamentService>();

            return services;
        }
    }
}
