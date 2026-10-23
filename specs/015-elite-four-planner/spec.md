# Feature Specification: Planejador da Elite Four

**Feature Branch**: `015-elite-four-planner`

**Created**: 2026-10-23

**Status**: Draft

**Clarifications**: 2026-10-23 — o usuário pediu que a equipe seja sincronizada.

**Input**: User description: "Eu quero poder ter uma forma de planejar minha luta contra E4, tanto na primeira versão
quanto na segunda, eu quero poder definir uma equipe, e quero poder ver como seriam os melhores golpes para poder ter
maior coverage na luta contra toda E4, eu estou fazendo desafios e escolho os Pokémons e vou lutar, aí quero poder ter
algo que me ajude a ver como consigo passar por todos membros de E4 e seus pokemons"

**Contexto**: o jogador faz desafios (escolhe com quais Pokémon vai enfrentar a Liga) e quer saber, antes de entrar,
se a equipe tem resposta para cada Pokémon da Elite Four e do Campeão. No FireRed/LeafGreen a Liga tem duas versões:
a **primeira luta** (níveis 51–63) e a **revanche** (níveis 63–75, times diferentes), liberada depois da parte pós-jogo
das Ilhas Sevii. Os times, níveis, golpes e itens de cada membro estão no código do jogo (decompilação
pret/pokefirered), assim como os golpes que cada espécie pode aprender. O time do Campeão depende do inicial que o
jogador escolheu (o rival fica com o inicial que vence o do jogador).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver os times da Liga, golpe a golpe (Priority: P1)

O jogador abre a página **"Planejar a Elite Four"** pelo menu, escolhe a luta (**primeira** ou **revanche**) e o
inicial que ele escolheu no começo do jogo. A página mostra, em ordem de luta (Lorelei, Bruno, Agatha, Lance,
Campeão), cada Pokémon adversário com nível, tipos, habilidade, item e os 4 golpes que ele realmente usa (com o tipo de
cada golpe).

**Why this priority**: é a base de todo o planejamento; sozinha já substitui a pesquisa fora do app.

**Independent Test**: escolher "primeira luta" e ver a Lorelei com Dewgong nv 52 (Ice Beam, Surf, Hail, Safeguard),
Cloyster nv 51, Slowbro nv 52, Jynx nv 54 e Lapras nv 54 com Sitrus Berry; trocar para "revanche" e ver a Lorelei com
Dewgong nv 64, Cloyster nv 63, Piloswine nv 63, Jynx nv 66 e Lapras nv 66.

**Acceptance Scenarios**:

1. **Given** a página aberta, **When** o jogador escolhe "primeira luta", **Then** vê os cinco adversários em ordem,
   cada Pokémon com nível, tipos, habilidade, item segurado e os 4 golpes com seus tipos.
2. **Given** "revanche" escolhida, **When** a página é exibida, **Then** os times mostrados são os da revanche.
3. **Given** o inicial escolhido (Bulbasaur, Charmander ou Squirtle), **When** a página mostra o Campeão, **Then** o
   time é o que o rival usa para esse inicial (ex.: jogador com Bulbasaur → rival com Charizard).
4. **Given** a luta e o inicial escolhidos, **When** o jogador reabre o app, **Then** as escolhas continuam.

---

### User Story 2 - Montar a equipe e ver a cobertura contra cada adversário (Priority: P1)

O jogador monta a sua equipe: até 6 Pokémon, cada um com a espécie e até 4 golpes, escolhidos entre os golpes que essa
espécie pode aprender no FireRed/LeafGreen (por nível, TM/HM e tutores; a lista indica de onde vem cada golpe). Para
cada Pokémon adversário, a página mostra o melhor golpe da equipe contra ele (quem usa, qual golpe e a efetividade:
4×, 2×, 1×, ½×, ¼× ou imune) e marca em destaque os adversários que **nenhum** golpe da equipe acerta super efetivo e
os que nenhum golpe acerta sequer neutro.

**Why this priority**: é o pedido central: saber se a equipe passa por todos.

**Independent Test**: montar uma equipe só com um Pokémon que tem Earthquake e Thunderbolt; na Agatha, ver o Gengar
marcado como imune ao Earthquake (Levitate) e com Thunderbolt neutro; no Lance, ver Gyarados recebendo 4× do
Thunderbolt.

