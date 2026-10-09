# Feature Specification: EVs Trainer — reconstrução no browser

**Feature Branch**: `001-ev-trainer-app` (repositório ainda sem git; nome reservado para a branch)

**Created**: 2026-10-09

**Status**: Draft

**Input**: User description: "Quero a feature baseada no @HANDOFF.md mas considerando o que já
discutimos."

**Contexto considerado**: `HANDOFF.md` (produto, regras, algoritmos, interface e casos de teste),
`reference-data.json`, `user-data-export.json`, `app-atual.html` (referência de comportamento) e a
constituição v1.0.0. Em relação ao HANDOFF, esta versão **substitui a sincronização em nuvem por
dados só no aparelho + exportar/importar backup**, troca a "Tela cheia" por app instalável e
indicador de status local.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar um Pokémon e contar EVs com um toque (Priority: P1)

O jogador está com o FireRed aberto ao lado. Ele cadastra o Pokémon que vai treinar (nome, nature,
linha evolutiva com stats base e metas de EV), e depois de cada batalha toca no adversário
derrotado. O app soma os EVs respeitando as regras do jogo, mostra quanto ganhou e permite desfazer
um toque errado. Pokérus e Macho Brace podem ser ligados para as próximas batalhas.

**Why this priority**: é a razão de existir do app: o jogo não mostra EVs e não permite resetá-los;
sem o contador o treino planejado é impossível.

**Independent Test**: num navegador limpo, cadastrar um Pokémon, tocar em adversários e vitaminas,
fechar e reabrir o navegador e conferir que os totais por stat e o total geral batem com as regras
do jogo.

**Acceptance Scenarios**:

1. **Given** nenhum Pokémon cadastrado, **When** o jogador abre o app, **Then** vê "Nenhum Pokémon
   ainda." e um botão "Adicionar Pokémon"; ao tocar, o novo Pokémon abre direto na aba Ajustes.
2. **Given** um Pokémon com 0 EVs e modificadores desligados, **When** o jogador toca em Caterpie,
   **Then** o HP passa a 1, o contador do card mostra `×1` e aparece o aviso "Caterpie: +1 HP" com
   opção "Desfazer".
3. **Given** o aviso de uma batalha recém-registrada, **When** o jogador toca "Desfazer", **Then** a
   batalha é removida e os totais voltam ao valor anterior.
4. **Given** Pokérus ligado, **When** o jogador toca em Caterpie, **Then** ganha +2 HP; **Given**
   Pokérus e Macho Brace ligados, **Then** ganha +4 HP.
5. **Given** batalhas registradas com Pokérus ligado, **When** o jogador desliga o Pokérus, **Then**
   os totais já registrados não mudam.
6. **Given** os eventos `Bulbasaur ×1`, `Calcium ×12`, `Caterpie ×300 (×2)`, **When** os totais são
   calculados, **Then** o resultado é `[255, 0, 0, 100, 0, 0]`.
7. **Given** um stat que já atingiu o limite, **When** o jogador toca num adversário que só dá esse
   stat, **Then** a batalha é registrada e o aviso diz "<nome>: não deu EV (limite atingido)".
8. **Given** EVs registrados, **When** o jogador fecha e reabre o navegador no mesmo aparelho,
   **Then** todos os dados estão lá, sem nenhum login.
9. **Given** o resumo de EVs, **When** um stat atinge exatamente a meta (> 0), **Then** o mini card
   fica destacado como concluído; **When** passa da meta, **Then** fica destacado como excedido.

---

### User Story 2 - Migrar os dados atuais e fazer backup (Priority: P2)

O jogador já tem um Wartortle em treino no app antigo, exportado em `user-data-export.json`. Ele
importa esse arquivo e continua de onde parou. Como os dados ficam só no aparelho, ele também pode
exportar um backup a qualquer momento e importá-lo em outro aparelho ou navegador.

**Why this priority**: sem migração o jogador perde o progresso do treino em andamento; sem backup,
trocar de aparelho ou limpar o navegador apaga tudo, e não existe sincronização em nuvem.

**Independent Test**: importar `user-data-export.json` num navegador limpo e comparar totais de EV,
contagens por adversário e faixas de IV com o app atual; exportar, importar em outro navegador e
comparar novamente.

