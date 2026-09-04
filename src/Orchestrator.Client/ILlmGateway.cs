using Microsoft.Extensions.AI;

namespace Orchestrator.Client;

/// <summary>
/// Puerta de entrada unica a los modelos. La app pide por TAREA semantica
/// (ej. "investigacion") y nunca nombra un proveedor concreto:
/// el proxy LiteLLM decide el modelo y aplica fallback/cooldown.
/// </summary>
public interface ILlmGateway
{
    /// <summary>Lista de tareas configuradas (claves del mapeo).</summary>
    IReadOnlyCollection<string> TareasDisponibles { get; }

    /// <summary>
    /// Completa un chat de una sola pasada pidiendo por tarea.
    /// </summary>
    /// <param name="tarea">Clave semantica definida en <see cref="LlmGatewayOptions.Tareas"/>.</param>
    /// <param name="prompt">Texto del usuario.</param>
    /// <param name="opciones">Opciones extra (temperatura, max_tokens, etc.).</param>
    Task<ChatResponse> CompleteAsync(
        string tarea,
        string prompt,
        ChatOptions? opciones = null,
        CancellationToken ct = default);

    /// <summary>
    /// Igual que <see cref="CompleteAsync(string,string,ChatOptions?,CancellationToken)"/>
    /// pero con historial de mensajes (multi-turno / system prompt).
    /// </summary>
    Task<ChatResponse> CompleteAsync(
        string tarea,
        IEnumerable<ChatMessage> mensajes,
        ChatOptions? opciones = null,
        CancellationToken ct = default);

    /// <summary>
    /// Escape hatch: devuelve el <see cref="IChatClient"/> (Microsoft.Extensions.AI)
    /// asociado a un alias concreto del proxy (streaming, tools, etc.).
    /// </summary>
    /// <param name="alias">Alias del proxy (ej. "deepseek", "local").</param>
    IChatClient GetChatClient(string alias);

    /// <summary>
    /// Escape hatch por tarea: mismo resultado que <see cref="GetChatClient(string)"/>
    /// pero resolviendo el alias a partir de la tarea semantica.
    /// </summary>
    IChatClient GetChatClientForTask(string tarea);
}
