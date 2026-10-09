# Contract: localização (pt-BR agora, inglês depois)

Atende o Princípio VII da constituição, FR-035 e SC-010. Decisão técnica em
[research.md R14](../research.md#r14-localização-pt-br-agora-inglês-depois).

## Arquivos de idioma

- Local: `src/EvTrainer.Web/Localization/strings.<tag>.json`, embutidos no assembly
  (`EmbeddedResource`, `LogicalName="EvTrainer.strings.<tag>.json"`).
- Nesta versão: apenas `strings.pt-BR.json` (idioma padrão e de fallback).
- Formato: objeto plano chave → texto. Argumentos posicionais `{0}`, `{1}` (formatados com
  `CultureInfo.InvariantCulture`; o app só exibe inteiros).

```json
{
  "App.Title": "EVs Trainer",
  "Tabs.Evs": "EVs",
  "Tabs.Ivs": "IVs",
  "Tabs.Settings": "Ajustes",
  "Stat.Short.0": "HP",
  "Stat.Long.1": "Ataque",
  "Toast.EvGain": "{0}: {1}",
  "Toast.NoEvGain": "{0}: não deu EV (limite atingido)",
  "Status.Saved": "Salvo neste aparelho",
  "Backup.ImportPreview": "{0} Pokémon: {1} novos, {2} substituídos"
}
```

## Convenção de chaves

`Área.Elemento[.Variação]`, em inglês e PascalCase: `Menu.AddPokemon`, `Evs.Filter.Useful`,
`Ivs.Cell.Inconsistent`, `Settings.Targets.OverLimit`, `Empty.NoUseful`… Uma chave por texto
visível (inclusive `aria-label`, `title` e placeholders).

## API usada pelos componentes

```csharp
// Microsoft.Extensions.Localization.Abstractions (pacote oficial)
@inject IStringLocalizer<App> L
<h1>@L["App.Title"]</h1>
<span>@L["Toast.NoEvGain", opponentName]</span>

// Implementação própria
sealed class JsonStringLocalizer : IStringLocalizer      // lê o idioma ativo do LanguageState
sealed class LanguageState                               // Current, Available, Changed; persiste em prefs
```

Regras:

- Busca: idioma ativo → `pt-BR` → a própria chave entre colchetes (`[Chave]`), que torna a
  ausência visível e é detectada pelos testes.
- Trocar o idioma atualiza `LanguageState.Current`, grava em `evtrainer:v1:prefs`, ajusta
  `<html lang>` via interop e re-renderiza, sem recarregar a página.
- `Available` é derivado dos recursos embutidos; com um só idioma o seletor do menu fica oculto.

## Testes obrigatórios

| Teste | Garante |
|---|---|
| Paridade de chaves | Todo `strings.*.json` tem exatamente as chaves do `pt-BR` e os mesmos placeholders `{n}`. |
| Chaves usadas existem | Toda chave literal em `L["..."]` nos `.razor`/`.cs` existe no `pt-BR`. |
| Sem texto fixo | Renderizar cada tela com um idioma de teste sintético (todas as chaves → `«chave»`) não deixa texto visível fora desse padrão, exceto dados (nomes de Pokémon, números, locais). |
| Troca em tempo real | Mudar `LanguageState.Current` re-renderiza as telas com o novo idioma. |

## Para adicionar inglês (feature futura)

1. Criar `strings.en.json` com todas as chaves (o teste de paridade aponta o que falta).
2. Adicionar `reference-data.en.json` com nomes de locais traduzidos e regra de busca de rotas em
   inglês ("route 4").
3. Nenhuma alteração em componentes ou domínio.