**Acceptance Scenarios**:

1. **Given** um navegador sem dados, **When** o jogador importa `user-data-export.json`, **Then** o
   Wartortle aparece com as mesmas batalhas, níveis anotados, metas, nature e linha evolutiva, e os
   totais de EV e faixas de IV são idênticos aos do app atual.
2. **Given** eventos com nomes antigos (`Bulbasaur (rival)`, `Nidoran F`), **When** importados,
   **Then** são reconhecidos como Bulbasaur e Nidoran♀ e contam EVs normalmente, e o arquivo
   exportado depois mantém os nomes como foram gravados.
3. **Given** Pokémon cadastrados, **When** o jogador toca "Exportar backup", **Then** recebe um
   arquivo com a data no nome contendo todos os Pokémon.
4. **Given** um backup exportado, **When** importado em outro navegador, **Then** os dados ficam
   idênticos aos de origem.
5. **Given** que o jogador escolhe importar um backup, **When** o arquivo é lido, **Then** o app
   mostra uma prévia ("N Pokémon: X novos, Y substituídos") e só grava após confirmação.
6. **Given** um arquivo inválido ou de outro formato, **When** importado, **Then** nada é alterado e
   aparece uma mensagem de erro clara.

---

### User Story 3 - Encontrar o adversário certo rapidamente (Priority: P3)

Na lista de adversários o jogador vê, por padrão, só os que ainda ajudam a bater as metas
("Úteis"), pode alternar para "Todos", vê os adversários recentes no topo e busca por nome, número
da Pokédex, rota, líder de ginásio ou local de treinador.

**Why this priority**: com 157 entradas, achar o adversário certo em segundos é o que torna o
registro de um toque viável na prática.

**Independent Test**: com um Pokémon e metas definidas, verificar o filtro Úteis, a lista de
recentes e as buscas de referência do HANDOFF §6.

**Acceptance Scenarios**:

1. **Given** metas `[252, 0, 2, 252, 0, 4]` e DEF = 2, **When** o filtro "Úteis" está ativo,
   **Then** Geodude e Ivysaur não aparecem, e Caterpie e Oddish aparecem.
