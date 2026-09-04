using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orchestrator.Client;

// La app NO sabe que modelos hay detras: solo pide por tarea semantica.
// El proxy LiteLLM decide el modelo real y aplica fallback si hace falta.

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLlmGateway(builder.Configuration.GetSection("LlmGateway"));

using var host = builder.Build();
var gateway = host.Services.GetRequiredService<ILlmGateway>();

Console.WriteLine("=== Orchestrator.Client (SDK del AI Gateway) ===\n");
Console.WriteLine("Tareas configuradas: " + string.Join(", ", gateway.TareasDisponibles) + "\n");

// Misma interfaz para todas: cambia SOLO la tarea (y por lo tanto el modelo).
var demos = new (string Tarea, string Etiqueta, string Prompt)[]
{
    ("investigacion", "DeepSeek  (investigacion profunda)", "Responde solo con la palabra OK"),
    ("contexto-largo", "Gemini 2.5 Flash  (contexto-largo)", "Responde solo con la palabra OK"),
    ("privado", "qwen3-vl:8b local en GPU  (privado)", "Responde solo con la palabra OK"),
    ("chat", "Groq Llama 3.3 70B  (chat; sin key -> el proxy hace fallback a Gemini)", "Responde solo con la palabra OK"),
};

foreach (var (tarea, etiqueta, prompt) in demos)
{
    Console.WriteLine($"--- {etiqueta} ---");
    try
    {
        var respuesta = await gateway.CompleteAsync(
            tarea,
            prompt,
            new ChatOptions { MaxOutputTokens = 1000 });

        var texto = respuesta.Messages.LastOrDefault()?.Text ?? "(respuesta vacia)";
        Console.WriteLine($"  modelo reportado : {respuesta.ModelId}");
        Console.WriteLine($"  respuesta        : {texto}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR (gestionado): {ex.Message}");
    }
    Console.WriteLine();
}

Console.WriteLine("=== Fin de la demo ===");
