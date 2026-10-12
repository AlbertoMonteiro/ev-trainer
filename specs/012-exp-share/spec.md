# Feature Specification: Exp. Share

**Feature Branch**: `012-exp-share`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Em algum momento você vai ganhar o exp share, e dessa forma vai poder compartilhar os
evs ganhos com quem está com exp share, o app deveria permitir dizer qual outro pokemon está com exp share, assim o ev
é contabilizado automaticamente, vale lembrar que se macho brace estiver marcado no pokemon principal, quem está com
exp share não deve ganhar dobrado"

**Contexto**: em FireRed o jogador recebe o **Exp. Share** (de um assistente do Prof. Oak, ao registrar 50 Pokémon na
Pokédex). Na Gen 3, o Pokémon que segura o Exp. Share ganha, a cada adversário derrotado, **os mesmos EVs inteiros**
que o Pokémon que lutou (diferente da experiência, os EVs não são divididos), aplicando só os **multiplicadores dele
mesmo**: Pokérus dobra se **ele** tiver Pokérus; Macho Brace só dobra para quem **segura** o Macho Brace — e quem
segura o Exp. Share não pode segurar o Macho Brace ao mesmo tempo. Hoje o app só conta EVs para o Pokémon selecionado;
quem treina dois Pokémon ao mesmo tempo precisa registrar cada batalha duas vezes, trocando de Pokémon.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Indicar quem está com o Exp. Share (Priority: P1)

O jogador tem mais de um Pokémon no app. Ele marca que o Exp. Share está com um deles (por exemplo, o Abra) e passa a
treinar outro (o Wartortle). A partir daí, cada batalha registrada para o Wartortle também conta EVs para o Abra,
sem precisar trocar de Pokémon no app.

**Why this priority**: é o pedido central e o que economiza o trabalho de registrar tudo duas vezes.

**Independent Test**: com dois Pokémon, marcar o Exp. Share no segundo, registrar uma batalha no primeiro e conferir
que os EVs dos dois subiram conforme as regras.

**Acceptance Scenarios**:

1. **Given** dois Pokémon (Wartortle selecionado, Abra com Exp. Share, nenhum com Pokérus ou Macho Brace), **When** o
   jogador registra 1 Caterpie (+1 HP), **Then** Wartortle ganha +1 HP e Abra ganha +1 HP.
2. **Given** o Wartortle com **Macho Brace** marcado e o Abra com Exp. Share, **When** o jogador registra 1 Caterpie,
   **Then** Wartortle ganha +2 HP e Abra ganha +1 HP (o Macho Brace não vale para quem tem o Exp. Share).
3. **Given** o Abra com **Pokérus** e Exp. Share e o Wartortle sem Pokérus, **When** o jogador registra 1 Caterpie,
   **Then** Wartortle ganha +1 HP e Abra ganha +2 HP (o Pokérus vale para quem o tem).
4. **Given** os dois com Pokérus e o Wartortle com Macho Brace, **When** o jogador registra 1 Caterpie, **Then**
   Wartortle ganha +4 HP e Abra ganha +2 HP.
5. **Given** o Exp. Share marcado num Pokémon, **When** o jogador olha a tela de EVs, **Then** vê claramente quem está
   com o Exp. Share (ex.: "Exp. Share: Abra") e pode trocar ou tirar com um toque.
6. **Given** o Exp. Share marcado num Pokémon, **When** o jogador marca outro Pokémon, **Then** o Exp. Share sai do
   primeiro (existe só um Exp. Share no jogo).

---

### User Story 2 - Ver e desfazer o que foi compartilhado (Priority: P1)

Depois de registrar uma batalha, o aviso mostra o ganho dos dois Pokémon (ex.: "Caterpie: +2 HP · Abra +1 HP"). Tocar
em **Desfazer** desfaz a batalha nos dois. No histórico do Pokémon com Exp. Share, as batalhas compartilhadas aparecem
marcadas como vindas do Exp. Share.

**Why this priority**: sem isso o jogador não confia na conta automática e não consegue corrigir um toque errado.

**Independent Test**: registrar uma batalha com Exp. Share ativo, conferir o aviso com os dois ganhos, tocar em
Desfazer e ver os EVs dos dois voltarem.

**Acceptance Scenarios**:

1. **Given** o Exp. Share ativo, **When** o jogador registra uma batalha, **Then** o aviso mostra o ganho do Pokémon
   principal e o do Pokémon com Exp. Share.
2. **Given** o aviso de uma batalha compartilhada, **When** o jogador toca em Desfazer, **Then** os EVs dos dois
   Pokémon voltam ao valor anterior.
3. **Given** batalhas compartilhadas, **When** o jogador abre o Pokémon com Exp. Share, **Then** o histórico recente
   dele mostra essas batalhas com a indicação "Exp. Share" e cada uma pode ser removida individualmente (sem afetar o
   Pokémon principal).
4. **Given** o Pokémon com Exp. Share já atingiu 510 no total (ou 255 no atributo), **When** o jogador registra uma
   batalha, **Then** o principal ganha normalmente e o aviso indica que o do Exp. Share não ganhou EV (limite atingido).

---

### Edge Cases

- **Exp. Share no próprio Pokémon selecionado**: se o jogador seleciona como principal o Pokémon que está com o
  Exp. Share, nada é compartilhado (ele ganha só uma vez, como hoje).