2. **Given** uma busca "rota 4", **Then** os resultados incluem Mankey (local selvagem "Rota 3, 4,
   22") e Paras/Parasect (treinadores da Rota 4).
3. **Given** uma busca "rota 1", **Then** só aparecem Pidgey e Rattata (não casa Rotas 12–19).
4. **Given** uma busca "brock", **Then** aparecem Sandshrew, Geodude e Onix.
5. **Given** uma busca "nidoran f", **Then** aparece Nidoran♀.
6. **Given** um resultado encontrado por local de treinador, **Then** o card mostra
   "Treinadores: <locais>"; local selvagem e número da Pokédex não aparecem no card.
7. **Given** texto na busca, **Then** o filtro Úteis/Todos é ignorado e a lista de recentes some.
8. **Given** batalhas registradas e busca vazia, **Then** aparecem até 6 adversários recentes
   distintos (mais recente primeiro), que não se repetem na lista principal.
9. **Given** nenhum adversário útil restante, **Then** aparece "Nenhum outro adversário útil. Toque
   em Todos…"; **Given** busca sem resultado, **Then** aparece "Nada encontrado. Locais depois da
   Rota 19 ainda não têm treinadores cadastrados: busque pelo nome do Pokémon."

---

### User Story 4 - Estimar IVs a partir dos stats de cada nível (Priority: P4)

Logo após subir de nível, o jogador anota o nível, a espécie atual e os 6 stats da tela. Os EVs
vêm pré-preenchidos com os totais atuais (editáveis). O app mostra a faixa de IV possível por stat
em cada anotação e a faixa final cruzando todas.

**Why this priority**: valioso para decidir se vale treinar o Pokémon, mas depende do registro de
EVs (US1) para ser preciso.

**Independent Test**: anotar os 6 níveis do caso de interseção do HANDOFF §6 e conferir a linha
Final.

**Acceptance Scenarios**:

1. **Given** Squirtle Timid, nível 5, EVs 0, stats `[20, 9, 12, 11, 12, 11]`, **Then** a faixa é
   HP 12–31, ATK 4–31, DEF 10–29, SPA 20–31, SPD 12–31, SPE 14–31.
2. **Given** as 6 anotações do caso de interseção (Squirtle → Wartortle, Timid), **Then** a linha
   Final mostra HP 23–25, ATK 20–21, DEF 18–19, SPA 26–28, SPD 29, SPE 30.
3. **Given** uma anotação cujo stat nenhum IV de 0 a 31 reproduz, **Then** a célula mostra "?" em
   destaque de erro e uma nota explica que o dado está inconsistente.
4. **Given** anotações com faixas que não se cruzam, **Then** a linha Final mostra "conferir"
   para aquele stat; sem anotações válidas, mostra "—".
5. **Given** o formulário de anotação, **Then** a espécie vem selecionada com a última usada e os
   EVs vêm com os totais atuais; **When** salvo, a anotação entra na tabela ordenada por nível.
6. **Given** uma anotação existente, **When** o jogador a remove, **Then** ela sai da tabela com
   opção de desfazer.

---

### User Story 5 - Gerenciar vários Pokémon e o histórico (Priority: P5)

O jogador treina mais de um Pokémon. Pelo menu ele troca entre eles (vendo o total de cada um),
adiciona novos, edita ajustes, corrige o histórico recente e apaga um Pokémon, sempre com opção
de desfazer.

**Why this priority**: amplia o uso para o time inteiro e permite corrigir erros, mas o fluxo
principal funciona com um único Pokémon.

**Independent Test**: cadastrar dois Pokémon, alternar entre eles registrando batalhas, remover um
item do histórico e apagar um Pokémon, desfazendo cada ação.

**Acceptance Scenarios**:

1. **Given** dois Pokémon, **When** o jogador abre o menu, **Then** vê cada um com nome e total
   (ex. `82/510`), com marcador no selecionado; escolher um troca o Pokémon ativo e fecha o menu.
2. **Given** o menu aberto, **When** o jogador toca fora ou pressiona Esc, **Then** o menu fecha.
3. **Given** o histórico recente, **Then** mostra os últimos 12 eventos, mais novo primeiro, com
   quantidade e multiplicador; remover um evento recalcula os totais e oferece desfazer.
4. **Given** a aba Ajustes, **When** o jogador define metas que somam mais de 510, **Then** vê a
   soma ao vivo e um aviso, e ainda pode salvar.
5. **Given** a aba Ajustes, **When** o jogador apaga o Pokémon, **Then** ele some do menu e um
   aviso oferece desfazer por alguns segundos.
6. **Given** a nature escolhida, **Then** o seletor mostra "(+X −Y)" ou "neutra" para cada opção.

---

### User Story 6 - Importar registros colados de uma planilha (Priority: P6)

O jogador tem anotações antigas numa planilha. Ele cola as linhas no app, escolhe se são batalhas
ou níveis, vê uma prévia de quantas linhas foram reconhecidas e confirma.

**Why this priority**: útil para quem tem histórico fora do app; o backup JSON (US2) já cobre a
migração principal.

**Independent Test**: colar linhas de batalhas e de níveis com separador TAB e `;` e conferir a
prévia e o resultado.

**Acceptance Scenarios**:

1. **Given** o modo Batalhas, **When** o jogador cola `Caterpie;3;S`, **Then** a prévia reconhece
   1 linha e, ao confirmar, adiciona Caterpie ×3 com multiplicador ×2 ao fim do registro.
2. **Given** uma linha sem nome de adversário reconhecido, **Then** ela é contada como ignorada na
   prévia ("N linha(s) reconhecida(s), M ignorada(s)").
3. **Given** o modo Níveis, **When** o jogador cola `21<TAB>Wartortle<TAB>` seguido de 6 stats e 6
   EVs, **Then** a anotação é inserida e a tabela de IVs fica ordenada por nível.
4. **Given** uma linha de nível com menos de 6 stats, **Then** ela é ignorada.

---

### Edge Cases

- Total de 510 atingido: novas batalhas são registradas, mas não somam EV, e o aviso informa.
- Vitamina usada com o stat em 95: soma só +5 (para em 100); com o stat ≥ 100: não soma nada.
- Pokérus/Macho Brace não afetam vitaminas.
- Toques repetidos no mesmo adversário com o mesmo multiplicador são agrupados num só evento;
  mudar o multiplicador inicia um novo evento.
- Evento com nome desconhecido (nem adversário nem apelido antigo): é preservado nos dados e
  exportado, mas não soma EV.
- Nature não escolhida: cálculo de IV trata como neutra.
- Espécie anotada que foi removida da linha evolutiva: a anotação mostra "?" em vez de falhar.
- Armazenamento do navegador indisponível ou cheio (modo privado, cota): o app continua utilizável
  na sessão, o indicador de status mostra erro e sugere exportar backup.
- Dados apagados pelo navegador: sem backup não há recuperação; o app deixa isso explícito perto
  da opção de exportar.
- Mesmo app aberto em duas abas: a última gravação vence; a outra aba passa a refletir os dados
  gravados ao ser recarregada.
- Busca com acentos ou símbolos (♀/♂) e busca terminada em número ("rota 1" ≠ "rota 13").
- Importação de backup com um Pokémon que já existe (mesmo identificador): é substituído pela
  versão do arquivo, após confirmação.

## Requirements *(mandatory)*

### Functional Requirements

**Pokémon em treino**

- **FR-001**: Usuários DEVEM poder cadastrar vários Pokémon, cada um com nome, nature (25 opções
  ou nenhuma), linha evolutiva de 1 a 3 espécies e metas de EV (0–255 por stat). A espécie DEVE ser
  escolhida numa lista pesquisável (nome ou número da Pokédex, espécies #1–386); a linha evolutiva e
  os stats base da Geração III DEVEM ser preenchidos automaticamente, sem digitação. Em evoluções
  ramificadas (ex.: Eevee, Gloom, Poliwhirl) o usuário só escolhe a opção. O nome do Pokémon vem
  preenchido com o da espécie escolhida.
- **FR-002**: O sistema DEVE mostrar a soma das metas ao vivo e avisar quando passar de 510, sem
  impedir o salvamento.
- **FR-003**: Usuários DEVEM poder trocar o Pokémon ativo por um menu que lista nome e total de EVs
  de cada um, adicionar um novo Pokémon e apagar um Pokémon com desfazer.
- **FR-004**: Um Pokémon novo DEVE abrir direto na aba de ajustes.

**Contagem de EVs**

- **FR-005**: Tocar num adversário ou vitamina DEVE registrar uma batalha/uso com um único toque,
  sem botão adicional.
- **FR-006**: Cada registro DEVE guardar o multiplicador vigente (×1, ×2 com Pokérus ou Macho Brace,
  ×4 com os dois); mudar os modificadores depois NÃO DEVE alterar registros anteriores.
- **FR-007**: Os totais de EV DEVEM ser sempre recalculados a partir do registro ordenado, aplicando
  limite de 255 por stat, 510 no total, e as regras de vitamina (+10, só abaixo de 100, teto 100,
  sem multiplicador).
- **FR-008**: Toques consecutivos no mesmo adversário com o mesmo multiplicador DEVEM ser agrupados
  num único registro com quantidade.
- **FR-009**: Após cada toque, o sistema DEVE mostrar um aviso com o ganho real ("Caterpie: +1 HP"
  ou "não deu EV (limite atingido)") e a opção Desfazer, que remove uma unidade do último registro.
- **FR-010**: O resumo DEVE mostrar total/510, soma das metas e, para cada um dos 6 stats,
  atual/meta com barra de progresso e destaque de concluído (= meta > 0) ou excedido (> meta).
- **FR-011**: Cada card de adversário DEVE mostrar nome, EVs que ele dá e quantas vezes já foi
  derrotado (`×N`); local selvagem e número da Pokédex NÃO aparecem no card.
- **FR-012**: O sistema DEVE mostrar o histórico dos últimos 12 registros (mais novo primeiro), com
  quantidade e multiplicador, permitindo remover um registro com desfazer.

**Lista, filtro e busca**

- **FR-013**: A lista DEVE oferecer os filtros "Úteis" (padrão) e "Todos". Um adversário é útil se
  todo stat em que ele dá EV estiver abaixo da meta e, se for vitamina, abaixo de 100.
- **FR-014**: A busca DEVE casar nome, número da Pokédex, local selvagem e locais de treinadores
  (incluindo líderes de ginásio), ignorando acentos e maiúsculas, tratando ♀/♂ como "f"/"m",
  expandindo listas e intervalos de rotas, e sem casar um número como prefixo de outro.
- **FR-015**: Com texto na busca, o filtro DEVE ser ignorado; quando o casamento vier de treinador,
  o card DEVE indicar "Treinadores: <locais>".
- **FR-016**: Sem texto na busca, o sistema DEVE mostrar até 6 adversários recentes distintos, sem
  repeti-los na lista principal.
- **FR-017**: Estados vazios DEVEM orientar o usuário (sem úteis restantes; busca sem resultado,
  mencionando a cobertura de treinadores até a Rota 19).

**IVs**

- **FR-018**: Usuários DEVEM poder anotar, por nível, a espécie, os 6 stats exibidos e os EVs
  daquele momento (pré-preenchidos com os totais atuais e editáveis), com teclado numérico.
- **FR-019**: Para cada anotação o sistema DEVE calcular a faixa de IV (0–31) de cada stat pela
  fórmula da Gen 3 com a nature do Pokémon, indicando "?" quando nenhum IV reproduz o valor.
- **FR-020**: O sistema DEVE mostrar uma linha Final com a interseção das faixas válidas por stat:
  "lo–hi", valor único, "conferir" se não se cruzam, "—" sem dados.
- **FR-021**: Anotações DEVEM ficar ordenadas por nível e poder ser removidas com desfazer.

**Dados, backup e importação**

- **FR-022**: Todos os dados DEVEM ser salvos automaticamente no próprio aparelho/navegador, sem
  conta, login ou servidor, e reaparecer ao reabrir o app.
- **FR-023**: A interface DEVE atualizar imediatamente a cada ação; salvar NÃO DEVE travar a
  interação nem apagar o que o usuário está digitando.
- **FR-024**: Um indicador DEVE mostrar o estado do salvamento (salvo neste aparelho, salvando,
  erro), com texto acessível.
- **FR-025**: Usuários DEVEM poder exportar todos os Pokémon num arquivo de backup e importar um
  backup, com prévia (novos/substituídos) e confirmação antes de gravar.
- **FR-026**: O formato de backup DEVE ser o mesmo de `user-data-export.json`, de modo que esse
  arquivo seja importado sem conversão manual.
- **FR-027**: Nomes antigos de adversários DEVEM ser reconhecidos pelos apelidos de referência na
  leitura, sem reescrever os dados gravados.
- **FR-028**: Usuários DEVEM poder colar linhas de planilha (separadas por TAB ou `;`) no modo
  Batalhas ou Níveis, ver a prévia "N reconhecida(s), M ignorada(s)" e confirmar a importação.
- **FR-029**: Nenhum dado do usuário DEVE sair do aparelho, exceto pelo arquivo que ele mesmo
  exporta.

**Uso e apresentação**

- **FR-030**: O app DEVE ser acessível por um endereço público na web, sem instalação, e DEVE
  continuar funcionando sem conexão após o primeiro acesso.
- **FR-031**: O app DEVE poder ser instalado na tela inicial do celular e abrir sem a barra do
  navegador (substitui a opção "Tela cheia" do app atual).
- **FR-032**: A interface DEVE ser em coluna única para celular, com tema claro e escuro seguindo o
  sistema e opção de forçar um dos dois.
- **FR-035**: A interface DEVE estar em português (pt-BR) nesta versão, com todos os textos vindos
  de recursos de idioma, de modo que um novo idioma (inglês) possa ser adicionado depois sem mudar
  telas nem regras. A opção de trocar de idioma DEVE aparecer no menu assim que houver mais de um
  idioma disponível, e a escolha DEVE ser lembrada no aparelho.
- **FR-033**: Animações DEVEM ser desativadas quando o usuário pedir menos movimento ao sistema;
  controles só com ícone DEVEM ter nome acessível.
- **FR-034**: Os dados de referência (151 Pokémon de Kanto + 6 vitaminas, locais selvagens,
  treinadores por local, natures e apelidos) e a Pokédex da Geração III (386 espécies com stats base
  da Gen III e de quem cada uma evolui) DEVEM vir embutidos no app.

### Key Entities

- **Pokémon em treino**: o Pokémon que o jogador está treinando. Nome, nature, linha evolutiva
  (1–3 espécies com stats base), metas de EV, registro de batalhas, anotações de nível, estado dos
  modificadores Pokérus/Macho Brace e posição no menu.
- **Registro de batalha**: um item do registro ordenado: adversário ou vitamina, quantidade e
  multiplicador vigente. Os totais de EV são sempre derivados destes registros.
- **Anotação de nível**: nível, espécie naquele nível, 6 stats exibidos e 6 EVs daquele momento.
- **Adversário (referência)**: Pokémon de Kanto ou vitamina, com número da Pokédex, EVs que
  concede, local selvagem e indicador de vitamina.
- **Local de treinadores (referência)**: nome do local (rota, ginásio/líder) e os adversários que
  os treinadores de lá usam.
- **Nature (referência)**: nome e o stat que aumenta e o que diminui (ou neutra).
- **Apelido (referência)**: nome antigo → nome atual de um adversário.
- **Backup**: arquivo com todos os Pokémon em treino, no formato de `user-data-export.json`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Registrar uma batalha leva 1 toque a partir da lista, e o ganho aparece na tela em
  menos de 1 segundo, num celular intermediário.
- **SC-002**: 100% dos casos de aceite do HANDOFF §6 (IVs, interseção, limites de EV, apelidos,
  buscas, filtro Úteis) produzem exatamente os resultados esperados.
- **SC-003**: Após importar `user-data-export.json`, os totais de EV, as contagens por adversário e
  as faixas de IV do Wartortle são idênticos aos do app atual.
- **SC-004**: Exportar e importar um backup em outro navegador resulta em dados 100% idênticos.
- **SC-005**: Após o primeiro acesso, todas as funções funcionam com o aparelho sem conexão.
- **SC-006**: Em 100 toques seguidos rápidos, nenhum registro é perdido após fechar e reabrir o app.
- **SC-007**: Em 90% das buscas por rota, líder ou nome, o adversário desejado aparece entre os
  resultados visíveis sem rolar, digitando até 6 caracteres.
- **SC-008**: Todas as telas são utilizáveis em 360 px de largura sem rolagem horizontal, em tema
  claro e escuro.
- **SC-009**: O jogador consegue cadastrar um novo Pokémon com metas e linha evolutiva em menos de
  2 minutos.
- **SC-010**: 100% dos textos visíveis da interface vêm de recursos de idioma; adicionar um idioma
  de teste com todas as chaves traduzidas troca todos os textos das telas sem nenhuma outra
  alteração.

## Assumptions

- Um único jogador por aparelho; não há contas, permissões nem compartilhamento entre usuários.
- Sem sincronização automática entre aparelhos (decisão registrada na constituição): o backup
  exportado é o único meio de levar os dados a outro aparelho.
- Importar um backup mescla por identificador: Pokémon do arquivo substituem os de mesmo
  identificador e os demais são adicionados; nada é apagado sem confirmação.
- O nome do arquivo de backup inclui a data da exportação.
- Os dados de referência são os de `reference-data.json` como estão; completar treinadores após a
  Rota 19, Pokémon de Gen 2+ (Sevii Islands) e adversários personalizados ficam fora do escopo.
- O comportamento de `app-atual.html` é a referência em qualquer ponto não detalhado aqui.
- Visual (cores, fontes, tamanhos) segue os tokens e regras do HANDOFF §5.
- A tradução para inglês (textos da interface e nomes de locais/rotas nos dados de referência,
  incluindo buscar por "route 4") fica para uma feature futura; esta versão só garante que ela
  possa ser feita sem retrabalho.
- Hospedagem estática pública e processo de publicação seguem a constituição; não fazem parte do
  comportamento descrito nesta spec.
