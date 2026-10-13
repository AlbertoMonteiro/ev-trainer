# Contract: página "Onde treinar"

## Acesso
- Menu: `button.guide[role=menuitem]` → `L["Menu.Guide"]`.
- Aba EVs: `button.open-guide` (link discreto acima das listas) → abre a página.
- `Home` mostra `TrainingGuideView` no lugar das abas (como `SyncView`); `button.close-guide` volta.

## Controles
- Atributo: `.guide-stat` segmentado, `button[data-stat=0|1|3|5]` (HP, ATK, SPA, SPE), `aria-pressed`.
- Fase: `.guide-phase` segmentado, `button[data-phase=before|after]`, `aria-pressed`; mudar grava a preferência.

## Lista
- `ol.guide-list > li.guide-entry[data-trainer=<id>]`, na ordem do ranking.
- Cabeçalho: `.who` = `Guide.Trainer` ("{classe} {nome}"), `.where` = local no idioma ativo, marcas `.double`,
  `.access-*`.
- `ol.stages > li.stage[data-index]` em ordem; `.stage.best` no melhor estágio; cada estágio: `.label`
  (`Guide.Stage.N`), `.team` ("Magneton 28 · Magneton 28 · …"), `.evs` com o atributo escolhido em `.main` e os outros
  em `.also` (`Guide.Also`); `.requires-e4` nos estágios 4–5.
- Aviso `.waste` (`Guide.Waste`) quando o melhor estágio dá EV em atributo com meta atingida.
- Vazio: `.empty` com `Guide.Empty`.

## Notas fixas
- `.guide-tips`: multiplicadores (Macho Brace ×2, Pokérus + Macho Brace ×4, Exp. Share), VS Seeker (~100 passos, só
  quem está na tela), Ilhas Sevii não incluídas.
- `.guide-sources`: link para o repositório da decompilação e créditos.

## Textos novos (pt-BR / en) — principais
`Menu.Guide` (Onde treinar / Where to train), `Guide.Title`, `Guide.Stat.Label`, `Guide.Phase.Label`,
`Guide.Phase.Before` (Antes da Elite Four / Before the Elite Four), `Guide.Phase.After`, `Guide.Stage.0..5`
(Original / Revanche / Após Celadon / Após Fuchsia / Após a Elite Four / Após as Ilhas Sevii), `Guide.Double`,
`Guide.Access.surf|cyclingRoad|victoryRoad`, `Guide.Also`, `Guide.Waste`, `Guide.Empty`, `Guide.Tips.*`,
`Guide.Sources`, `Guide.Close`, `Guide.Open`.