- **Só um Pokémon no app**: a opção de Exp. Share não aparece (não há com quem compartilhar).
- **Pokémon com Exp. Share apagado**: o Exp. Share deixa de estar marcado; desfazer a exclusão o traz de volta marcado.
- **Vitaminas**: não são batalhas; usar uma vitamina no principal nunca é compartilhada.
- **Edição manual de EVs** ("Editar EVs") e importação de planilha: não são compartilhadas.
- **Limites de cada Pokémon**: cada um respeita os próprios limites (255 por atributo, 510 no total); o que um não pode
  ganhar não muda o que o outro ganha.
- **Macho Brace marcado no Pokémon com Exp. Share**: ignorado para os ganhos compartilhados (ele está segurando o
  Exp. Share, não o Macho Brace); o marcador dele continua valendo quando ele for o principal.
- **Pokémon desmaiado**: no jogo, um Pokémon desmaiado não ganha EVs; o app não sabe disso — o jogador tira o
  Exp. Share ou desfaz o ganho compartilhado no histórico.
- **Pokémon nível 100**: na Gen 3, Pokémon nível 100 não ganham EVs em batalha; o app não acompanha o nível atual —
  o jogador tira o Exp. Share.
- **Outro aparelho / backup**: quem está com o Exp. Share é salvo junto com os dados e vai no backup e na
  sincronização, como Pokérus e Macho Brace.
- **Contagens ×N e "Recentes"** do Pokémon com Exp. Share também passam a contar as batalhas compartilhadas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app MUST permitir marcar qual Pokémon da lista está com o Exp. Share, trocar e tirar a marcação.
- **FR-002**: No máximo um Pokémon MUST estar com o Exp. Share por vez; marcar outro tira do anterior.
- **FR-003**: Ao registrar uma batalha para o Pokémon selecionado, se outro Pokémon estiver com o Exp. Share, o app
  MUST registrar a mesma batalha (mesmo adversário e quantidade) também para ele.
- **FR-004**: O Pokémon com Exp. Share MUST ganhar o rendimento de EV inteiro do adversário (sem dividir), dobrado
  somente se **ele** tiver Pokérus.
- **FR-005**: O Macho Brace do Pokémon principal MUST NOT dobrar o ganho do Pokémon com Exp. Share, e o Macho Brace
  marcado no próprio Pokémon com Exp. Share MUST NOT valer para ganhos compartilhados.
- **FR-006**: Os ganhos do Pokémon principal MUST continuar exatamente como hoje (Pokérus e Macho Brace dele).
- **FR-007**: Cada Pokémon MUST respeitar seus próprios limites de EV (255 por atributo, 510 no total).
- **FR-008**: Se o Pokémon selecionado for o próprio Pokémon com Exp. Share, nada MUST ser compartilhado.
- **FR-009**: O aviso da batalha MUST mostrar o ganho dos dois Pokémon, incluindo quando o do Exp. Share não ganhou
  nada por limite.
- **FR-010**: Desfazer pelo aviso MUST desfazer a batalha nos dois Pokémon de uma vez.
- **FR-011**: O histórico do Pokémon com Exp. Share MUST mostrar as batalhas compartilhadas identificadas como
  "Exp. Share", removíveis uma a uma sem afetar o principal.
- **FR-012**: Vitaminas, edição manual de EVs e importação de planilha MUST NOT ser compartilhadas.
- **FR-013**: A tela de EVs MUST mostrar quem está com o Exp. Share quando houver, com acesso de um toque para trocar
  ou tirar.
- **FR-014**: Quem está com o Exp. Share MUST ser salvo no aparelho, incluído no backup e na sincronização; backups
  antigos sem essa informação MUST ser lidos como "ninguém com Exp. Share".
- **FR-015**: Todos os textos novos MUST vir dos recursos de idioma (português e inglês).

### Key Entities

- **Portador do Exp. Share**: referência a um Pokémon da lista (ou nenhum). Único por conjunto de dados.
- **Batalha compartilhada**: registro de batalha no histórico do Pokémon com Exp. Share, com o mesmo adversário e
  quantidade da batalha original, multiplicador só pelo Pokérus dele, e marcado como vindo do Exp. Share.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Com o Exp. Share ativo, registrar uma batalha atualiza os EVs dos dois Pokémon com 1 toque (antes: 2
  registros e 2 trocas de Pokémon).
- **SC-002**: Em 100% das combinações de Pokérus/Macho Brace dos dois Pokémon, os ganhos batem com as regras da Gen 3
  descritas no contexto (verificado por tabela de casos).
- **SC-003**: Desfazer pelo aviso devolve os EVs dos dois Pokémon ao valor anterior em 100% dos casos.
- **SC-004**: Um backup exportado com Exp. Share marcado, importado em outro aparelho, mantém o mesmo portador.

## Assumptions

- Regra da Gen 3: o portador do Exp. Share ganha o rendimento de EV inteiro do adversário; Pokérus dobra por Pokémon;
  Macho Brace dobra só para quem o segura; quem segura o Exp. Share não segura o Macho Brace.
- Há um único Exp. Share em FireRed, por isso só um portador.
- A marcação vale para as batalhas registradas a partir dela; batalhas já registradas não são alteradas.
- O app não acompanha desmaios nem o nível atual dos Pokémon; nesses casos o jogador ajusta manualmente (tirar o
  Exp. Share ou remover o ganho no histórico).
- O portador do Exp. Share é parte dos dados de treino (como Pokérus e Macho Brace), não uma preferência do aparelho.