**Acceptance Scenarios**:

1. **Given** uma equipe montada, **When** o jogador vê um adversário, **Then** vê o melhor golpe da equipe contra ele,
   com o Pokémon que o usa e a efetividade, já considerando tipos e habilidades que anulam ou reduzem o golpe
   (ex.: Levitate contra Ground).
2. **Given** um adversário que nenhum golpe da equipe acerta super efetivo, **When** a página é exibida, **Then** ele
   aparece marcado como "sem golpe super efetivo"; se nem neutro, "sem resposta".
3. **Given** a escolha de golpes de um membro, **When** o jogador abre a lista, **Then** só aparecem golpes que a
   espécie pode aprender no FRLG, com a origem (nível X, TM/HM nº, tutor) e o tipo, poder e precisão de cada um.
4. **Given** um resumo no topo, **When** a equipe muda, **Then** o resumo mostra quantos adversários da luta escolhida
   têm golpe super efetivo, quantos só neutro e quantos ficam sem resposta.
5. **Given** a equipe montada, **When** o jogador reabre o app, **Then** a equipe continua lá.
6. **Given** a sincronização ligada em dois aparelhos, **When** o jogador muda a equipe num deles, **Then** o outro
   recebe a equipe na próxima sincronização; se os dois mudaram, fica a mudança mais recente.

---

### User Story 3 - Ver quem ameaça cada membro da equipe (Priority: P2)

Para cada Pokémon adversário, a página mostra quais membros da equipe ele ameaça: os golpes de dano que ele usa e
que acertam super efetivo um membro da equipe; e, para cada membro da equipe, quais adversários o ameaçam e contra
quais ele resiste a todos os golpes de dano. Assim o jogador sabe quem colocar na frente em cada luta.

**Why this priority**: completa o planejamento (ataque e defesa), mas a cobertura ofensiva já entrega valor sozinha.

**Independent Test**: com um Charizard na equipe, ver que o Onix do Bruno (Rock Tomb) e a Lapras da Lorelei (Surf) o
ameaçam; ver que o Gengar da Agatha (primeira luta, nv 54: Shadow Punch, Confuse Ray, Toxic, Double Team) não tem
golpe super efetivo contra o Charizard.

**Acceptance Scenarios**:

1. **Given** um adversário e a equipe, **When** a página é exibida, **Then** cada golpe de dano dele que acerta super
   efetivo algum membro aparece ligado aos membros ameaçados.
2. **Given** um membro da equipe, **When** o jogador vê a visão por membro, **Then** vê os adversários que o ameaçam e
   aqueles contra quem ele resiste a todos os golpes de dano (bons para entrar em campo).
3. **Given** golpes sem dano (Hail, Safeguard, Toxic…), **When** a ameaça é calculada, **Then** eles não contam como
   ameaça.

---

### User Story 4 - Sugerir os 4 melhores golpes de cada membro (Priority: P2)

O jogador toca em **"Sugerir golpes"** num membro (ou na equipe toda). O app escolhe até 4 golpes que a espécie pode
aprender, priorizando cobrir a Liga inteira da luta escolhida: primeiro aumentar o número de adversários que a equipe
acerta super efetivo, depois o de adversários acertados ao menos neutro, e então golpes mais fortes para esse Pokémon
(poder, mesmo tipo do Pokémon e categoria física/especial que combina com o melhor atributo de ataque da espécie).
O jogador pode aceitar a sugestão ou trocar golpes à mão; golpes fixados à mão não são trocados pela sugestão.

**Why this priority**: acelera a montagem; sem ela o jogador ainda planeja escolhendo golpes manualmente.

**Independent Test**: com um Vileplume Modest sozinho, sugerir golpes e ver um golpe Grass especial forte (ex.:
Solar Beam/Giga Drain), Sludge Bomb e golpes que cobrem os adversários que Grass e Poison não cobrem; nenhum golpe
sugerido é um que o Vileplume não aprende no FRLG.

**Acceptance Scenarios**:

1. **Given** um membro sem golpes, **When** o jogador pede a sugestão, **Then** recebe até 4 golpes de dano que a
   espécie aprende no FRLG, escolhidos para maximizar a cobertura da equipe contra a luta escolhida.
