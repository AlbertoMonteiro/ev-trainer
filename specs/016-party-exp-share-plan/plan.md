# Implementation Plan: Equipe atual e plano de batalhas com Exp. Share

**Branch**: `016-party-exp-share-plan` | **Date**: 2026-10-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/016-party-exp-share-plan/spec.md`

## Summary

A feature tem três partes, todas sem dados novos de referência.

**Equipe atual**

- `TrainingPokemon` ganha `Party` (`bool?`). `null` significa dado antigo, sem a informação.
- O domínio `Party` normaliza a equipe: no máximo 6, e os dados antigos são preenchidos pelos alterados mais
  recentemente (research R1). A normalização roda ao carregar, importar e sincronizar, como o
  `KeepOneExpShareHolder` da spec 012.
- O JSON do Pokémon ganha `party`, que entra no backup e na sincronização sem mudar a regra de merge.
- O `TrainerStore` ganha:
  - `PartyOrdered` e `Stored`;
  - `JoinParty`, `LeaveParty`, `SwapParty` e `MoveInParty`.
- `AddPokemon`/`AddFromCapture` põem o novo na equipe se houver vaga.
- O Exp. Share só pode ir para quem está na equipe.
- O menu e o painel lateral listam só a equipe. Um Pokémon aberto fora da equipe aparece separado.
- Uma página nova, `PartyView`, gerencia a equipe e os guardados.

**Plano de batalhas com Exp. Share**

- O domínio `ExpSharePlanner` é puro. Ele recebe:
  - para cada Pokémon, os EVs atuais, as metas e o Pokérus;
  - o rendimento por batalha do adversário.
- Ele procura a melhor combinação das 4 configurações (R2). As configurações 3 e 4 (cada um sozinho) são resolvidas por
  conta direta, então a busca só percorre as configurações com Exp. Share. São até 256², bem abaixo de 1 s.
- O resultado traz as fases, os EVs finais, o restante e a comparação com treinar cada um sozinho.
- A página `ExpSharePlanView` escolhe os dois Pokémon da equipe e o adversário (treinador e revanche, ou selvagem).
- Ela mostra o plano e aplica uma fase com `TrainerStore.ApplyConfiguration`, que tem Desfazer.

**Busca por treinador**

- O `OpponentSearch` passa a indexar os treinadores do guia (`VsSeekerGuide`) por classe, nome e "classe nome".
- `FindTrainers` devolve os cartões. `Find` passa a incluir os Pokémon dos times encontrados.
- O `TrainerCard` mostra as revanches e os EVs por batalha, com dois botões:
  - "Registrar batalha inteira", com `TrainerStore.RecordTeam` e um Desfazer só;
  - "Planejar com Exp. Share", que abre o plano já preenchido. Ele não aparece em batalhas duplas.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**:

- Novo campo `party` no documento de cada Pokémon. Vale no aparelho, no backup e no Gist.
- Ausente é lido como `null`, ou seja, dado antigo.
- Nenhuma chave nova no `localStorage`.

**Testing**: xUnit v3 e bUnit.

- `Party`: normalização, migração, limite após a sincronização.
- `ExpSharePlanner`: o exemplo da spec, o Pokérus e uma tabela de casos conferida contra força bruta em 4 dimensões.
- Busca: por treinador e pela classe.
- `RecordTeam` e `ApplyConfiguration`: incluindo o Desfazer.
- JSON: o campo `party`.
- bUnit: menu, painel lateral, `PartyView`, `ExpSharePlanView`, `TrainerCard` na aba de EVs, textos.

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**:

- O plano sai em ≤ 256 × 256 iterações de vetores de 6 posições, instantâneo no WebAssembly.
- A busca por treinador percorre 221 treinadores pré-normalizados.

**Constraints**:

- Offline.
- Textos via recursos (pt/en).
- A sincronização nunca perde dados.
- Um Pokémon fora da equipe nunca perde dados.

**Scale/Scope**:

- 2 classes de domínio novas (`Party`, `ExpSharePlanner`).
- Mudanças no JSON, no `TrainerStore`, no `OpponentSearch` e em `BattleLog` (só reuso).
- 3 componentes novos (`PartyView`, `ExpSharePlanView`, `TrainerCard`) e ajustes no menu, no painel lateral, na
  `EvTab`, no `EvSummary` e na `Home`.
- Cerca de 45 chaves de texto por idioma.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Nada de servidor; usa só os dados já embutidos (guia, referência). | ✅ | ✅ |
| II. Dois projetos | Nada novo. Regras no domínio puro (`Party`, `ExpSharePlanner`, `OpponentSearch`). Os componentes só exibem. | ✅ | ✅ |
| III. Test-First | Testes de domínio, JSON, store e bUnit antes do código. O otimizador é conferido contra uma busca completa. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Ganhos com as regras da spec 012 (`Evs.Multiplier`, `BattleLog`). O registro da batalha inteira reusa `RecordBattle`, sem nova conta de EV. Totais continuam derivados dos eventos. | ✅ | ✅ |
| V. Dados locais / sync | `party` é um campo do Pokémon. O merge por Pokémon (mais recente vence) não muda, e a equipe é normalizada depois do merge. Backups antigos são lidos sem perda. Aplicar configuração e registrar o time têm Desfazer. | ✅ | ✅ |
| VI. Mobile-first, acessível | Páginas em coluna única no celular. Botões com rótulos e `aria-pressed`. Equipe reordenável com botões ↑/↓, sem arrastar. | ✅ | ✅ |
| VII. Outros idiomas | Textos via recursos. Classes e nomes de treinador ficam como no jogo, em inglês, como no guia. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/016-party-exp-share-plan/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── party.md
│   ├── exp-share-plan.md
│   └── trainer-search.md
├── checklists/
│   └── requirements.md
└── tasks.md            # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/
│   ├── Party.cs                    # novo: normalização da equipe (máx. 6, migração)
│   ├── ExpSharePlanner.cs          # novo: configurações, busca do plano, comparação
│   ├── OpponentSearch.cs           # + treinadores do guia (FindTrainers, Pokémon dos times em Find)
│   ├── Model/TrainingPokemon.cs    # + Party (bool?)
│   └── Json/PokemonJson.cs         # + "party"
├── State/TrainerStore.cs           # equipe, RecordTeam, ApplyConfiguration, Exp. Share só na equipe
├── Components/
│   ├── Party/PartyView.razor       # novo
│   ├── Plan/ExpSharePlanView.razor # novo (+ PlanText.cs se precisar)
│   ├── Evs/TrainerCard.razor       # novo; EvTab mostra os cartões na busca
│   ├── Evs/EvSummary.razor         # Exp. Share só com a equipe; link "Planejar batalhas"
│   └── Shell/ (PokemonList, PokemonMenu, PokemonSidebar, AppHeader)
├── Pages/Home.razor                # novas páginas _party e _expPlan
├── Program.cs                      # ExpSharePlanner; OpponentSearch com VsSeekerGuide
├── Localization/strings.*.json
└── wwwroot/css/app.css
tests/EvTrainer.Tests/ (Domain, State, Components/Party, Components/Plan, Components/Evs, Localization)
```

**Structure Decision**: mesma organização das features 012–015.

## Complexity Tracking

Sem violações.
