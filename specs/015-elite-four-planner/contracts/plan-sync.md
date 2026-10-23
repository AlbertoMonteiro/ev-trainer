# Contract: equipe planejada (aparelho e sincronização)

## Aparelho — `evtrainer:v1:plan`

```json
{
  "round": "first",
  "starter": "bulbasaur",
  "members": [
    { "species": "Raichu", "ability": "Static", "moves": ["Thunderbolt", "Dig"], "locked": ["Dig"] }
  ],
  "updatedAt": "2026-10-23T12:00:00.0000000Z"
}
```

Leitura tolerante: JSON inválido → plano vazio; campos inválidos normalizados (data-model.md).

## Documento sincronizado (Gist `evs-trainer.json`)

O documento de dados ganha a seção opcional `plan` (mesmo formato acima):

```json
{ "collection": "pokemon", "documents": { ... }, "deleted": { ... }, "plan": { ... } }
```

- Escrita: `plan` só é gravado quando existe um plano carimbado.
- Merge (`SyncMerge`): vence o `updatedAt` mais recente; empate → maior JSON (ordem ordinal); plano ausente perde
  para presente. O resultado indica se o plano local mudou (aplicar) e se o remoto precisa ser reescrito.
- O arquivo de backup (exportar/importar) não leva o plano; importar não mexe nele.
