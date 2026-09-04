using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using System.ClientModel;

namespace Orchestrator.Client;

/// <summary>
/// Implementacion sobre Microsoft.Extensions.AI. Crea un cliente OpenAI
/// apuntando al proxy LiteLLM y cachea un <see cref="IChatClient"/> por alias.
/// </summary>
public sealed class LlmGateway : ILlmGateway, IDisposable
{
    private readonly LlmGatewayOptions _options;
    private readonly ILogger<LlmGateway> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly Dictionary<string, IChatClient> _byAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public LlmGateway(LlmGatewayOptions options, ILogger<LlmGateway> logger)
    {
        _options = options;
        _logger = logger;

        _openAIClient = new OpenAIClient(
            new ApiKeyCredential(_options.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.BaseUrl),
            });
    }

    public IReadOnlyCollection<string> TareasDisponibles => _options.Tareas.Keys.ToArray();

    public Task<ChatResponse> CompleteAsync(
        string tarea, string prompt,
        ChatOptions? opciones = null, CancellationToken ct = default)
        => CompleteAsync(tarea, [new ChatMessage(ChatRole.User, prompt)], opciones, ct);

    public async Task<ChatResponse> CompleteAsync(
        string tarea, IEnumerable<ChatMessage> mensajes,
        ChatOptions? opciones = null, CancellationToken ct = default)
    {
        var chat = GetChatClientForTask(tarea);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        _logger.LogInformation("Llamada al gateway. Tarea={Tarea}, modelo-alias={Alias}",
            tarea, AliasDe(tarea));

        var respuesta = await chat.GetResponseAsync(mensajes, opciones, timeout.Token).ConfigureAwait(false);

        _logger.LogInformation("Respuesta recibida. Tarea={Tarea}, modelo-reportado={Modelo}",
            tarea, respuesta.ModelId);

        return respuesta;
    }

    public IChatClient GetChatClientForTask(string tarea)
        => GetChatClient(AliasDe(tarea));

    public IChatClient GetChatClient(string alias)
    {
        lock (_sync)
        {
            if (!_byAlias.TryGetValue(alias, out var client))
            {
                var chatClient = _openAIClient.GetChatClient(alias);
                client = chatClient.AsIChatClient();
                _byAlias[alias] = client;
            }
            return client;
        }
    }

    private string AliasDe(string tarea)
    {
        if (!_options.Tareas.TryGetValue(tarea, out var alias))
        {
            throw new KeyNotFoundException(
                $"La tarea '{tarea}' no esta configurada. Tareas disponibles: {string.Join(", ", TareasDisponibles)}");
        }
        return alias;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            foreach (var client in _byAlias.Values)
            {
                (client as IDisposable)?.Dispose();
            }
            _byAlias.Clear();
        }
        if (_openAIClient is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