2. **Given** um membro com golpes fixados, **When** pede a sugestão, **Then** só as vagas livres são preenchidas.
3. **Given** "sugerir para a equipe", **When** o app sugere, **Then** considera a equipe inteira (evita que todos os
   membros cubram os mesmos adversários e deixem outros sem resposta).
4. **Given** a sugestão aplicada, **When** a cobertura é recalculada, **Then** o resumo mostra o resultado na hora.

---

### Edge Cases

- **Habilidades dos adversários**: os Pokémon dos treinadores sempre usam a primeira habilidade da espécie no
  FireRed/LeafGreen; Levitate anula Ground; Water Absorb, Volt Absorb e Flash Fire anulam Water, Electric e Fire;
  Thick Fat reduz Fire e Ice à metade; Wonder Guard só é atingido super efetivo.
- **Habilidade do membro da equipe**: o jogador pode escolher entre as habilidades da espécie (padrão: a primeira);
  ela vale para as ameaças (ex.: Levitate no próprio Gengar).
- **Golpes de dano fixo** (Dragon Rage, Seismic Toss, Night Shade, Sonic Boom, Super Fang): não são super efetivos;
  aparecem como dano fixo e não contam como ameaça super efetiva (exceto imunidade de tipo: Night Shade não afeta
  Normal; Seismic Toss não afeta Ghost; Dragon Rage e Sonic Boom acertam qualquer um).
- **Golpes de tipo variável ou especiais**: Hidden Power entra como tipo Normal, sem calcular o tipo real; golpes com
  poder variável (Return, Frustration, Low Kick, Magnitude, Present…) entram com o tipo e marcados "poder variável".
- **Struggle e golpes sem dano**: não contam para cobertura nem para ameaça.
- **Membros repetidos**: a equipe pode ter espécies repetidas.
- **Equipe vazia**: a página mostra só os times da Liga e um convite para montar a equipe.
- **Espécie sem o golpe**: se o jogador trocar a espécie de um membro, golpes que a nova espécie não aprende são
  removidos (com aviso).
- **Idioma**: nomes de Pokémon, golpes, habilidades e itens como no jogo em inglês; textos da página no idioma ativo.
- **Offline**: a página funciona sem internet.
- **Versão do jogo**: os times da Liga são iguais em FireRed e LeafGreen; a lista de golpes que a espécie aprende
  também; a página funciona igual nas duas versões.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app MUST ter uma página "Planejar a Elite Four" acessível pelo menu.
- **FR-002**: A página MUST permitir escolher a luta: primeira luta ou revanche.
- **FR-003**: A página MUST permitir escolher o inicial do jogador (Bulbasaur, Charmander, Squirtle) e mostrar o time do
  Campeão correspondente.
- **FR-004**: Para a luta escolhida, a página MUST mostrar Lorelei, Bruno, Agatha, Lance e o Campeão, em ordem, com
  cada Pokémon: espécie, nível, tipos, habilidade, item segurado e os 4 golpes (com o tipo de cada golpe).
- **FR-005**: O jogador MUST poder montar uma equipe de até 6 membros, cada um com espécie, habilidade (entre as da
  espécie) e até 4 golpes.
- **FR-006**: A escolha de golpes MUST oferecer só os golpes que a espécie aprende no FireRed/LeafGreen por nível,
  TM/HM ou tutor, cada um com origem, tipo, poder, precisão e categoria (física/especial, pela regra da Gen 3: depende
  do tipo).
- **FR-007**: Para cada Pokémon adversário, a página MUST mostrar o melhor golpe da equipe contra ele (membro, golpe e
  efetividade), considerando tipos (tabela da Gen 3) e as habilidades que anulam ou reduzem golpes.
- **FR-008**: A página MUST marcar os adversários sem golpe super efetivo da equipe e os sem resposta (nenhum golpe ao
  menos neutro).
- **FR-009**: A página MUST mostrar um resumo da luta escolhida: quantos adversários com golpe super efetivo, só
  neutro e sem resposta.
- **FR-010**: Para cada adversário, a página MUST mostrar os membros da equipe que ele ameaça (golpe de dano dele super
  efetivo contra o membro, considerando a habilidade do membro).
