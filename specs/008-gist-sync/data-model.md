# Data Model: Sincronização com Gist privado

## Mudanças em entidades existentes

### TrainingPokemon

| Campo novo | Tipo | JSON | Regras |
|---|---|---|---|
| `UpdatedAt` | `DateTimeOffset?` | `"updatedAt": "2026-10-11T21:30:00Z"` (opcional) | carimbado em UTC a cada comando que muda o Pokémon (research R5); ausente/ inválido na leitura ⇒ `null` (= "nunca alterado") |

### Documento de dados / backup (`evtrainer:v1:data`, export, conteúdo do Gist)

```json
{
  "collection": "pokemon",
  "documents": { "p1": { "…": "…", "updatedAt": "2026-10-11T21:30:00Z" } },
  "deleted": { "p9": "2026-10-10T18:02:11Z" }
}
```

| Campo | Regras de leitura (tolerante) |
|---|---|
| `documents[id].updatedAt` | data ISO 8601 válida, senão `null` |
| `deleted` | opcional; cada valor data ISO 8601 válida, senão a entrada é ignorada; ids que também estão em `documents` ficam com a regra do mais recente |

Arquivos antigos (sem `updatedAt` nem `deleted`) continuam válidos. Importar um backup carimba os
Pokémon importados com o horário da importação.

## Entidades novas

### SyncConnection (só no aparelho, `evtrainer:v1:sync`)

| Campo | Tipo | Regras |
|---|---|---|
| `Provider` | `"gist"` | único provedor desta versão |
| `Token` | `string` | nunca exibido após salvo; nunca no backup, no Gist, em URL ou mensagens |
| `Login` | `string` | conta GitHub confirmada na conexão |
| `Location` | `string` | onde os dados estão no provedor (o id do Gist) |
| `LastSyncedAt` | `DateTimeOffset?` | última sincronização concluída |
| `ETag` | `string?` | última versão lida do Gist (leitura condicional) |

Documento ausente/inválido ⇒ sincronização desligada.

### SyncState (memória)

`Off` · `Syncing` · `Synced(at)` · `Pending` · `Error(kind)`, com
`kind ∈ { Offline, InvalidToken, MissingGistPermission, RateLimited, InvalidRemote, Unknown }`.

```text
Off ──Conectar ok──▶ Syncing ──▶ Synced
Synced ──mudança local──▶ Pending ──(5 s / visível / abrir / Sincronizar agora)──▶ Syncing
Syncing ──falha──▶ Error(kind) ──próximo gatilho──▶ Syncing
qualquer ──Desconectar──▶ Off
```

### MergeResult (domínio, `SyncMerge.Merge`)

| Campo | Significado |
|---|---|
| `Documents` | Pokémon resultantes por id |
| `Deleted` | exclusões mantidas (≤ 90 dias) |
| `Updated`, `Added`, `Removed` | quantos Pokémon locais mudaram por causa do remoto (para o aviso) |
| `LocalChanged` | o local precisa ser substituído |
| `RemoteChanged` | o Gist precisa ser regravado |
