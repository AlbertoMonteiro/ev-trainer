# Research: Sincronização com Gist privado

Sem itens "NEEDS CLARIFICATION": as duas dúvidas da spec foram respondidas (Q1 = A exclusões
propagam; Q2 = A sincronização automática). Fatos da API do GitHub verificados na documentação oficial
em 2026-10-11 (links no fim).

## R1. Acesso à API do GitHub direto do browser

- **Decision**: o app chama `https://api.github.com` com o `HttpClient` do Blazor (que usa `fetch`),
  com os cabeçalhos `Authorization: Bearer <token>`, `Accept: application/vnd.github+json` e
  `X-GitHub-Api-Version: 2022-11-28`. Sem JS novo para a API, sem pacote NuGet novo.
- **Rationale**: a REST API do GitHub aceita CORS de qualquer origem, com `Authorization` entre os
  cabeçalhos permitidos e `ETag`, `x-ratelimit-*` e `X-OAuth-Scopes` expostos. Constituição: chamar a
  API direto do .NET é a forma preferida (Restrições Técnicas) e não há proxy (Princípio I).
- **Alternatives considered**: OAuth com redirecionamento — o endpoint de troca de token do GitHub
  não aceita CORS e exigiria um servidor do app (vedado); device flow — mesmo problema.

## R2. Token e permissões mínimas

- **Decision**: o passo a passo recomenda um **token fine-grained** com só a permissão de conta
  **"Gists: Read and write"**, sem acesso a repositórios e com expiração escolhida pelo jogador; aceita
  também token clássico com só o escopo `gist`. Validação ao conectar: `GET /user` (conta) e, em
  seguida, localizar/criar o Gist — um 401 vira "token inválido ou expirado"; 403/404 nas chamadas de
  gist vira "token sem permissão de gists". Tokens clássicos com escopos além de `gist` (cabeçalho
  `X-OAuth-Scopes`) geram um aviso recomendando um token mais restrito, sem bloquear.
- **Rationale**: a permissão "Gists" de tokens fine-grained cobre criar e atualizar gists; é o menor
  escopo possível (Princípio V).

## R3. Onde ficam os dados no GitHub

- **Decision**: um Gist **secreto** (`public: false`) com descrição `EVs Trainer` e um único arquivo
  `evs-trainer.json` no formato do backup. Na conexão, o app lista os gists da conta (`GET /gists`,
  paginado) e usa o mais antigo que tiver esse arquivo; se não houver, cria (`POST /gists`). Escrita com
  `PATCH /gists/{id}` mudando só esse arquivo. O id do Gist fica guardado no aparelho.
- **Rationale**: um arquivo legível, importável à mão (FR-005); o tamanho típico (dezenas de KB) fica
  muito abaixo do limite de 1 MB por arquivo na resposta da API (acima disso o conteúdo vem truncado —
  tratado como erro "conteúdo grande demais", improvável).

## R4. Regra de combinação (merge)

- **Decision**: função pura `SyncMerge.Merge(local, remoto, agora)` no domínio, por id de Pokémon:
  - cada lado oferece, por id, uma versão (`updatedAt`, ausente = mínimo) e/ou uma exclusão
    (`deletedAt`);
  - vence o **evento mais recente** entre as versões e exclusões dos dois lados; exclusão vencedora ⇒
    o Pokémon não existe no resultado e a exclusão (mais recente) é mantida; versão vencedora ⇒ ela
    entra e exclusões mais antigas do mesmo id são descartadas;
  - empate exato de horário entre duas versões diferentes ⇒ desempate determinístico pelo conteúdo
    serializado (os dois aparelhos chegam ao mesmo resultado);
  - exclusões com mais de **90 dias** são descartadas (FR-008);
  - o resultado traz contagens para o aviso (atualizados, adicionados e removidos a partir do remoto) e
    se o remoto precisa ser regravado.
- **Rationale**: "vence o mais recente por item, com exclusões registradas" converge em qualquer ordem
  de sincronização (comutativa, idempotente), o que torna a concorrência segura (R6).
- **Alternatives considered**: merge por batalha/anotação — mais fino, mas a spec pede regra por
  Pokémon e o caso real (um jogador, dois aparelhos) raramente edita o mesmo Pokémon nos dois.

