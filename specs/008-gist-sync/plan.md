# Implementation Plan: Sincronização com Gist privado

**Branch**: `008-gist-sync` | **Date**: 2026-10-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-gist-sync/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Primeiro provedor de armazenamento externo (constituição v1.3.0): o jogador conecta um token do GitHub
só com permissão de gists e o app mantém um Gist secreto `evs-trainer.json`, no formato do backup,
sincronizado automaticamente entre aparelhos — ao abrir, ao voltar à página e 5 s depois de mudanças,
além de "Sincronizar agora". A combinação é por Pokémon (vence o mais recente, com exclusões
registradas por 90 dias), feita por uma função pura do domínio; o app fala direto com a API do GitHub
pelo `HttpClient`, sem servidor, e o token fica só no aparelho.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova — `HttpClient` / `System.Net.Http.Json` do próprio runtime;
GitHub REST API (`api.github.com`, versão `2022-11-28`) chamada do browser (CORS verificado, research R1).

**Storage**: `localStorage` — documento de dados ganha `updatedAt` por Pokémon e `deleted`; nova chave
`evtrainer:v1:sync` com a conexão (token só no aparelho). Remoto: um Gist secreto do usuário.

**Testing**: xUnit v3 + bUnit; provedor testado com `HttpMessageHandler` falso (sem rede nos testes);
serviço com provedor falso e `ManualTimeProvider` (debounce de 5 s).

**Target Platform / Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: sincronização típica (≤ 20 Pokémon) < 3 s em rede móvel (SC-006); várias
mudanças seguidas agrupadas num ciclo; leituras condicionais (304) sem gastar limite.

**Constraints**: offline-first (nenhuma função depende da rede além da própria sincronização);
nunca sobrescrever dado mais novo nem perder dado local (FR-007); token nunca em backup/URL/mensagens
(SC-005); chamadas ao GitHub em série; textos via recursos; 360 px e desktop.

**Scale/Scope**: domínio `SyncMerge` + carimbo `UpdatedAt` + exclusões; `ISyncProvider` +
`GistSyncProvider`; `SyncService`; 3 métodos no repositório; tela `SyncView`; status ampliado; 1 função
JS de visibilidade.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Browser → `api.github.com` direto, sem proxy; tudo funciona offline exceto a sincronização. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; merge e carimbo no `Domain/` (C# puro), HTTP e agenda no `State/`. | ✅ | ✅ |
| III. Test-First | Testes antes: tabela de casos do merge (contrato), carimbo por comando, JSON tolerante (`updatedAt`/`deleted`), provedor com HTTP falso (status → erros), serviço (gatilhos, debounce, concorrência, nunca perder local), tela e "token nunca aparece". | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Não muda regras; o documento continua sendo a lista de eventos, totais derivados. | ✅ | ✅ |
| V. Locais primeiro, externos por opção | Desligado por padrão; Gist secreto **na conta do usuário**; `ISyncProvider` (Drive depois); formato do backup; token só no aparelho, escopo mínimo ("Gists"), desconectar apaga; merge nunca sobrescreve mais novo; falha não perde local; status de sincronização; aviso de que Gist secreto não é privado. | ✅ | ✅ |
| VI. Mobile-first, bom no desktop | Tela nova responsiva; status no cabeçalho; nenhuma regressão de layout. | ✅ | ✅ |
| VII. Outros idiomas | Chaves `Sync.*`/`Menu.Sync`; mensagens de erro por código; teste de texto fixo cobre a tela. | ✅ | ✅ |
| Restrições: JS pontual | Uma função `evt.onVisible` (o .NET não recebe `visibilitychange`); API chamada do .NET, sem script de terceiros. | ✅ | ✅ |
| Restrições: pacotes | Nenhum pacote novo. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/008-gist-sync/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── sync.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/Model/TrainingPokemon.cs        # UpdatedAt
├── Domain/Json/PokemonJson.cs             # updatedAt, deleted (tolerante)
├── Domain/SyncMerge.cs                    # novo: SyncSnapshot, MergeResult, Merge
├── State/TrainerStore.cs                  # TimeProvider; carimbo em Commit(changedId); exclusões; Snapshot/ApplySync
├── State/IPokemonRepository.cs            # Load/Save/ClearSyncConnection
├── State/LocalStorageRepository.cs        # evtrainer:v1:sync
├── State/SyncConnection.cs                # novo: conexão e estado
├── State/ISyncProvider.cs                 # novo: interface + SyncException/SyncErrorKind
├── State/GistSyncProvider.cs              # novo: HTTP com o GitHub
├── State/SyncService.cs                   # novo: ciclo, gatilhos, debounce, fila serial
├── Components/Sync/SyncView.razor         # novo: conectar/desconectar/status
├── Components/Shell/StatusIndicator.razor # estado da sincronização
├── Components/Shell/PokemonMenu.razor     # item Sincronização
├── Pages/Home.razor                       # visão de sincronização
├── wwwroot/js/interop.js                  # evt.onVisible
├── Program.cs                             # HttpClient do GitHub, provedor, SyncService.Start()
└── Localization/strings.pt-BR.json        # Sync.*
tests/EvTrainer.Tests/
├── Domain/SyncMergeTests.cs               # novo (tabela do contrato)
├── Domain/ModelJsonTests.cs, BackupFormatTests.cs  # updatedAt/deleted, arquivos antigos
├── State/TrainerStoreStampTests.cs        # novo: carimbo e exclusões
├── State/GistSyncProviderTests.cs         # novo: HTTP falso
├── State/SyncServiceTests.cs              # novo: provedor falso + relógio manual
├── Components/Sync/SyncViewTests.cs       # novo
├── Localization/NoHardcodedTextTests.cs
└── Support/FakeSyncProvider.cs, FakeHttpHandler.cs, InMemoryRepository.cs
```

**Structure Decision**: mesma solução de 2 projetos; regra de combinação no domínio, rede e agenda no
`State/`, tela fina.

## Complexity Tracking

Sem violações da constituição a justificar.
