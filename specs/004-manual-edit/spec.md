# Feature Specification: Edição manual de EVs, IVs e anotações

**Feature Branch**: `004-manual-edit`

**Created**: 2026-10-10

**Status**: Draft

**Input**: User description: "Quero uma forma que seja possivel editar manualmente, e se for necessário
apagar o histórico, avise e deixe o usuário decidir"

**Contexto**: os EVs são sempre calculados a partir do histórico de batalhas e as faixas de IV a partir
das anotações de nível (constituição, Princípio IV). Quando o jogador descobre que o valor real é outro
(batalhas não registradas, anotação errada, IV conhecido por outra fonte), hoje só consegue corrigir
mexendo no histórico item a item. Esta feature permite informar os valores corretos diretamente.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ajustar os EVs para os valores reais (Priority: P1)

O jogador percebe que os EVs do app não batem com o jogo (esqueceu de registrar batalhas, registrou a
mais). Ele toca em "Editar EVs", digita os 6 valores corretos e escolhe como aplicar: **manter o
histórico** (o app registra um "ajuste manual" no histórico a partir do qual a soma passa a valer os
valores informados) ou **apagar o histórico** e começar destes valores. Apagar exige confirmação
explícita com aviso do que será perdido.

**Why this priority**: os EVs são o núcleo do app; sem correção direta, um erro antigo exige refazer o
histórico inteiro.

**Independent Test**: com EVs em `[10, 0, 2, 20, 0, 4]`, editar para `[12, 0, 2, 26, 0, 4]` mantendo
o histórico ⇒ totais exatos, histórico preservado com um item "Ajuste manual"; batalhas seguintes somam
a partir daí.

**Acceptance Scenarios**:

1. **Given** EVs atuais, **When** o jogador toca "Editar EVs", **Then** vê os 6 campos já preenchidos
   com os totais atuais (teclado numérico, 0–255, soma até 510).
2. **Given** valores novos e a opção "Manter histórico" (padrão), **When** salva, **Then** os totais
   passam a ser exatamente os informados, o histórico continua com todas as batalhas e ganha um item
   "Ajuste manual" no fim; o aviso oferece Desfazer.
3. **Given** um ajuste manual no histórico, **When** o jogador registra novas batalhas, **Then** elas
   somam a partir dos valores ajustados, com as regras normais (limites, vitaminas até 100, Pokérus).
4. **Given** a opção "Apagar histórico e começar destes valores", **When** o jogador toca salvar,
   **Then** o app avisa quantas batalhas serão apagadas, que as contagens `×N` e os Recentes serão
   zerados e sugere exportar backup antes; só apaga se o jogador confirmar; o aviso final oferece
   Desfazer.
5. **Given** valores fora dos limites (acima de 255, negativos ou soma acima de 510), **Then** salvar
   fica desabilitado com a explicação.
6. **Given** um ajuste manual no histórico, **When** o jogador o remove (×), **Then** os totais voltam
   a ser calculados só pelas batalhas, com Desfazer.

---

### User Story 2 - Informar IVs conhecidos (Priority: P2)

O jogador sabe o IV real de um ou mais stats (de outra fonte). Ele abre "IVs conhecidos" na aba IVs e
digita os valores que conhece; a linha Final passa a mostrar o valor informado e o app indica quando
ele contradiz as anotações de nível. Nada do histórico é apagado.

**Why this priority**: resolve o caso em que as anotações não bastam ou o jogador já tem a resposta.

**Independent Test**: informar IV de Speed = 30 ⇒ a linha Final mostra "30" marcado como informado;
informar 10 quando as anotações dizem 26–30 ⇒ marcação de conflito.

**Acceptance Scenarios**:

1. **Given** a aba IVs, **When** o jogador abre "IVs conhecidos", **Then** vê 6 campos (vazios = não
   sabe), aceitando 0–31.
2. **Given** um IV informado, **Then** a linha Final mostra esse valor com uma marca de "informado" em
   vez da faixa calculada; os stats não informados continuam calculados.
3. **Given** um IV informado fora da faixa calculada pelas anotações, **Then** a célula mostra também o
   aviso "não bate com as anotações", sem bloquear.
