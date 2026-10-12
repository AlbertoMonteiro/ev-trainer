# Data Model: App em inglês

Nenhuma entidade de treino muda: backup, armazenamento dos Pokémon e sincronização ficam iguais.

## Preferência de idioma (`evtrainer:v1:prefs`, só no aparelho)

| Campo | Antes | Depois |
|---|---|---|
| `language` | `string`, padrão `"pt-BR"` | `string?`, padrão ausente/`null` |

- **Leitura**: valor salvo disponível no catálogo (`pt-BR`, `en`) → usado; ausente, `null` ou
  desconhecido → idioma detectado pelo navegador (não gravado).
- **Escrita**: só pela escolha no menu (`SetLanguage`). Salvar tema/filtro/seleção preserva o valor
  atual (inclusive `null`).
- **Compatibilidade**: preferências antigas com `"language": "pt-BR"` continuam em português.

## Idioma detectado (calculado, não salvo)

`Pick(lista do navegador)`:

| Lista do navegador | Resultado |
|---|---|
| `["en-US", "pt-BR"]` | `en` |
| `["pt-PT"]` | `pt-BR` |
| `["es", "pt-BR", "en"]` | `pt-BR` |
| `["fr", "ja"]` | `en` |
| `[]` / indisponível | `en` |
| `["EN-gb"]` | `en` (sem diferenciar maiúsculas) |

## Catálogo de textos

| Idioma | Arquivo | `Language.Name` |
|---|---|---|
| `pt-BR` (padrão) | `strings.pt-BR.json` | Português (Brasil) |
| `en` | `strings.en.json` | English |

Regra: as duas listas têm exatamente as mesmas chaves e os mesmos marcadores `{n}` por chave.

## Nomes de locais (`reference-data.en.json`)

- Mapa `texto pt (como em reference-data.json) → texto en`.
- Cobre toda chave de `trainersByLocation` e todo `wildLocation` não vazio.
- Exibição: idioma `en` → tradução; `pt-BR` ou sem tradução → texto original.
- Busca: cada adversário é indexado pelo texto pt e pelo texto en.
