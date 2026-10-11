# Contract: regiões de layout por largura

O markup define **regiões** (classes) na ordem lógica do celular; o CSS as posiciona por largura.
Nenhum componente renderiza conteúdo diferente conforme a largura (research R2).

## Pontos de quebra

| Largura | Nome | Mudança |
|---|---|---|
| < 1024 px | celular | igual a hoje (coluna ≤ 560 px, alvos de 44 px) |
| ≥ 1024 px | desktop | conteúdo até 1440 px; regiões lado a lado; toast no canto inferior direito |
| ≥ 1280 px | desktop com painel | `aside.pokemon-sidebar` visível (240 px); lista do menu ☰ oculta |

## Estrutura da página

```text
#app
├── header.app-header                     (largura total)
└── .app-body                             (≥1280: grid 240px | 1fr)
    ├── aside.pokemon-sidebar             (oculto < 1280)
    │   ├── h2 (Sidebar.Title)
    │   ├── PokemonList                   (mesma marcação do menu)
    │   └── button.add (Menu.AddPokemon)
    └── main.page
```

## Regiões por tela (ordem do DOM = ordem no celular)

| Tela | Contêiner | Filhos, em ordem | Desktop (≥ 1024) |
|---|---|---|---|
| EVs | `.ev-layout` | `.ev-summary-area` (EvSummary), busca, `.recent-list`, `.main-list`, histórico | `340px 1fr`; resumo à esquerda, `sticky`; demais à direita; grade de cards `auto-fill, minmax(160px, 1fr)` |
| IVs | `.iv-layout` | IvTable, KnownIvsPanel, LevelForm | tabela + IVs conhecidos à esquerda, formulário à direita |
| Ajustes | `.settings-layout` | SettingsForm, BackupPanel, SpreadsheetImportPanel, DeletePanel | formulário à esquerda, demais à direita |
| Avaliar captura | `.capture-view` > `.capture-columns` | `.capture-inputs` (campos, ações), `.capture-output` (resultado, melhor até agora) | duas colunas; cabeçalho e dica ocupam ambas |

## Painel lateral

| Elemento | Comportamento |
|---|---|
| `aside.pokemon-sidebar[aria-label=Sidebar.Title]` | sempre renderizado; `display: none` < 1280 px |
| item `button.pokemon-item[data-id]` | sprite, forma atual, `Menu.Total`; `aria-current="true"` no selecionado |
| clique num item | `TrainerStore.Select(id)`; mantém a aba; fecha Avaliar captura se aberta |
| `button.add` | igual a "+ Adicionar Pokémon" do menu (cria e abre Ajustes) |
| sem Pokémon | só o botão de adicionar |

## Chaves de idioma novas

`Sidebar.Title` ("Seus Pokémon").