- **FR-011**: Para cada membro, a página MUST mostrar os adversários que o ameaçam e aqueles contra quem ele resiste a
  todos os golpes de dano.
- **FR-012**: O app MUST sugerir até 4 golpes por membro (ou para a equipe toda), só entre golpes que a espécie
  aprende, maximizando, nesta ordem: adversários com golpe super efetivo da equipe, adversários com golpe ao menos
  neutro, força do golpe para aquele Pokémon (poder × precisão, bônus de mesmo tipo, categoria que combina com o maior
  atributo de ataque base da espécie).
- **FR-013**: A sugestão MUST preservar golpes fixados pelo jogador.
- **FR-014**: A equipe, a luta e o inicial escolhidos MUST ficar salvos no aparelho e, com a sincronização ligada,
  MUST ir para os outros aparelhos; em conflito vence a versão alterada por último.
- **FR-015**: Os dados (times da Liga, golpes, aprendizado por espécie, habilidades) MUST estar embutidos no app e a
  página MUST citar a fonte (decompilação pret/pokefirered).
- **FR-016**: Todos os textos MUST vir dos recursos de idioma (pt e en).

### Key Entities

- **Membro da Liga**: Lorelei, Bruno, Agatha, Lance ou Campeão; tem um time por luta (primeira, revanche) e, no caso
  do Campeão, um time por inicial do jogador.
- **Pokémon adversário**: espécie, nível, item, habilidade, 4 golpes.
- **Golpe**: nome, tipo, poder (ou dano fixo/variável), precisão, PP; categoria física/especial derivada do tipo.
- **Aprendizado da espécie**: golpes que a espécie aprende, cada um com a origem (nível, TM/HM, tutor).
- **Equipe planejada**: até 6 membros; cada membro tem espécie, habilidade, até 4 golpes e quais golpes estão fixados.
- **Cobertura**: para cada adversário, o melhor golpe da equipe e a efetividade; para cada par adversário × membro, se
  há ameaça.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O jogador vê o time completo de qualquer membro da Liga, nas duas lutas, em no máximo 3 toques a partir de
  qualquer tela.
- **SC-002**: 100% dos Pokémon da Liga (primeira luta, revanche e as três versões do Campeão) aparecem com nível, item,
  habilidade e golpes iguais aos do jogo.
- **SC-003**: Mudar um golpe, a luta ou o inicial atualiza cobertura, ameaças e resumo na hora.
- **SC-004**: 100% dos golpes oferecidos e sugeridos são aprendíveis pela espécie no FRLG (verificável
  automaticamente contra os dados do jogo).
- **SC-005**: A efetividade mostrada bate com a tabela de tipos da Gen 3 e com as habilidades listadas em 100% dos
  casos de teste.
- **SC-006**: Com a sugestão aplicada à equipe toda, nenhum adversário fica sem golpe super efetivo quando existe
  alguma combinação de golpes aprendíveis pela equipe que o cubra (verificável nos casos de teste).
- **SC-007**: A equipe planejada persiste em 100% das reaberturas do app e chega ao outro aparelho na sincronização.

## Assumptions

- "Primeira versão" e "segunda versão" da E4 são a primeira luta e a revanche do FireRed/LeafGreen.
- A cobertura é por **tipo** (efetividade, mesmo tipo, poder e categoria), não um cálculo de dano com atributos, IVs,
  EVs e nível; uma calculadora de dano fica para uma versão futura.
- Golpes de ovo, de evento e os que a espécie só aprende em outros jogos não entram nesta versão; só nível, TM/HM e
  tutores do FRLG. Golpes que um Pokémon aprende só numa pré-evolução não entram.
- A equipe planejada é independente dos Pokémon em treino de EVs; fica salva no aparelho e entra na sincronização
  (Gist) junto com os Pokémon, mas não no arquivo de backup.
- Há uma única equipe planejada (o jogador troca os membros entre desafios).
- Itens dos adversários que mudam a luta (Sitrus Berry, berries de status) são só mostrados, sem efeito no cálculo.
- Climas, efeitos de golpes secundários e a ordem em que o adversário envia os Pokémon (sempre na ordem do time) não
  entram no cálculo; a ordem é mostrada.