## R5. Carimbo "alterado em"

- **Decision**: `TrainingPokemon.UpdatedAt` (`DateTimeOffset?`, JSON `updatedAt` em ISO 8601 UTC,
  opcional). O `TrainerStore` carimba com `TimeProvider.GetUtcNow()` todo Pokémon mudado por um comando
  (batalha, desfazer, ajustes, níveis, importação de backup, adicionar a partir da captura…), num único
  ponto (`Commit(changedId)`); seleção, filtro e tema não carimbam. Exclusões viram registro
  `deleted: { id: data }` no documento; desfazer uma exclusão remove o registro e carimba o Pokémon.
- **Rationale**: um só ponto evita esquecer um comando; arquivos antigos sem `updatedAt` continuam
  válidos (perdem para qualquer versão datada — spec, Assumptions).

## R6. Ciclo de sincronização e concorrência

- **Decision**: `SyncService` executa um ciclo de cada vez (fila serial):
  1. lê o Gist (`GET` com `If-None-Match` do último ETag — 304 não gasta limite);
  2. de forma **síncrona**, combina com o estado local **atual** (`SyncMerge`), aplica no
     `TrainerStore` o que mudou e mostra o aviso;
  3. se o remoto precisa mudar, relê com `If-None-Match` logo antes do `PATCH`; se mudou nesse meio,
     volta ao passo 2; senão grava.
  Mudanças feitas durante a escrita ficam com `updatedAt` mais novo e disparam outro ciclo.
- **Rationale**: o `PATCH` de gist não aceita escrita condicional; a releitura imediata reduz a janela
  de corrida a milissegundos e, como cada aparelho mantém suas mudanças com data, uma eventual escrita
  perdida volta no próximo ciclo daquele aparelho (auto-correção). O GitHub recomenda chamadas em
  série.

## R7. Gatilhos automáticos (Q2 = A)

- **Decision**: ciclo ao iniciar o app (se conectado), quando a página volta a ficar visível e 5 s
  depois da última gravação local (debounce com `TimeProvider`), além de **Sincronizar agora**. Sem
  internet ou com erro: estado "pendente"/"erro" e nova tentativa no próximo gatilho. "Voltar a ficar
  visível" precisa de JS: uma função pontual em `interop.js` (`evt.onVisible(ref)`) chama o .NET via
  `DotNetObjectReference` (justificado: o browser não expõe `visibilitychange` ao .NET).
- **Rationale**: cobre trocar de aparelho sem toques (SC-007) com poucas requisições (várias batalhas
  seguidas = um ciclo).

## R8. Guarda da credencial

- **Decision**: chave própria no `localStorage`, `evtrainer:v1:sync` = `{ provider, token, login,
  gistId, lastSyncedAt, etag }`, lida e gravada por `IPokemonRepository`; nunca entra no backup nem no
  conteúdo do Gist; o campo do token é `type=password`, e após salvo a tela mostra só a conta;
  mensagens de erro são montadas por códigos (nunca repassam texto de exceção que poderia conter
  cabeçalhos). **Desconectar** apaga a chave.
- **Rationale**: Princípio V (credenciais só no aparelho, nunca em backup/URL/logs); SC-005 vira teste.

## R9. Interface de provedor

- **Decision**: `ISyncProvider` (`Connect(token)`, `Read(etag)`, `Write(document)`) no `State/`, com
  `GistSyncProvider` como primeira implementação; `SyncService` e `SyncMerge` não conhecem o GitHub.
- **Rationale**: Princípio V — o Google Drive entra depois como outro `ISyncProvider`.

## Fontes

- [REST API endpoints for gists](https://docs.github.com/en/rest/gists/gists)
- [Using CORS and JSONP to make cross-origin requests](https://docs.github.com/en/rest/using-the-rest-api/using-cors-and-jsonp-to-make-cross-origin-requests)
- [Permissions required for fine-grained personal access tokens](https://docs.github.com/en/rest/authentication/permissions-required-for-fine-grained-personal-access-tokens)
- [Best practices for using the REST API](https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api)
