using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Campanhas.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDoadorService, DoadorService>();
        services.AddScoped<ICampanhaService, CampanhaService>();
        services.AddScoped<IDoacaoService, DoacaoService>();
        return services;
    }
}
