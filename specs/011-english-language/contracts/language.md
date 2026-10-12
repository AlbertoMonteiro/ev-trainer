# Contract: idioma da interface

Verificado por testes de domínio, bUnit e pelo validação manual do quickstart.

## Escolha na inicialização

```text
idioma = prefs.language ∈ catálogo ? prefs.language : LanguageMatcher.Pick(IBrowserApi.Languages())
LanguageState.Current = idioma
IBrowserApi.SetLanguage(idioma)        # document.documentElement.lang
```

`LanguageMatcher.Pick`: ver tabela em [data-model.md](../data-model.md).

## JS (`interop.js`)

| Função | Retorno |
|---|---|
| `evt.languages()` | `navigator.languages` (array de strings); se ausente, `[navigator.language]`; se nada, `[]` |
| `evt.setLanguage(lang)` | inalterada: define `lang` no `<html>` |

Script inline do `index.html` (antes do Blazor): mesma regra de escolha; define `lang` no `<html>` e
os textos de `#blazor-error-ui` (mensagem e "Recarregar"/"Reload").

## Menu (`PokemonMenu`)

- Seção de idioma visível (2 idiomas): `button[data-lang=pt-BR]` "Português (Brasil)",
  `button[data-lang=en]` "English"; `aria-pressed="true"` no ativo.
- Clique: grava `prefs.language`, aplica na hora (sem recarregar), atualiza `lang`, fecha o menu.
  Aba ativa, Pokémon selecionado, texto da busca e editor aberto permanecem.

## Locais nos cartões (`OpponentCard`)

- `Evs.Card.Trainers` recebe os locais do `SearchHit` traduzidos para o idioma ativo
  (`LocationNames.Display`).

## Busca (`OpponentSearch`)

- Casa por nome/número, local selvagem (pt ou en) ou local de treinador (pt ou en).
- `RouteExpander`: `rotas? <lista>` e `routes? <lista>` expandem para `rota N`/`route N` por item.
- Exemplos (app em inglês): "route 3" ≡ "rota 3"; "pewter gym" casa "Ginásio de Pewter (Brock)";
  "routes 12–15" ≡ "rotas 12–15".

## Textos

- `strings.en.json`: todas as chaves de `strings.pt-BR.json`, mesmos marcadores.
- Interface em inglês: nenhum texto fixo (teste `NoHardcodedText` em `en`) e nenhuma palavra da lista
  de controle em português nos textos renderizados (ex.: "Rota", "Ginásio", "Ataque", "Buscar",
  "Editar", "Salvar", "Cancelar", "Úteis").
