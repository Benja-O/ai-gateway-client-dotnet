namespace Orchestrator.Client;

/// <summary>
/// Configuracion del SDK. La app elige TAREAS (semanticas); el mapeo
/// tarea -&gt; alias de modelo vive aca, y el proxy resuelve el resto.
/// </summary>
public sealed class LlmGatewayOptions
{
    /// <summary>URL base del proxy LiteLLM (API OpenAI-compatible).</summary>
    public string BaseUrl { get; set; } = "http://localhost:4000/v1";

    /// <summary>
    /// Key que se envia al proxy. LiteLLM por defecto no la valida;
    /// se puede dejar cualquier valor o configurar master key en el proxy.
    /// </summary>
    public string ApiKey { get; set; } = "sk-local";

    /// <summary>
    /// Mapeo tarea semantica -&gt; alias de modelo en el proxy.
    /// Se puede extender por app (ej. "resumen": "groq-free").
    /// </summary>
    public Dictionary<string, string> Tareas { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chat"] = "groq-free",          // simple y rapido (fallback automatico en proxy)
        ["contexto-largo"] = "gemini-free", // contexto grande / multimodal
        ["investigacion"] = "deepseek",   // razonamiento profundo (respaldo garantizado)
        ["privado"] = "local",            // offline: Ollama en GPU propia
    };

    /// <summary>Tiempo maximo de espera por respuesta en segundos (por defecto 120).</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
