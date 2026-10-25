# Feature Specification: Equipe atual e plano de batalhas com Exp. Share

**Feature Branch**: `016-party-exp-share-plan`

**Created**: 2026-10-25

**Status**: Draft

**Input**: User description: "A listagem de pokemons fica muito grande, o ideal é que ficasse só 6, para ficar coerente
com a equipe montada, então teria que ter outra tela parar poder editar a equipe atual, além disso, quando você está
treinando com ex share, seria bom que a aplicação ajudasse a como otimizar a luta, por exemplo: Eu estava lutando contra
o Engineer Bernie da Rota 11 com os 3 magneton, usando um Electabuzz como primeiro segurando macho brace, e o Joelton
como ex share, se o aplicativo me disse quantas batalhas eu deva fazer contra o Bernie sendo 1. X batalhas com
Electabuzz com macho brace na frente + Joelton como ex share 2. X batalhas com Joelton com macho brace na frente +
Electabuzz como ex share. Assim eu otimizaria quantas batalhas tenho que fazer"

**Contexto**:

- **A lista cresce sem parar.** Hoje o menu ☰ e o painel lateral do desktop mostram **todos** os Pokémon já
  cadastrados. Com o tempo, a lista fica longa e não corresponde ao que o jogador carrega no jogo, que tem no máximo
  **6 Pokémon na equipe**.
- **As regras de EV com Exp. Share na Gen 3** (spec 012):
  - O Pokémon que lutou ganha o rendimento de EV do adversário ×2 se segurar o **Macho Brace** e ×2 se tiver
    **Pokérus**.
  - O Pokémon que segura o **Exp. Share** ganha o rendimento **inteiro**, ×2 só pelo Pokérus dele.
  - Num treinador com vários Pokémon, esses ganhos valem para cada Pokémon dele que é derrotado.
- **Revezar o Macho Brace encurta o treino.** Quando dois Pokémon precisam dos mesmos EVs, alternar quem vai na frente
  com o Macho Brace e quem fica com o Exp. Share leva às metas dos dois com menos batalhas.
- **O exemplo do pedido**: o Engineer Bernie da Rota 11, na revanche com 3 Magneton, dá 6 Sp. Atk por batalha.
  - Na frente com Macho Brace, o Pokémon ganha 12.
  - Com o Exp. Share, o outro ganha 6.

## Clarifications

### Session 2026-10-25

- Q: Quando o jogador digita o nome de um treinador na busca (ex.: "Bernie" ou "Engineer Bernie"), como o resultado deve aparecer? → A: Um cartão do treinador com o time de cada revanche, os EVs por batalha e os botões "Registrar batalha inteira" e "Planejar com Exp. Share"; os Pokémon do time também aparecem na grade, como hoje, para registro individual.
- Q: A busca deve encontrar só os treinadores que dá para enfrentar de novo com o VS Seeker, ou todos os treinadores do jogo? → A: Só os treinadores com revanche pelo VS Seeker (os do guia de treino, spec 013).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lista só com a equipe atual (Priority: P1)

O jogador tem 15 Pokémon cadastrados no app, mas só 6 na equipe do jogo. O menu ☰ e o painel lateral passam a mostrar
só esses 6. Os outros continuam salvos e acessíveis numa tela própria.

**Why this priority**: é o incômodo do dia a dia citado primeiro. A lista longa atrapalha trocar de Pokémon a cada
treino.

**Independent Test**: com mais de 6 Pokémon cadastrados, definir uma equipe de 6. Conferir que o menu e o painel
lateral mostram só eles, e que os demais continuam com os dados intactos.

**Acceptance Scenarios**:

1. **Given** 15 Pokémon cadastrados e 6 deles na equipe, **When** o jogador abre o menu ☰ (ou vê o painel lateral no
   desktop), **Then** aparecem só os 6 da equipe, na ordem da equipe.
2. **Given** o menu ☰ aberto, **When** o jogador procura como ver os demais, **Then** há um item "Equipe e Pokémon
   guardados" (ou equivalente) que abre a tela da equipe.
3. **Given** dados antigos sem informação de equipe e com até 6 Pokémon, **When** o app abre, **Then** todos ficam na
   equipe e nada muda para o jogador.
