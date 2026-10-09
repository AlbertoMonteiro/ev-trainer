# Feature Specification: Sprites dos Pokémon

**Feature Branch**: `002-pokemon-sprites`

**Created**: 2026-10-09

**Status**: Draft

**Input**: User description: "vamos seguir com essa ideia, usar PokeAPI/sprites, sobre o item 1,
aceitar o buraco"

**Contexto considerado**: conversa anterior sobre fontes de sprites (escolhida: coleção pública
PokeAPI/sprites, versão FireRed/LeafGreen da Geração III) e sobre o comportamento offline:
os sprites vão junto com o app e ficam guardados no aparelho na instalação; o caso raro de ficar
sem conexão antes de terminar essa primeira instalação é **aceito** (sem esforço extra para
garanti-lo), e nesse caso — ou em qualquer falha de carregamento — o app mostra um selo colorido
no lugar da imagem. Constituição v1.1.0 (Princípios I, V, VI e VII).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reconhecer adversários pela imagem (Priority: P1)

Na aba EVs, cada card de adversário (recentes, lista útil, resultados de busca) mostra o sprite do
Pokémon como ele aparece no FireRed. O jogador acha o adversário que acabou de derrotar olhando a
imagem, sem precisar ler o nome.

**Why this priority**: é onde o jogador passa a maior parte do tempo, tocando após cada batalha; a
imagem acelera o reconhecimento na lista de duas colunas.

**Independent Test**: abrir a aba EVs com um Pokémon cadastrado e conferir que cada card de
Pokémon mostra o sprite correspondente ao número da Pokédex, e que o toque continua registrando a
batalha normalmente.

**Acceptance Scenarios**:

1. **Given** a lista de adversários, **When** ela é exibida, **Then** cada card de Pokémon mostra o
   sprite FireRed/LeafGreen daquela espécie, sem alterar nome, etiquetas de EV e `×N`.
2. **Given** um card com sprite, **When** o jogador toca nele, **Then** a batalha é registrada como
   antes (o card inteiro continua sendo o botão).
3. **Given** um card de vitamina, **Then** ele mostra o selo colorido (vitaminas não têm sprite).
4. **Given** os cards ainda carregando as imagens, **Then** o tamanho do card não muda quando a
   imagem aparece (sem "pulos" na lista).

---

### User Story 2 - Visual agradável sem a imagem (Priority: P2)

Se a imagem não puder ser exibida (sem conexão antes de o app terminar de se guardar no aparelho,
arquivo ausente ou erro), o lugar do sprite mostra um selo colorido com a cor do stat de EV que
aquele Pokémon dá e o número da Pokédex, no mesmo tamanho do sprite.

**Why this priority**: o caso é raro, mas sem tratamento a lista ficaria com ícones quebrados; o
selo mantém a tela coerente e ainda informa algo útil (o stat do EV).

**Independent Test**: simular falha de carregamento das imagens e conferir que cada card mostra o
selo na cor certa, com o número correto, sem ícone quebrado e sem mudar o layout.

**Acceptance Scenarios**:

1. **Given** que a imagem de Caterpie não carrega, **Then** o card mostra um selo na cor do stat
   HP com "#10".
2. **Given** um Pokémon que dá EV em dois stats (ex.: Ivysaur, SpAtk + SpDef), **Then** o selo
   combina as duas cores.
