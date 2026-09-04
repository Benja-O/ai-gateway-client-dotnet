using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orchestrator.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="ILlmGateway"/> leyendo la seccion de configuracion.
    /// </summary>
    /// <param name="services">Coleccion de servicios.</param>
    /// <param name="configuracion">Configuracion raiz (appsettings.json, etc.).</param>
    /// <param name="seccion">Nombre de la seccion; por defecto "LlmGateway".</param>
    public static IServiceCollection AddLlmGateway(
        this IServiceCollection services,
        IConfiguration configuracion,
        string seccion = "LlmGateway")
    {
        var opciones = configuracion.GetSection(seccion).Get<LlmGatewayOptions>() ?? new LlmGatewayOptions();
        return services.AddLlmGateway(opciones);
    }

    /// <summary>
    /// Registra <see cref="ILlmGateway"/> configurando las opciones en codigo.
    /// </summary>
    public static IServiceCollection AddLlmGateway(
        this IServiceCollection services,
        Action<LlmGatewayOptions> configurar)
    {
        var opciones = new LlmGatewayOptions();
        configurar(opciones);
        return services.AddLlmGateway(opciones);
    }

    /// <summary>
    /// Registra <see cref="ILlmGateway"/> con opciones ya construidas.
    /// </summary>
    public static IServiceCollection AddLlmGateway(
        this IServiceCollection services,
        LlmGatewayOptions opciones)
    {
        services.AddSingleton(opciones);
        services.AddSingleton<ILlmGateway, LlmGateway>();
        return services;
    }
}