4. **Given** dados antigos sem informação de equipe e com mais de 6 Pokémon, **When** o app abre, **Then** a equipe
   tem os 6 alterados mais recentemente, os demais ficam guardados e nenhum dado se perde.

---

### User Story 2 - Editar a equipe atual (Priority: P1)

Numa tela própria, o jogador vê a equipe (até 6) e os Pokémon guardados. Ele pode:

- colocar um Pokémon guardado na equipe;
- tirar um da equipe;
- trocar um da equipe por um guardado;
- abrir qualquer um deles para ver ou registrar EVs.

**Why this priority**: sem essa tela, limitar a lista a 6 deixaria Pokémon inacessíveis.

**Independent Test**: na tela da equipe, tirar um Pokémon, colocar outro e conferir que o menu reflete a mudança.

**Acceptance Scenarios**:

1. **Given** a equipe com 5 Pokémon, **When** o jogador coloca um guardado na equipe, **Then** ele passa a aparecer no
   menu.
2. **Given** a equipe com 6 Pokémon, **When** o jogador tenta colocar mais um, **Then** o app pede para escolher quem
   sai da equipe e não permite 7.
3. **Given** um Pokémon na equipe, **When** o jogador o tira da equipe, **Then** ele vai para os guardados com todos os
   EVs, IVs e histórico intactos.
4. **Given** um Pokémon guardado, **When** o jogador o abre pela tela da equipe, **Then** ele vira o Pokémon
   selecionado e as abas de EVs/IVs/Config funcionam normalmente.
5. **Given** a equipe com menos de 6, **When** o jogador cadastra um novo Pokémon, **Then** ele entra na equipe.
6. **Given** a equipe cheia, **When** o jogador cadastra um novo Pokémon, **Then** ele não tira ninguém da equipe:
   fica guardado e selecionado, e um aviso diz que a equipe está cheia, com acesso à tela da equipe para trocar.
7. **Given** o jogador muda a ordem da equipe, **When** abre o menu, **Then** a ordem é a mesma.

---

### User Story 3 - Quantas batalhas fazer com cada um na frente (Priority: P1)

O jogador vai treinar o Electabuzz e o Jolteon contra o Engineer Bernie (revanche com 3 Magneton). Ele abre o plano
de batalhas com Exp. Share, escolhe o treinador e a revanche, e os dois Pokémon. O app responde quantas batalhas fazer
em cada configuração, por exemplo:

- **8 batalhas** com Electabuzz na frente (Macho Brace) e Jolteon com o Exp. Share;
- **4 batalhas** com Jolteon na frente (Macho Brace) e Electabuzz com o Exp. Share.

Com isso, as metas de Sp. Atk dos dois são atingidas no menor número de batalhas.

**Why this priority**: é o ganho prático do pedido: menos batalhas repetidas e nenhum EV desperdiçado.

**Independent Test**: com dois Pokémon, metas e EVs conhecidos, pedir o plano contra um treinador e conferir os números
contra a conta feita à mão.

**Acceptance Scenarios**:

1. **Given** os dados abaixo, **When** o jogador pede o plano, **Then** o app indica 8 batalhas com Electabuzz na frente
   e 4 com Jolteon na frente, num total de 12, e mostra que os dois terminam exatamente na meta.
   - Electabuzz: faltam 120 Sp. Atk.
   - Jolteon: faltam 96 Sp. Atk.
   - Nenhum dos dois tem Pokérus.
   - Adversário: Bernie com 3 Magneton (6 Sp. Atk por batalha).
2. **Given** um dos dois com Pokérus, **When** o jogador pede o plano, **Then** os ganhos de cada configuração
   consideram o Pokérus de cada um, conforme as regras da spec 012.
3. **Given** metas que nenhuma combinação atinge exatamente, **When** o jogador pede o plano, **Then** o app mostra:
   - o plano que chega **mais perto sem passar** da meta de nenhum dos dois;
   - quanto faltará para cada um no fim;
   - que esse restante deve ser completado com adversários que dão menos EVs (por exemplo, Pokémon selvagens).
4. **Given** um dos dois já sem espaço num atributo que o treinador dá, **When** o jogador pede o plano, **Then**:
   - o plano não o coloca com o Exp. Share nas batalhas em que ele passaria da meta;
   - o app pode indicar batalhas só com o outro na frente, sem ninguém com o Exp. Share.