3. **Given** uma espécie sem dados de EV (#152–#386, fora da lista de adversários), **Then** o selo
   usa a cor de destaque do app.
4. **Given** o selo sendo exibido, **When** a imagem passa a carregar, **Then** o sprite aparece
   por cima do selo com uma transição suave (sem transição para quem pede movimento reduzido).
5. **Given** um leitor de tela, **Then** a imagem e o selo são tratados como decorativos (o nome
   do Pokémon já está no card), sem anunciar texto duplicado.

---

### User Story 3 - Sprites ao escolher espécie e no menu (Priority: P3)

O seletor de espécie (busca e linha evolutiva) e a lista de Pokémon do menu também mostram os
sprites, com o mesmo selo de reserva.

**Why this priority**: reforça o reconhecimento ao cadastrar e ao trocar de Pokémon, mas são telas
usadas com menos frequência.

**Independent Test**: buscar "squir" no seletor e conferir os sprites nas opções e na linha
Squirtle → Wartortle → Blastoise; abrir o menu e conferir o sprite de cada Pokémon em treino.

**Acceptance Scenarios**:

1. **Given** o seletor de espécie, **When** o jogador busca, **Then** cada opção mostra o sprite da
   espécie ao lado do número e do nome.
2. **Given** uma linha evolutiva escolhida, **Then** cada espécie da linha mostra seu sprite junto
   do nome e dos stats base.
3. **Given** o menu com Pokémon em treino, **Then** cada item mostra o sprite da espécie da anotação
   de nível mais recente, ou da primeira espécie da linha se não houver anotações.
4. **Given** qualquer uma dessas telas sem a imagem, **Then** aparece o selo de reserva (US2).

---

### Edge Cases

- Primeiro acesso interrompido (conexão cai antes de o app terminar de se guardar no aparelho):
  aceito; as imagens que não chegaram mostram o selo, e na próxima vez com conexão os sprites
  passam a aparecer normalmente.
- Espécie de um backup importado que não existe na Pokédex (nome digitado à mão no app antigo):
  selo na cor de destaque com a inicial do nome.
- Vitaminas: sempre selo, na cor do stat que a vitamina aumenta.
- Busca com muitos resultados: as imagens fora da tela só carregam quando o jogador rola até elas.
- Tema claro e escuro: o sprite (fundo transparente) e o selo ficam legíveis nos dois.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app DEVE exibir o sprite da Geração III (FireRed/LeafGreen) de cada uma das 386
  espécies, identificado pelo número da Pokédex, nos cards de adversário (recentes, lista e
  resultados de busca).
- **FR-002**: O seletor de espécie DEVE exibir o sprite em cada opção da busca e em cada espécie da
  linha evolutiva escolhida.
- **FR-003**: O menu DEVE exibir, para cada Pokémon em treino, o sprite da espécie da anotação de
  nível mais recente ou, sem anotações, da primeira espécie da linha evolutiva.
- **FR-004**: Os sprites DEVEM fazer parte do próprio app e ser servidos pelo mesmo endereço; o app
  NÃO DEVE buscar imagens em serviços de terceiros em tempo de uso.
- **FR-005**: Os sprites DEVEM ficar guardados no aparelho junto com o restante do app para uso
  offline; o caso de ficar sem conexão antes de essa primeira gravação terminar é aceito e coberto
  apenas pelo selo de reserva (FR-006).
- **FR-006**: Quando a imagem não puder ser exibida, o app DEVE mostrar no mesmo lugar e tamanho
  um selo com: (a) a cor do stat em que o Pokémon dá EV, ou um degradê das duas cores quando são
  dois stats; (b) a cor de destaque do app quando não houver dado de EV; (c) o número da Pokédex
  (`#10`) ou, sem número, a inicial do nome.
- **FR-007**: O espaço da imagem DEVE ter tamanho fixo, de modo que carregar, falhar ou trocar a
  imagem não altere o layout.
- **FR-008**: A imagem DEVE aparecer sobre o selo com transição suave, desativada quando o usuário
  pede menos movimento ao sistema.
- **FR-009**: Imagens fora da área visível NÃO DEVEM ser carregadas até ficarem próximas de
  aparecer.
- **FR-010**: Imagem e selo DEVEM ser decorativos para leitores de tela; o nome visível do
  Pokémon continua sendo o texto acessível do card.
- **FR-011**: O repositório DEVE registrar a origem dos sprites (coleção PokeAPI/sprites) e o aviso
  de direitos ("imagens © The Pokémon Company") junto aos arquivos, e o app DEVE exibir esse
  crédito numa área de informações acessível pelo menu.
- **FR-012**: Os sprites DEVEM ser obtidos por um processo de geração reproduzível versionado no
  repositório (mesma abordagem da Pokédex), não baixados à mão.
- **FR-013**: Todo texto novo (crédito, rótulos) DEVE vir dos recursos de idioma (Princípio VII).

### Key Entities

- **Sprite**: imagem de uma espécie, identificada pelo número da Pokédex (1–386), com fundo
  transparente, no estilo FireRed/LeafGreen.
- **Selo de reserva**: substituto visual do sprite; definido pela cor (stat de EV, degradê de dois
  stats ou cor de destaque) e pelo rótulo (número da Pokédex ou inicial).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das 386 espécies exibem o sprite correto (mesmo número da Pokédex) quando o app
  está instalado.
- **SC-002**: Com as imagens indisponíveis, 0 ícones quebrados aparecem em qualquer tela; todo
  espaço de imagem mostra o selo correto.
- **SC-003**: O carregamento ou a falha das imagens não desloca nenhum card (deslocamento de layout
  igual a zero ao rolar a lista).
- **SC-004**: O primeiro download do app cresce no máximo 400 KB com os sprites.
- **SC-005**: O registro de uma batalha continua levando 1 toque e o ganho aparece em menos de 1
  segundo, como na feature 001.
- **SC-006**: O app continua sem nenhuma requisição a domínios de terceiros.

## Assumptions

- Fonte: coleção pública PokeAPI/sprites, pasta da Geração III FireRed/LeafGreen, que cobre as
  espécies #1–386 (cerca de 0,5–0,9 KB por imagem, ~250 KB no total). O repositório declara as
  imagens como "© The Pokémon Company" e o restante sob CC0; o uso segue a prática comum de
  projetos de fã, gratuito e sem anúncios, com crédito explícito (FR-011). Remover os sprites a
  pedido dos detentores dos direitos é aceitável: o selo de reserva já cobre a ausência.
- O caso de ficar offline antes de o app terminar de se guardar no aparelho é aceito (decisão do
  usuário); não haverá imagem única combinada nem pré-carregamento adicional.
- Sprites frontais, estáticos, sem versões shiny, femininas ou animadas.
- A cor do selo usa os tokens de stat já definidos (HANDOFF §5.7), em tema claro e escuro.
- Para o Pokémon em treino não existe "espécie atual" registrada; usar a da anotação de nível mais
  recente é uma aproximação suficiente.
