# Feature Specification: App em inglês

**Feature Branch**: `011-english-language`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Criar a tradução para inglês, é escolher entre pt-br e inglês dependendo da
localização do broswer, não é localização GPS, é língua mesmo, além disso permitir mudar manualmente"

**Contexto**: o app é público, mas hoje só existe em português (pt-BR). A estrutura já foi feita para
receber outros idiomas (todo texto da interface vem de um arquivo de textos por idioma; o menu já tem
um seletor de idioma que só aparece quando há mais de um). Ainda faltam: os textos em inglês, a escolha
automática do idioma pelo idioma configurado no navegador e a tradução dos dados de referência que
estão em português (nomes de locais como "Rota 22", "Laboratório do Prof. Oak", "Ginásio de Pewter
(Brock)", anotações como "(raro)", "Pesca", "rival em Cerulean").

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Abrir o app no idioma do navegador (Priority: P1)

Um jogador cujo navegador está em inglês abre o app pela primeira vez e vê tudo em inglês: abas,
botões, títulos, mensagens, avisos, telas de captura, backup e sincronização. Um jogador com o
navegador em português vê tudo em português, como hoje. Nada é perguntado; o app usa o idioma
preferido configurado no navegador (não a localização geográfica do aparelho).

**Why this priority**: é o que torna o app utilizável para quem não lê português; sem isso, o resto
não tem valor.

**Independent Test**: abrir o app num navegador limpo configurado em inglês e conferir que nenhuma
tela mostra texto em português; repetir com o navegador em português.

**Acceptance Scenarios**:

1. **Given** um navegador com idioma preferido inglês (qualquer variante, ex.: en-US, en-GB) e nenhum
   idioma escolhido antes no app, **When** o jogador abre o app, **Then** toda a interface aparece em
   inglês.
2. **Given** um navegador com idioma preferido português (qualquer variante, ex.: pt-BR, pt-PT) e
   nenhum idioma escolhido antes, **When** o jogador abre o app, **Then** toda a interface aparece em
   português.
3. **Given** um navegador com a lista de idiomas "es, pt-BR, en", **When** o app abre pela primeira
   vez, **Then** usa português (o primeiro da lista que o app suporta).
4. **Given** um navegador só com idiomas que o app não suporta (ex.: francês, japonês), **When** o app
   abre pela primeira vez, **Then** usa inglês.
5. **Given** o app aberto em inglês, **When** o jogador navega por todas as telas (EVs, IVs, Ajustes,
   captura, edição manual, backup, sincronização, menus, avisos e confirmações), **Then** nenhum texto
   aparece em português.

---

### User Story 2 - Trocar o idioma manualmente (Priority: P1)

O jogador quer o app num idioma diferente do navegador (ex.: navegador em inglês, mas prefere
português). No menu, escolhe "Português (Brasil)" ou "English"; a interface muda na hora, sem
recarregar e sem perder o que estava fazendo. A escolha fica salva no aparelho e passa a valer em
todas as próximas aberturas, mesmo que o idioma do navegador mude.

**Why this priority**: faz parte do pedido e corrige qualquer detecção que não agrade.

**Independent Test**: com o app em inglês, escolher português no menu, conferir a troca imediata,
fechar e reabrir: continua em português.

**Acceptance Scenarios**:

1. **Given** o app em inglês, **When** o jogador escolhe "Português (Brasil)" no menu, **Then** toda a
   interface passa para português imediatamente, mantendo a aba, o Pokémon selecionado e a busca
   digitada.
2. **Given** o jogador escolheu um idioma no menu, **When** reabre o app (com o navegador em outro
   idioma), **Then** o app abre no idioma escolhido, não no do navegador.
3. **Given** o menu aberto, **When** o jogador olha a seção de idioma, **Then** vê as duas opções,
   cada uma escrita no próprio idioma ("Português (Brasil)", "English"), com a ativa marcada.
4. **Given** qualquer idioma ativo, **When** um leitor de tela lê a página, **Then** o idioma da
   página informado ao navegador corresponde ao idioma ativo.

---

### User Story 3 - Locais e anotações dos adversários no idioma ativo (Priority: P2)

Em inglês, os cartões e a busca mostram os locais com os nomes do jogo em inglês ("Route 22",
"Prof. Oak's Lab", "Pewter Gym (Brock)", "rare", "Fishing", "rival in Cerulean"). A busca por local
aceita o nome no idioma ativo: em inglês, "route 3" encontra os adversários da Rota 3; em português,
"rota 3" continua funcionando como hoje.

**Why this priority**: os locais são parte importante da busca; misturar "Rota" numa interface em
inglês confunde, mas o app já é usável em inglês sem isso.

**Independent Test**: em inglês, buscar "route 3" e "pewter gym" e conferir os mesmos resultados que
"rota 3" e "ginásio de pewter" dão em português, com os locais exibidos em inglês.

**Acceptance Scenarios**:

1. **Given** o app em inglês, **When** o jogador busca "route 22", **Then** aparecem os adversários
   da Rota 22, com o local exibido como "Route 22".
2. **Given** o app em inglês, **When** um cartão mostra onde encontrar o Pokémon, **Then** o texto
   não contém palavras em português ("Rota", "raro", "Pesca", "Ginásio", "Laboratório", "parcial",
   "foge", "em").
3. **Given** o app em português, **When** o jogador busca "rota 3", **Then** o resultado é o mesmo de
   antes desta feature.

---

### Edge Cases

- **Usuário atual (já usou o app antes desta feature)**: continua no idioma em que estava (português);
  a detecção automática vale só para quem ainda não tem idioma salvo.
- **Navegador sem lista de idiomas** (ou lista vazia): usa inglês.
- **Variantes regionais**: qualquer "pt-*" conta como português; qualquer "en-*" conta como inglês.
- **Idioma salvo que deixou de existir** (dado antigo ou corrompido): ignorado; volta a valer a
  detecção pelo navegador.
- **Nomes que não mudam**: nomes de Pokémon, de natures, de líderes e de lugares que já são em inglês
  no jogo (ex.: "Mt. Moon", "Viridian Forest") ficam iguais nos dois idiomas.
- **Dados do usuário**: apelidos, anotações e registros de batalha salvos não são traduzidos nem
  alterados; trocar o idioma não muda nenhum dado salvo, backup ou sincronização.
- **Backup e sincronização**: um backup exportado em inglês importa normalmente com o app em português
  e vice-versa; o idioma não faz parte dos dados sincronizados entre aparelhos.
- **Números e datas**: seguem o formato do idioma ativo onde o app mostra datas (ex.: avisos de
  sincronização, nome do arquivo de backup continua com data AAAA-MM-DD).
- **Primeira pintura**: ao abrir, o app não deve mostrar textos de um idioma e trocar para o outro de
  forma visível.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app MUST oferecer dois idiomas de interface: português (Brasil) e inglês.
- **FR-002**: Todo texto da interface que hoje existe em português MUST ter equivalente em inglês;
  nenhuma tela, mensagem, aviso, confirmação, rótulo de acessibilidade ou texto alternativo pode ficar
  sem tradução.
- **FR-003**: Sem idioma salvo no aparelho, o app MUST escolher o idioma pela lista de idiomas
  preferidos do navegador, na ordem dela: o primeiro que for português (qualquer variante) ou inglês
  (qualquer variante) vence.
- **FR-004**: Se nenhum idioma da lista do navegador for suportado, ou a lista estiver indisponível, o
  app MUST usar inglês.
- **FR-005**: A escolha MUST usar o idioma configurado no navegador, nunca a localização geográfica do
  aparelho; o app MUST NOT pedir permissão de localização.
- **FR-006**: O menu MUST permitir trocar entre os dois idiomas a qualquer momento, com cada opção
  escrita no próprio idioma e a opção ativa indicada.
- **FR-007**: A troca de idioma MUST ser aplicada imediatamente em toda a interface, sem recarregar e
  sem perder o estado da tela (aba, Pokémon selecionado, busca, editor aberto).
- **FR-008**: Uma escolha manual MUST ficar salva no aparelho e MUST prevalecer sobre o idioma do
  navegador em todas as aberturas seguintes.
- **FR-009**: Quem já tem idioma salvo antes desta feature MUST continuar no idioma salvo.
- **FR-010**: O idioma da página informado ao navegador e a leitores de tela MUST corresponder ao
  idioma ativo desde a abertura.
- **FR-011**: Os nomes de locais e as anotações de onde encontrar cada adversário MUST aparecer no
  idioma ativo, usando os nomes oficiais do jogo em inglês quando o idioma for inglês.
- **FR-012**: A busca por local ou líder MUST aceitar os nomes no idioma ativo (em inglês, "route 3",
  "pewter gym"; em português, como hoje).
- **FR-013**: Os nomes de stats e as notas de avaliação (ex.: Ótimo/Bom/Médio/Ruim) MUST aparecer no
  idioma ativo.
- **FR-014**: Trocar de idioma MUST NOT alterar dados salvos, backups ou o que é sincronizado; o
  idioma é uma preferência só do aparelho.
- **FR-015**: Um teste automatizado MUST garantir que o inglês tem todos os textos do português
  (nenhuma chave faltando) e que nenhuma tela em inglês mostra texto fixo.

### Key Entities

- **Idioma da interface**: português (Brasil) ou inglês; tem um nome exibido no próprio idioma.
- **Preferência de idioma**: escolha salva no aparelho; ausente até o jogador escolher (ou até ser
  definida pela detecção na primeira abertura — ver Assumptions).
- **Textos de local**: cada local/anotação de referência tem uma versão por idioma, usada na exibição
  e na busca.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Com o navegador em inglês, 100% das telas do app abrem sem nenhum texto em português
  (fora nomes próprios iguais nos dois idiomas e dados digitados pelo usuário).
- **SC-002**: Com o navegador em português, o app abre em português em 100% dos casos, sem mudança
  visível para quem já usa o app.
- **SC-003**: O jogador troca o idioma com no máximo 2 toques a partir de qualquer tela, e a troca
  aparece em menos de 1 segundo.
- **SC-004**: Depois de uma escolha manual, 100% das aberturas seguintes respeitam essa escolha.
- **SC-005**: Em inglês, buscar o nome em inglês de qualquer local de referência retorna os mesmos
  adversários que o nome em português retorna em português.
- **SC-006**: O teste de completude acusa 0 chaves faltando no inglês.

## Assumptions

- "Localização do navegador" significa a lista de idiomas preferidos configurada no navegador, não a
  região nem a posição GPS.
- Inglês é o idioma de reserva para navegadores em idiomas não suportados, por ser o mais
  compreendido por um público internacional; português continua sendo o idioma padrão do projeto
  (o idioma base de onde vêm os textos).
- Não há opção "automático" no menu: depois de uma escolha manual, para voltar a seguir o navegador
  basta escolher o idioma do navegador. Na primeira abertura, o idioma detectado não é gravado como
  escolha manual, então se o navegador mudar de idioma antes de o jogador escolher, o app acompanha.
- Usuários atuais que já têm preferências salvas no aparelho têm "português" gravado nelas e por isso
  continuam em português (FR-009); os que nunca salvaram preferência passam pela detecção, o que dá
  português para quem usa o navegador em português.
- Os nomes oficiais em inglês dos locais de FireRed (Route N, Prof. Oak's Lab, Pewter Gym, etc.) são
  usados na tradução.
- O README e a documentação do projeto não fazem parte desta feature (o README já está em inglês).
- O nome do app ("EVs Trainer") é o mesmo nos dois idiomas.