5. **Given** um treinador cujo time dá EVs em mais de um atributo, **When** o jogador pede o plano, **Then** nenhum dos
   dois passa da meta em nenhum desses atributos.
6. **Given** o plano calculado, **When** o jogador vê o resultado, **Then** ele vê para cada configuração:
   - quem vai na frente com o Macho Brace e quem segura o Exp. Share;
   - o número de batalhas;
   - os EVs ganhos por batalha;
   - os EVs finais de cada um nos atributos envolvidos.
7. **Given** o plano calculado, **When** o jogador o compara com treinar cada um sozinho com o Macho Brace, **Then** o
   app mostra quantas batalhas o plano economiza.

---

### User Story 4 - Aplicar a configuração e acompanhar o plano (Priority: P2)

Ao começar uma fase do plano, o jogador toca em "Usar esta configuração". O app então:

- seleciona o Pokémon da frente;
- marca o Macho Brace nele (e tira do outro);
- põe o Exp. Share no outro.

Depois, cada batalha registrada conta para os dois, como já acontece hoje. Ao reabrir o plano, os números consideram os
EVs atuais, então mostram as batalhas que ainda faltam.

**Why this priority**: evita configurar à mão três coisas a cada troca de fase. Sem isso, o plano ainda é útil, mas dá
mais trabalho.

**Independent Test**: tocar em "Usar esta configuração" e conferir o Pokémon selecionado e as marcações. Registrar uma
batalha e reabrir o plano: há uma batalha a menos na fase.

**Acceptance Scenarios**:

1. **Given** um plano com a fase "Electabuzz na frente + Jolteon com Exp. Share", **When** o jogador toca em "Usar esta
   configuração", **Then**:
   - Electabuzz fica selecionado e com o Macho Brace marcado;
   - Jolteon fica com o Exp. Share e sem o Macho Brace.
2. **Given** essa configuração aplicada, **When** o jogador registra os 3 Magneton do Bernie e reabre o plano, **Then**
   o número de batalhas que faltam diminui de acordo.
3. **Given** um dos Pokémon do plano fora da equipe, **When** o jogador aplica a configuração, **Then** o app avisa que
   ele precisa estar na equipe (no jogo, quem segura o Exp. Share precisa estar na equipe) e oferece colocá-lo.

---

### User Story 5 - Registrar a batalha inteira do treinador (Priority: P3)

Com o plano aberto, o jogador registra "1 batalha contra o Bernie" com um toque. O app registra os 3 Magneton de uma
vez, com o compartilhamento do Exp. Share como hoje, e um único Desfazer desfaz a batalha toda.

**Why this priority**: conveniência. Hoje dá para registrar os Magneton um a um, ou "Magneton ×3", na aba de EVs.

**Independent Test**: tocar em registrar uma batalha do treinador e conferir os EVs dos dois Pokémon. Tocar em Desfazer
e ver os EVs voltarem.

**Acceptance Scenarios**:

1. **Given** a configuração da fase aplicada, **When** o jogador registra uma batalha do treinador, **Then** cada
   Pokémon do time é registrado e os dois Pokémon ganham os EVs previstos no plano.
2. **Given** essa batalha registrada, **When** o jogador toca em Desfazer no aviso, **Then** a batalha inteira é
   desfeita nos dois.

---

### User Story 6 - Buscar pelo treinador (Priority: P2)

Na busca de adversários da aba de EVs, o jogador digita "Bernie", "Engineer Bernie" ou só "Engineer". Aparece um
cartão para cada treinador encontrado. Os Pokémon dos times também aparecem na grade de adversários, como na busca por
local, para registro individual.

O cartão mostra:

- o nome, a classe e o local do treinador;
- o time de cada revanche (original, VS Seeker, após Celadon…), com os EVs por batalha;
- os botões "Registrar batalha inteira" e "Planejar com Exp. Share".

**Why this priority**: hoje só dá para achar o time de um treinador pelo local. Pelo nome, que é o que o jogador vê no
jogo, é mais rápido. O cartão também é a porta natural para registrar a batalha inteira (US5) e para o plano (US3).

**Independent Test**: digitar "Bernie" na busca e ver o cartão do Engineer Bernie com os dois times. Tocar em
"Registrar batalha inteira" no time de 3 Magneton e conferir +6 Sp. Atk (sem modificadores) no Pokémon selecionado.

**Acceptance Scenarios**:

