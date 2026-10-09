# Contract: formato de backup e de armazenamento local

Usado em três lugares, com o **mesmo** conteúdo:

1. Arquivo exportado (`evs-trainer-backup-AAAA-MM-DD.json`, FR-025).
2. Arquivo importado, incluindo o `user-data-export.json` existente (FR-026).
3. Valor da chave `localStorage` `evtrainer:v1:data` (research R4).

## Estrutura

```json
{
  "collection": "pokemon",
  "documents": {
    "<id>": {
      "name": "Wartortle",
      "nature": "Timid",
      "species": [
        { "name": "Squirtle",  "base": [44, 48, 65, 50, 64, 43] },
        { "name": "Wartortle", "base": [59, 63, 80, 65, 80, 58] }
      ],
      "targets": [252, 0, 2, 252, 0, 4],
      "events": [ { "o": "Caterpie", "n": 8, "m": 1 } ],
      "ivLog": [
        { "lv": 5, "sp": "Squirtle", "st": [20, 9, 12, 11, 12, 11], "ev": [0, 0, 0, 0, 0, 0] }
      ],
      "pokerus": false,
      "macho": false,
      "order": 1
    }
  }
}
```

Significado e validação de cada campo: [data-model.md](../data-model.md).

## Regras de escrita

- `collection` sempre `"pokemon"`.
- Todos os campos acima são sempre escritos (sem omitir padrões).
- `events[].o` é escrito exatamente como foi gravado (aliases **não** são reescritos).
- UTF-8; JSON indentado no arquivo exportado e compacto no `localStorage`.

## Regras de leitura (tolerante)

| Situação | Comportamento |
|---|---|
| JSON inválido, `collection` ≠ `"pokemon"` ou `documents` ausente/não-objeto | Rejeita o arquivo inteiro; nada muda; mensagem de erro. |
| Propriedade desconhecida | Ignorada. |
| `n` ausente ou < 1 | Lido como 1. |
| `m` ausente ou fora de {1, 2, 4} | Lido como 1. |
| `targets` / `st` / `ev` / `base` com tamanho ≠ 6 | Completa com 0 / trunca em 6. |
| `species` vazio | Lido com 1 espécie sem nome (usuário corrige em Ajustes). |
| `ivLog` fora de ordem | Reordenado por `lv` na leitura. |
| `order` ausente | Recebe a próxima posição livre. |

## Importação de backup (merge)

1. Lê e valida o arquivo → lista de `(id, TrainingPokemon)`.
2. Prévia: `novos` = ids inexistentes; `substituídos` = ids já existentes.
3. Confirmação → para cada id, substitui ou adiciona. Pokémon locais ausentes do arquivo são
   mantidos.

## Garantia de ida e volta

`Read(Write(x)) == x` para qualquer conjunto `x` válido (SC-004). Importar
`user-data-export.json` e exportar em seguida produz documentos equivalentes campo a campo.
