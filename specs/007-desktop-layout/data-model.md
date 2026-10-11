# Data Model: Layout para desktop

Esta feature **não altera dados**: nenhum campo novo no Pokémon, nas preferências, nas notas de captura
ou no backup. O estado de tela que precisa sobreviver ao redimensionar (aba atual, Pokémon selecionado,
avaliação de captura aberta, campos digitados) já vive em componentes e no `TrainerStore`, que não são
desmontados ao mudar a largura (research R2).

## Estado de UI envolvido (existente)

| Estado | Onde vive | Efeito do painel lateral |
|---|---|---|
| Pokémon selecionado | `UiPreferences.SelectedId` (`TrainerStore.Select`) | o painel muda a seleção com 1 clique |
| Aba atual | `Home._tab` | preservada ao trocar de Pokémon |
| Avaliação de captura aberta | `Home._capturing` | fechada ao escolher um Pokémon no painel |