1. **Given** a busca de adversários, **When** o jogador digita "bernie" (sem maiúsculas ou acentos), **Then** aparecem:
   - o cartão do Engineer Bernie (Rota 11), com o time original (Magnemite ×2 + Magneton, 4 Sp. Atk) e a revanche
     (Magneton ×3, 6 Sp. Atk);
   - Magnemite e Magneton na grade.
2. **Given** a busca "engineer", **When** os resultados aparecem, **Then** há um cartão para cada Engineer.
3. **Given** um cartão de treinador, **When** o jogador toca em "Registrar batalha inteira" num dos times, **Then**:
   - cada Pokémon do time é registrado para o Pokémon selecionado, com Macho Brace, Pokérus e Exp. Share como hoje;
   - um único Desfazer desfaz tudo.
4. **Given** um cartão de treinador, **When** o jogador toca em "Planejar com Exp. Share" num dos times, **Then** o
   plano abre com esse treinador e esse time já escolhidos.
5. **Given** um treinador de batalha dupla, **When** o cartão aparece, **Then**:
   - "Registrar batalha inteira" funciona normalmente;
   - "Planejar com Exp. Share" não é oferecido, com o motivo indicado.

### Edge Cases

- **Menos de 2 Pokémon na equipe**: o plano mostra as batalhas para treinar um só com o Macho Brace, sem Exp. Share.
- **Ninguém com meta nos atributos que o adversário dá**: o plano informa que esse adversário não ajuda nenhum dos dois.
- **Meta acima do possível**: o plano considera o que cada um ainda pode ganhar.
  - O limite é o menor entre a meta, 255 por atributo e 510 no total.
  - Metas que somam mais de 510 contam até o limite.
- **Batalhas duplas** (treinadores que lutam em dupla):
  - Na Gen 3, os dois Pokémon da frente participam, o que muda quem ganha EVs.
  - O plano não oferece esses treinadores. Se um aparecer por outro caminho, o app explica o motivo.
- **Treinador sem revanche** (ex.: Brock): a busca pelo nome não mostra cartão. O jogador registra os Pokémon um a um
  ou acha o time pelo local.
- **Revanches**: o jogador escolhe qual time do treinador está enfrentando (o original ou o de cada revanche do VS
  Seeker), porque os Pokémon e os EVs mudam.
- **Adversário selvagem**: o plano também aceita um único Pokémon selvagem como adversário. Ex.: Magnemite, 1 Sp. Atk
  por batalha.
- **Pokémon de nível 100 ou desmaiado**: não ganha EV no jogo. O app não sabe disso; vale o mesmo cuidado da spec 012.
- **Equipe e Exp. Share**:
  - O Exp. Share só pode ser dado a um Pokémon da equipe.
  - Ao tirar da equipe quem segura o Exp. Share, a marcação sai dele.
- **Equipe e Pokémon selecionado**:
  - Pode haver um Pokémon selecionado fora da equipe, aberto pela tela da equipe.
  - Nesse caso, o menu mostra a equipe e, separado, o Pokémon aberto, para o jogador saber onde está.
- **Apagar Pokémon**: um Pokémon apagado sai da equipe. Desfazer a exclusão o devolve à equipe se ainda houver vaga, ou
  aos guardados se não houver.
- **Outro aparelho**: a equipe é sincronizada e vai no backup. Se dois aparelhos mudarem a equipe, vale a regra de
  sincronização por Pokémon que já existe, sem nunca passar de 6.
- **Mais de 6 na equipe após juntar dados de dois aparelhos**: ficam os 6 alterados mais recentemente; os demais vão
  para os guardados.

## Requirements *(mandatory)*

### Functional Requirements

**Equipe atual**

- **FR-001**: Cada Pokémon cadastrado MUST estar **na equipe** ou **guardado**; a equipe MUST ter no máximo 6 Pokémon,
  em uma ordem definida pelo jogador.
- **FR-002**: O menu ☰ e o painel lateral do desktop MUST listar só os Pokémon da equipe, na ordem da equipe.
- **FR-003**: O app MUST oferecer uma tela da equipe, acessível pelo menu e pelo painel lateral, que mostra a equipe e
  os guardados, e permite:
  - colocar um Pokémon na equipe;
  - tirar um da equipe;
  - trocar um da equipe por um guardado;
  - mudar a ordem;
  - abrir qualquer Pokémon.
