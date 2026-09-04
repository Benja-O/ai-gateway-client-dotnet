# Orchestrator.Client — SDK .NET del AI Gateway

Librería genérica para que **cualquier app .NET** hable con modelos LLM sin saber
qué proveedor hay detrás. La app pide por **tarea semántica** (`investigacion`,
`contexto-largo`, `chat`, `privado`…) y el [proxy LiteLLM](../../litellm-proxy/README.md)
(AI Gateway) decide el modelo real, aplica fallback automático por cuotas free y
maneja cooldowns. Construida sobre **Microsoft.Extensions.AI**.

```
Tu app  →  Orchestrator.Client  →  Proxy LiteLLM (gateway)  →  DeepSeek · Gemini · Groq · Ollama
          (tarea semántica)       (ruteo + fallback + cuotas)
```

## Estructura

```
orchestrator-dotnet/
├─ Orchestrator.slnx
├─ src/
│  ├─ Orchestrator.Client/      ← la librería reutilizable
│  └─ Orchestrator.SampleApp/   ← consola demo (4 tareas)
```

## Cómo la usa cualquier app .NET

### 1) Referencia el proyecto (o el paquete NuGet, cuando lo publiques)

### 2) Registrala en DI (2 formas)

Desde configuración (appsettings.json):

```json
{
  "LlmGateway": {
    "BaseUrl": "http://localhost:4000/v1",
    "ApiKey": "sk-local",
    "TimeoutSeconds": 120,
    "Tareas": {
      "chat": "groq-free",
      "contexto-largo": "gemini-free",
      "investigacion": "deepseek",
      "privado": "local"
    }
  }
}
```

```csharp
builder.Services.AddLlmGateway(builder.Configuration.GetSection("LlmGateway"));
```

O en código:

```csharp
builder.Services.AddLlmGateway(opciones => {
    opciones.BaseUrl = "http://localhost:4000/v1";
    opciones.Tareas["resumen"] = "gemini-free";   // tu app agrega sus propias tareas
});
```

### 3) Consumila pidiendo por tarea (nunca por modelo)

```csharp
public class MiServicio(ILlmGateway gateway)
{
    public async Task<string> Investigar(string tema)
    {
        var respuesta = await gateway.CompleteAsync(
            "investigacion",                    // tarea semántica
            $"Investiga a fondo: {tema}",
            opciones: new() { MaxOutputTokens = 2000 });
        return respuesta.Messages.LastOrDefault()?.Text ?? "";
    }
}
```

Multi-turno / system prompt:

```csharp
var mensajes = new List<ChatMessage> {
    new(ChatRole.System, "Sos un asistente experto."),
    new(ChatRole.User,   "¿Qué es un AI Gateway?"),
};
var respuesta = await gateway.CompleteAsync("chat", mensajes);
```

Escape hatches (streaming, tools…):

```csharp
var chat = gateway.GetChatClientForTask("privado");        // IChatClient de M.E.AI
var chat = gateway.GetChatClient("deepseek");              // por alias directo
```

## Mapeo tarea → alias (política por defecto)

| Tarea | Alias del proxy | Por qué |
|---|---|---|
| `chat` | `groq-free` | simple y rápido (fallback automático en proxy) |
| `contexto-largo` | `gemini-free` | contexto grande / multimodal |
| `investigacion` | `deepseek` | razonamiento profundo |
| `privado` | `local` | offline: Ollama en GPU propia (RTX 3060) |

Cada app puede **extender o redefinir** el mapeo en su configuración.

## Correr la demo

```powershell
# 1) Proxy LiteLLM arriba (puerto 4000):  cd ..\..\litellm-proxy; .\start.ps1
# 2) Ejecutar sample
cd F:\Desarrollo\Deepseek\orchestrator-dotnet
dotnet run --project src\Orchestrator.SampleApp
```

Salida esperada (con keys de DeepSeek/Google y Ollama activo; `chat` demuestra
el **fallback automático** cuando Groq no tiene key):

```
--- DeepSeek  (investigacion profunda) ---      modelo reportado : deepseek         respuesta: OK
--- Gemini 2.5 Flash  (contexto-largo) ---      modelo reportado : gemini-free      respuesta: OK
--- qwen3-vl:8b local en GPU  (privado) ---     modelo reportado : local            respuesta: OK
--- Groq Llama 3.3 70B  (chat, sin key) ---     modelo reportado : gemini-2.5-flash respuesta: OK  ← fallback
```

## Notas técnicas

- **Stack**: .NET 10 · `Microsoft.Extensions.AI` 10.9 · `Microsoft.Extensions.AI.OpenAI` 10.9 ·
  `OpenAI` 2.12 · DI/logging/config estándar.
- En esta línea de M.E.AI la API base es `IChatClient.GetResponseAsync(...)`; el SDK la envuelve
  con el método semántico `CompleteAsync`. El conector convierte `OpenAIClient` con
  `GetChatClient(alias).AsIChatClient()`.
- El proxy LiteLLM es OpenAI-compatible: por eso un solo cliente sirve para todos los proveedores.
- `Response.ModelId` reporta el alias (o el modelo real tras un fallback) que respondió.

## Próximos pasos posibles

- Empaquetar como NuGet (`dotnet pack`) para distribuirlo entre apps.
- Agregar `ILlmGateway.GetStreamingAsync(...)` (streaming) con `GetStreamingResponseAsync`.
- Middleware de M.E.AI: logging, caché (`UseCaching`), OpenTelemetry — sin tocar las apps.