4. **Given** um campo apagado, **Then** aquele stat volta a ser calculado.

---

### User Story 3 - Editar uma anotação de nível (Priority: P3)

Em vez de remover e anotar de novo, o jogador toca numa linha da tabela de IVs, corrige nível, espécie,
stats ou EVs daquela anotação e salva.

**Why this priority**: conforto; o mesmo resultado já é possível removendo e anotando de novo.

**Independent Test**: editar o stat de HP da anotação do nível 21 ⇒ a linha e a linha Final recalculam;
Desfazer volta ao valor anterior.

**Acceptance Scenarios**:

1. **Given** a tabela de IVs, **When** o jogador toca "Editar" numa linha, **Then** o formulário abre
   preenchido com os valores daquela anotação (não com os EVs atuais).
2. **Given** a edição salva, **Then** a anotação é substituída (reordenada se o nível mudou) e um aviso
   oferece Desfazer.

---

### Edge Cases

- Ajuste manual igual aos totais atuais: nada é gravado (aviso "Nada mudou").
- Vários ajustes manuais seguidos: vale o último na ordem do histórico.
- Ajuste manual seguido de remoção de batalhas antigas: os totais após o ajuste não mudam (o ajuste
  fixa o valor naquele ponto).
- Backup exportado com ajustes manuais e IVs informados importa de volta idêntico; backups antigos
  continuam válidos (campos ausentes = nada informado).
- Anotação de nível editada para uma espécie fora da linha evolutiva: não permitido (lista fechada).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Usuários DEVEM poder definir os 6 EVs do Pokémon ativo (0–255 cada, soma ≤ 510) a partir
  de um formulário preenchido com os totais atuais.
- **FR-002**: Ao salvar, o usuário DEVE escolher entre "Manter histórico" (padrão) e "Apagar histórico
  e começar destes valores".
- **FR-003**: "Manter histórico" DEVE acrescentar ao fim do histórico um item de ajuste manual que fixa
  os totais nos valores informados naquele ponto; batalhas posteriores somam normalmente a partir dele.
- **FR-004**: "Apagar histórico" DEVE exigir confirmação explícita depois de um aviso com o número de
  itens que serão apagados, o efeito em contagens e Recentes e a sugestão de exportar backup; sem
  confirmação, nada muda.
- **FR-005**: Toda edição manual (ajuste, apagar histórico, IVs informados, anotação editada) DEVE
  oferecer Desfazer.
- **FR-006**: O ajuste manual DEVE aparecer no histórico como "Ajuste manual" com os valores, e poder
  ser removido como qualquer item.
- **FR-007**: Usuários DEVEM poder informar o IV conhecido (0–31) de qualquer stat, ou deixá-lo em
  branco; o valor informado prevalece na linha Final, com marca de "informado" e aviso quando contradiz
  as anotações.
- **FR-008**: Usuários DEVEM poder editar uma anotação de nível existente.
- **FR-009**: Ajustes manuais e IVs informados DEVEM ser salvos no aparelho e incluídos no backup, que
  continua aceitando arquivos antigos.
- **FR-010**: Todo texto novo DEVE vir dos recursos de idioma.

### Key Entities

- **Ajuste manual de EVs**: item do histórico com os 6 valores-alvo; fixa os totais naquele ponto.
- **IVs conhecidos**: até 6 valores (0–31) informados pelo usuário por Pokémon.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Corrigir os EVs para os valores do jogo leva menos de 30 segundos e no máximo 8 toques
  (abrir, digitar valores, salvar).
- **SC-002**: 0 casos de histórico apagado sem confirmação explícita do usuário.
- **SC-003**: Depois de um ajuste, os totais mostrados são exatamente os informados em 100% dos casos
  válidos.
- **SC-004**: Backups com e sem os novos dados importam e exportam sem perda (ida e volta idêntica).

## Assumptions

- O usuário pode conhecer os EVs reais por outra fonte (ex.: contagem própria) e os IVs por outra
  ferramenta; o app não valida a origem.
- "Apagar histórico" afeta só o Pokémon ativo e só as batalhas (anotações de nível ficam).