- **FR-004**: Colocar um Pokémon com a equipe cheia MUST exigir escolher quem sai; nunca pode haver mais de 6.
- **FR-005**: Tirar um Pokémon da equipe MUST NOT alterar EVs, IVs, histórico ou configurações dele.
- **FR-006**: Um Pokémon novo MUST entrar na equipe se houver vaga. Se a equipe estiver cheia, ele MUST ficar guardado e
  selecionado, com um aviso e acesso à tela da equipe.
- **FR-007**: Dados sem informação de equipe (aparelho, backup, sincronização) MUST ser lidos assim:
  - com até 6 Pokémon, todos na equipe;
  - com mais de 6, os 6 alterados mais recentemente na equipe e os demais guardados.
- **FR-008**: A equipe MUST ser salva no aparelho, incluída no backup e na sincronização; a junção de dados MUST manter
  no máximo 6 na equipe.
- **FR-009**: O Exp. Share MUST poder ser dado só a Pokémon da equipe. Tirar da equipe quem o segura MUST tirar a
  marcação.

**Plano de batalhas com Exp. Share**

- **FR-010**: O app MUST oferecer um plano de batalhas em que o jogador escolhe dois Pokémon da equipe e um adversário.
  - Por padrão, os dois Pokémon são o selecionado e o que segura o Exp. Share.
  - O adversário pode ser um treinador do guia de treino, com o time de uma das revanches, ou um Pokémon selvagem.
- **FR-011**: O rendimento por batalha MUST ser a soma dos rendimentos de EV dos Pokémon do time escolhido.
- **FR-012**: O plano MUST considerar quatro configurações:
  1. A na frente com Macho Brace e B com Exp. Share;
  2. B na frente com Macho Brace e A com Exp. Share;
  3. só A na frente com Macho Brace, sem Exp. Share;
  4. só B na frente com Macho Brace, sem Exp. Share.
  Os ganhos de cada uma MUST seguir as regras da spec 012, incluindo o Pokérus de cada um.
- **FR-013**: O plano MUST:
  1. nunca fazer nenhum dos dois passar do que ainda pode ganhar em nenhum atributo (o menor entre a meta, 255 e o
     limite de 510);
  2. entre os planos que respeitam isso, deixar o **menor restante** somado dos dois nos atributos que o adversário dá;
  3. em empate, usar o **menor número de batalhas**;
  4. em novo empate, usar menos configurações diferentes.
- **FR-014**: O resultado MUST mostrar cada configuração usada com:
  - quem vai na frente com o Macho Brace e quem segura o Exp. Share;
  - o número de batalhas;
  - os EVs ganhos por batalha por cada um;
  - os EVs finais esperados de cada um.
- **FR-015**: O resultado MUST mostrar o que ficará faltando para cada um ao fim, com a orientação de completar com
  adversários de menor rendimento.
- **FR-016**: O resultado MUST mostrar o total de batalhas e quantas a menos do que treinar cada um sozinho com o Macho
  Brace contra o mesmo adversário.
- **FR-017**: O plano MUST ser calculado a partir dos EVs atuais, de modo que, ao reabri-lo depois de batalhas
  registradas, mostre só o que falta.
- **FR-018**: Treinadores de batalha dupla MUST NOT ser oferecidos no plano, e o app MUST explicar o motivo.
- **FR-019**: "Usar esta configuração" MUST:
  - selecionar o Pokémon da frente;
  - marcar o Macho Brace nele e tirá-lo do outro;
  - pôr o Exp. Share no outro (ou em ninguém, nas configurações sem Exp. Share);
  - permitir desfazer.
- **FR-020**: O plano SHOULD permitir registrar uma batalha inteira do treinador com um toque, com um único Desfazer,
  como em FR-025.
**Busca pelo treinador**

- **FR-022**: A busca de adversários MUST encontrar treinadores pelo nome, pela classe ou pelos dois juntos, com as
  mesmas regras de texto da busca atual (sem diferenciar maiúsculas e acentos).
  - Só entram os treinadores com revanche pelo VS Seeker, os do guia de treino (spec 013).
  - Treinadores que lutam uma vez só (líderes de ginásio, Equipe Rocket etc.) não aparecem como cartão; os Pokémon deles
    continuam encontráveis pela busca por local, como hoje.
