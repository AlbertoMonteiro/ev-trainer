# Contract: Sincronização (domínio, provedor, serviço e UI)

## Domínio (`EvTrainer.Web.Domain`, C# puro)

```csharp
public sealed record SyncSnapshot(
    IReadOnlyDictionary<string, TrainingPokemon> Documents,
    IReadOnlyDictionary<string, DateTimeOffset> Deleted);

public sealed record MergeResult(
    Dictionary<string, TrainingPokemon> Documents,
    Dictionary<string, DateTimeOffset> Deleted,
    int Updated, int Added, int Removed,
    bool LocalChanged, bool RemoteChanged);

public static class SyncMerge
{
    public static readonly TimeSpan DeletionRetention = TimeSpan.FromDays(90);
    public static MergeResult Merge(SyncSnapshot local, SyncSnapshot remote, DateTimeOffset now);
}
```

Casos obrigatórios (testes):

| Local | Remoto | Resultado |
|---|---|---|
| A@10h | — | A@10h; `RemoteChanged` |
| — | A@10h | A@10h; `Added = 1`; `LocalChanged` |
| A@10h | A@11h | A@11h (remoto); `Updated = 1` |
| A@11h | A@10h | A@11h (local); `RemoteChanged` |
| A@10h | apagado@11h | sem A; exclusão@11h; `Removed = 1` |
| apagado@11h | A@10h | sem A; exclusão@11h; `RemoteChanged` |
| apagado@10h | A@11h (editado depois) | A@11h; sem exclusão; `Added = 1` |
| A sem data | A@10h | A@10h |
| A@10h ≠ A@10h (mesma hora) | | os dois lados escolhem o mesmo (desempate pelo conteúdo) |
| exclusão de 91 dias | | descartada |
| merge(merge(x, y), y) | | igual a merge(x, y) (idempotente) |

## Provedor (`EvTrainer.Web.State`)

```csharp
public enum SyncErrorKind { Offline, InvalidToken, MissingGistPermission, RateLimited, InvalidRemote, Unknown }

public sealed record SyncAccount(string Login, string Location);           // Location = gist id
public sealed record RemoteRead(string? Json, string? ETag, bool NotModified);

public interface ISyncProvider
{
    Task<SyncAccount> ConnectAsync(string token, CancellationToken ct);        // valida, acha ou cria
    Task<RemoteRead> ReadAsync(SyncConnection connection, string? etag, CancellationToken ct);
    Task<string?> WriteAsync(SyncConnection connection, string json, CancellationToken ct); // novo ETag
}

public sealed class SyncException(SyncErrorKind kind) : Exception;          // sem texto do servidor
```

`GistSyncProvider` (HTTP, base `https://api.github.com`, cabeçalhos do research R1):

| Operação | Chamada | Erros |
|---|---|---|
| Conectar | `GET /user` → login; `GET /gists?per_page=100` (+ páginas via `Link`) → o mais antigo com o arquivo `evs-trainer.json`; senão `POST /gists` `{ description: "EVs Trainer", public: false, files: { "evs-trainer.json": { content } } }` | 401 → `InvalidToken`; 403/404 em gists → `MissingGistPermission`; `x-ratelimit-remaining: 0` → `RateLimited`; falha de rede → `Offline` |
| Ler | `GET /gists/{id}` com `If-None-Match` | 304 → `NotModified`; arquivo ausente/`truncated` → `InvalidRemote` |
| Gravar | `PATCH /gists/{id}` `{ files: { "evs-trainer.json": { content } } }` | idem |

## Serviço

```csharp
public sealed class SyncService : IDisposable
{
    public SyncState State { get; }
    public string? Login { get; }
    public DateTimeOffset? LastSyncedAt { get; }
    public event Action? Changed;

    public Task<SyncErrorKind?> ConnectAsync(string token);   // null = ok
    public void Disconnect();
    public Task SyncNowAsync();                               // ciclo do research R6
    public void Start();                                      // ao iniciar: se conectado, sincroniza; liga gatilhos
    public void OnVisible();                                  // chamado pelo JS
}
```

Gatilhos: `Start`, `OnVisible`, 5 s após `TrainerStore.Changed` com gravação, e `SyncNowAsync`.
Mudanças aplicadas pelo próprio serviço não disparam o debounce.

## Armazenamento

`IPokemonRepository` ganha `SyncConnection? LoadSyncConnection()`, `void SaveSyncConnection(SyncConnection)`
e `void ClearSyncConnection()` (chave `evtrainer:v1:sync`). `TrainerStore` ganha `SyncSnapshot Snapshot()`
e `void ApplySync(MergeResult)` (substitui os dados sem carimbar), e as exclusões passam a ser
gravadas no documento de dados (`deleted`).

## UI (pt-BR, chaves `Sync.*`)

| Onde | Elemento | Comportamento |
|---|---|---|
| Menu ☰ | **Sincronização** (`Menu.Sync`) | abre a tela (mesmo padrão de Avaliar captura) |
| Tela, desligada | explicação, aviso de Gist secreto, passo a passo com link para criar o token (`gist`), campo `type=password`, **Conectar** | erros por tipo (`Sync.Error.*`) |
| Tela, ligada | "Conectado como @login", última sincronização, **Sincronizar agora**, **Desconectar** (com confirmação), link para o Gist | |
| Status (cabeçalho) | ☁️ sincronizado (horário) · 🔄 sincronizando · ⏳ pendente · ⚠️ erro | toque abre a tela |
| Aviso após sincronizar | `Sync.Merged` com atualizados/adicionados/removidos | só quando algo veio do outro aparelho |