- **FR-023**: Cada treinador encontrado MUST aparecer num cartão com:
  - classe, nome e local;
  - o time de cada revanche, com os EVs por batalha;
  - os botões "Registrar batalha inteira" e "Planejar com Exp. Share" para cada time.
- **FR-024**: Os Pokémon dos times encontrados MUST também aparecer na grade de adversários, para registro individual.
- **FR-025**: "Registrar batalha inteira" MUST:
  - registrar cada Pokémon do time escolhido para o Pokémon selecionado;
  - aplicar as mesmas regras de Macho Brace, Pokérus, Exp. Share e limites;
  - poder ser desfeito de uma vez.
- **FR-026**: "Planejar com Exp. Share" MUST abrir o plano com o treinador e o time já escolhidos. Esse botão MUST NOT
  ser oferecido para treinadores de batalha dupla.

- **FR-021**: Todos os textos novos MUST vir dos recursos de idioma (português e inglês), e as telas MUST seguir o
  layout do app no celular e no desktop.

### Key Entities

- **Equipe**: lista ordenada de até 6 Pokémon cadastrados. Os demais estão guardados. Faz parte dos dados de treino e é
  sincronizada.
- **Adversário do plano**: um treinador do guia com o time de uma revanche, ou um Pokémon selvagem. Tem o rendimento de
  EV por batalha.
- **Configuração**: quem vai na frente com o Macho Brace e quem segura o Exp. Share (ou ninguém), com os ganhos por
  batalha de cada um.
- **Plano**: as configurações usadas, com o número de batalhas de cada uma, os EVs finais esperados e o restante. É
  sempre recalculado; não é salvo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Com 15 Pokémon cadastrados, o menu mostra no máximo 6. Trocar para um Pokémon da equipe continua levando
  2 toques: abrir o menu e escolher.
- **SC-002**: Nenhum dado de Pokémon se perde ao entrar ou sair da equipe, verificado em 100% dos casos de teste.
- **SC-003**: No exemplo do pedido (Electabuzz faltando 120 e Jolteon faltando 96 de Sp. Atk, contra o Bernie com 3
  Magneton), o plano indica 12 batalhas (8 + 4). Treinar cada um sozinho com o Macho Brace levaria 18 batalhas (10 + 8).
- **SC-004**: Em uma tabela de casos que inclui combinações de Pokérus, metas exatas e não exatas e times com mais de um
  atributo, o plano:
  - nunca passa da meta;
  - deixa o menor restante possível;
  - entre os de menor restante, usa o menor número de batalhas, verificado contra uma busca completa.
- **SC-005**: O plano aparece em menos de 1 segundo após escolher o adversário, no celular.
- **SC-006**: Aplicar uma configuração leva 1 toque. Antes, eram 3 mudanças manuais: Pokémon selecionado, Macho Brace e
  Exp. Share.

## Assumptions

- **Regras de EV**: valem as da Gen 3 já usadas pelo app (spec 012).
  - Quem lutou ganha ×2 com o Macho Brace e ×2 com o Pokérus.
  - Quem segura o Exp. Share ganha o rendimento inteiro, ×2 só pelo Pokérus dele.
  - Num treinador, cada Pokémon derrotado conta separadamente.
- **Itens únicos**: há um único Macho Brace e um único Exp. Share no jogo; por isso o plano alterna quem segura cada um.
- **Quem participa**: o plano supõe que só o Pokémon da frente luta e derrota cada adversário, sem trocas durante a
  batalha.
- **Sem passar da meta**: passar da meta é pior do que deixar um pouco faltando.
  - EVs não podem ser reduzidos de forma prática em FireRed/LeafGreen.
  - O restante é completado com o treino normal (adversários de menor rendimento).
- **Dados de treinadores**: os times vêm do guia de treino (spec 013), extraídos da decompilação de FireRed. Os
  rendimentos de EV vêm dos dados de referência do app.
- **Equipe do app e equipe do jogo**: a equipe do app é só uma organização da lista. Ela não é conferida com a equipe
  real do jogo.
- **Planejador da Elite Four**: a equipe do planejador da Elite Four (spec 015) continua separada. Ela planeja espécies
  e golpes, não Pokémon em treino.
- **Plano não salvo**: o plano não é guardado; é recalculado sempre a partir dos EVs atuais.
