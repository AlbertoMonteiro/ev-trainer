# Feature Specification: Sincronização com Gist privado

**Feature Branch**: `008-gist-sync`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "Quero avaliar as possibilidades gratuitas que poderia permitir usar o dado
de forma que permita eu usar entre meu celular e meu pc" → constituição v1.3.0: "permitir dados salvos
externamente, a primeira versão são com gist privado, mas vai ter uma futura versão com Google drive".

**Contexto**: os dados de treino ficam só no aparelho; para usar o celular e o PC hoje é preciso
exportar o backup num e importar no outro, à mão, e uma importação pode sobrescrever trabalho mais
novo. A constituição (v1.3.0, Princípio V) passou a permitir armazenamento externo opcional no espaço
do próprio usuário. Esta feature é o primeiro provedor: um Gist secreto na conta GitHub do jogador.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Conectar o app ao meu Gist (Priority: P1)

O jogador abre **Sincronização** (no menu), segue o passo a passo para gerar um token do GitHub com
permissão apenas de gists, cola o token e toca **Conectar**. O app confirma a conta ("Conectado como
@usuario"), cria um Gist secreto "EVs Trainer" (ou encontra o que já existe nessa conta) e envia os
dados deste aparelho. A partir daí o app mostra quando foi a última sincronização. **Desconectar**
apaga o token deste aparelho; o Gist continua na conta.

**Why this priority**: sem conectar não existe sincronização; sozinha já entrega um backup automático
fora do aparelho.

**Independent Test**: com 2 Pokémon no aparelho, colar um token válido e conectar ⇒ status "Conectado
como @usuario", um Gist secreto com os dois Pokémon no formato do backup; token inválido ⇒ mensagem
clara e nada é enviado; desconectar ⇒ o token some deste aparelho e o app volta a funcionar só local.

**Acceptance Scenarios**:

1. **Given** o app sem sincronização, **When** o jogador abre **Sincronização**, **Then** vê o que a
   sincronização faz, o aviso de que um Gist secreto não é privado de verdade (quem tiver o link
   consegue ler), o passo a passo para criar o token só com permissão de gists e o campo do token.
2. **Given** um token válido, **When** o jogador toca **Conectar**, **Then** o app mostra a conta
   conectada, cria o Gist secreto (ou reaproveita o existente da mesma conta) e sincroniza.
3. **Given** um token inválido, expirado ou sem permissão de gists, **When** o jogador conecta,
   **Then** vê uma mensagem que diz qual é o problema e o token não é guardado.
4. **Given** a sincronização ativa, **When** o jogador toca **Desconectar** e confirma, **Then** o
   token é apagado deste aparelho, os dados locais continuam intactos e o Gist continua na conta.
5. **Given** a sincronização ativa, **Then** o token nunca aparece na tela depois de salvo, nem no
   backup exportado, nem em endereços.

---

### User Story 2 - Usar os mesmos dados no celular e no PC (Priority: P1)

O jogador conecta o celular e o PC à mesma conta. Ao sincronizar, cada aparelho envia o que mudou
e recebe o que o outro mudou: um Pokémon treinado no celular aparece atualizado no PC, e um novo
Pokémon criado no PC aparece no celular. Quando o mesmo Pokémon foi alterado nos dois, vale a
versão mais recente, e o app diz o que mudou.

**Why this priority**: é o objetivo da feature — continuar o treino em qualquer aparelho sem perder
nada.

**Independent Test**: celular e PC conectados; registrar 5 batalhas do Blastoise no celular e
sincronizar; no PC, sincronizar ⇒ o Blastoise do PC passa a ter as 5 batalhas; criar um Kirlia no PC
e sincronizar; no celular, sincronizar ⇒ o Kirlia aparece; nenhum dado mais novo é perdido.

**Acceptance Scenarios**:

1. **Given** um Pokémon alterado só num aparelho, **When** os dois sincronizam, **Then** ambos ficam
   com a versão alterada.
2. **Given** o mesmo Pokémon alterado nos dois aparelhos, **When** sincronizam, **Then** fica a
   versão alterada por último em cada Pokémon, e o app informa quantos Pokémon foram atualizados com
   dados do outro aparelho.
3. **Given** Pokémon que só existem num dos lados, **When** sincroniza, **Then** eles passam a
   existir nos dois (nada é descartado por estar só de um lado).
4. **Given** um Pokémon apagado num aparelho, **When** os dois sincronizam, **Then** a exclusão é
   levada para o outro aparelho e o Pokémon some dos dois — salvo se ele tiver sido alterado no
   outro aparelho **depois** da exclusão (aí vale a alteração, pela mesma regra do mais recente).
   O app informa quantos Pokémon foram removidos. (Clarificação 2026-10-11: Q1 = A.)
5. **Given** a sincronização ativa, **Then** ela acontece **automaticamente**: ao abrir o app, ao
   voltar para ele e alguns segundos depois de cada mudança (agrupando mudanças seguidas, como várias
   batalhas em sequência); o botão **Sincronizar agora** continua disponível.
   (Clarificação 2026-10-11: Q2 = A.)
6. **Given** um aparelho sem internet, **When** o jogador treina, **Then** tudo continua funcionando
   e salvo no aparelho; as mudanças são enviadas na próxima sincronização com conexão.

---

### User Story 3 - Saber o estado da sincronização (Priority: P2)

O indicador de status, que hoje diz "salvo neste aparelho", passa a mostrar também a sincronização:
sincronizado (com o horário), sincronizando, alterações pendentes ou erro (sem internet, token
revogado, limite do GitHub), com uma ação clara para resolver.

**Why this priority**: sem saber se sincronizou, o jogador não confia em trocar de aparelho; mas a
sincronização em si (US1/US2) já funciona sem isso.

**Independent Test**: com a sincronização ativa, desligar a internet e registrar uma batalha ⇒
status "alterações pendentes"; religar e sincronizar ⇒ "sincronizado às HH:MM"; revogar o token no
GitHub ⇒ status de erro com a ação "Reconectar".

**Acceptance Scenarios**:

1. **Given** a sincronização ativa e tudo enviado, **Then** o status mostra "sincronizado" e o
   horário da última sincronização.
2. **Given** mudanças ainda não enviadas, **Then** o status mostra "alterações pendentes".
3. **Given** um erro (sem internet, token revogado ou sem permissão, limite do GitHub), **Then** o
   status mostra o erro em palavras simples e o que fazer; os dados locais nunca são perdidos.
4. **Given** a sincronização desligada, **Then** o status continua como hoje ("salvo neste
   aparelho").

---

### Edge Cases

- **Primeira conexão com dados nos dois lados** (o Gist já tem dados de outro aparelho e este aparelho
  também): os dois conjuntos são combinados pela mesma regra da US2, sem descartar nada; o app
  informa o resultado.
- **Dois aparelhos sincronizando ao mesmo tempo**: o segundo a enviar percebe que o Gist mudou desde
  a última leitura e combina de novo antes de enviar, para não apagar o envio do outro.
- **Relógios diferentes** entre celular e PC: a regra "mais recente" usa o horário de cada aparelho;
  diferenças de alguns minutos podem decidir errado num conflito simultâneo — aceito (uso pessoal,
  conflitos raros).
- **Gist apagado ou editado à mão no GitHub**: o app trata conteúdo ausente ou inválido como vazio,
  avisa e recria a partir dos dados locais; nunca apaga os dados locais por causa disso.
- **Token revogado no GitHub**: erro com a ação "Reconectar"; os dados locais seguem intactos.
- **Vários Gists "EVs Trainer" na conta**: o app usa sempre o mesmo (o mais antigo criado por ele).
- **Pokémon apagado por engano**: o Desfazer continua valendo no aparelho onde foi apagado; desfazer
  conta como alteração mais recente e o Pokémon volta nos dois aparelhos na sincronização seguinte.
- **Aparelho que ficou meses sem sincronizar** (mais que o prazo do registro de exclusões): um
  Pokémon apagado nos outros aparelhos pode voltar a aparecer; aceito pelo prazo de 90 dias.
- **Backup importado com a sincronização ativa**: os Pokémon importados contam como alterados
  agora e entram na próxima sincronização.
- **Limite de requisições do GitHub**: com uso normal não é atingido; se for, o app avisa e tenta
  depois, sem perder nada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app DEVE oferecer uma tela **Sincronização**, acessível pelo menu, com explicação, o
  aviso de que um Gist secreto pode ser lido por quem tiver o link, o passo a passo para criar um
  token só com permissão de gists e os controles de conectar e desconectar.
- **FR-002**: A sincronização DEVE vir desligada; sem ela, o app funciona exatamente como hoje.
- **FR-003**: Ao conectar, o app DEVE validar o token, mostrar a conta GitHub conectada e usar um
  único Gist secreto "EVs Trainer" dessa conta (criando-o se não existir).
- **FR-004**: O token DEVE ficar guardado só neste aparelho; NÃO DEVE aparecer na tela após salvo,
  no backup exportado, em endereços nem em mensagens de erro; **Desconectar** DEVE apagá-lo.
- **FR-005**: O conteúdo do Gist DEVE estar no mesmo formato do backup (um arquivo legível, que
  também pode ser importado à mão).
- **FR-006**: Cada Pokémon DEVE registrar quando foi alterado pela última vez; a sincronização DEVE
  combinar os dados por Pokémon: alterado só de um lado ⇒ vale esse lado; alterado nos dois ⇒ vale o
  mais recente; existente só de um lado ⇒ passa a existir nos dois.
- **FR-007**: A sincronização NUNCA DEVE sobrescrever uma versão mais nova de um Pokémon com uma mais
  antiga, nem perder dados locais por falha de rede, token inválido ou conteúdo inválido no Gist.
- **FR-008**: Exclusões de Pokémon DEVEM ser levadas para os outros aparelhos: o app registra cada
  exclusão com data e hora e a sincroniza; na combinação, exclusão e alteração do mesmo Pokémon seguem
  a regra do mais recente (alteração depois da exclusão mantém o Pokémon). O registro de uma exclusão
  é mantido por pelo menos 90 dias.
- **FR-009**: Com a sincronização ativa, o app DEVE sincronizar automaticamente ao abrir, ao voltar a
  ficar visível e alguns segundos (até ~5 s) depois da última de uma sequência de mudanças, sem
  bloquear o uso; DEVE existir também **Sincronizar agora**. Sem internet, a sincronização automática
  apenas marca "alterações pendentes" e tenta de novo depois.
- **FR-010**: Após cada sincronização que trouxer mudanças do outro aparelho, o app DEVE informar
  quantos Pokémon foram atualizados, adicionados (e removidos, se aplicável).
- **FR-011**: O indicador de status DEVE mostrar, com a sincronização ativa: sincronizado (com
  horário), sincronizando, alterações pendentes ou erro com a ação recomendada.
- **FR-012**: Todos os textos novos DEVEM vir dos recursos de idioma; a tela DEVE funcionar em 360 px
  e no desktop.
- **FR-013**: Preferências do aparelho (tema, idioma, filtro, Pokémon selecionado) e as notas de
  captura (melhor até agora, importância dos stats) NÃO entram na sincronização nesta versão.

### Key Entities

- **Conexão de sincronização** (só no aparelho): provedor (Gist), token, conta conectada, Gist em uso,
  horário da última sincronização, estado (sincronizado / pendente / erro).
- **Pokémon de treino** (existente): ganha "alterado em" (data e hora da última mudança local).
- **Conteúdo sincronizado**: o mesmo documento do backup, com os "alterado em" de cada Pokémon e o
  registro de exclusões (Pokémon e data e hora da exclusão).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Conectar pela primeira vez (gerar o token seguindo o passo a passo e colar) leva menos
  de 3 minutos.
- **SC-002**: Uma alteração feita num aparelho aparece no outro em, no máximo, uma sincronização de
  cada lado.
- **SC-003**: Em 100% dos cenários de teste de conflito, nenhuma versão mais nova de um Pokémon é
  substituída por uma mais antiga.
- **SC-004**: Com a internet desligada, 100% das funções do app continuam funcionando; nenhuma
  mudança feita offline é perdida ao reconectar.
- **SC-005**: O token não aparece em nenhum backup exportado, endereço, mensagem ou registro (verificado
  por teste).
- **SC-006**: Uma sincronização típica (até 20 Pokémon) termina em menos de 3 segundos numa conexão
  móvel comum.
- **SC-007**: Com a sincronização automática, trocar de aparelho não exige nenhum toque para ver os
  dados atualizados (desde que os dois tenham internet).

## Assumptions

- Gist **secreto** = não listado publicamente, mas legível por quem tiver o link; os dados de treino
  de Pokémon são de baixa sensibilidade, e o aviso deixa isso claro (constituição v1.3.0, Princípio V).
- O jogador tem (ou cria) uma conta GitHub; criar o token é feito por ele no site do GitHub, seguindo
  o passo a passo do app (com o link já pedindo só a permissão de gists).
- O token é do tipo pessoal, colado pelo jogador; não há login com redirecionamento (exigiria servidor
  do app, vedado pelo Princípio I).
- Um único Gist por conta; vários aparelhos da mesma conta compartilham o mesmo Gist.
- Preferências e notas de captura ficam fora desta versão (FR-013); podem entrar numa versão futura.
- O provedor fica atrás de uma interface própria (Princípio V), para o Google Drive futuro entrar
  sem mudar esta feature.
- Arquivos de backup antigos (sem "alterado em") continuam sendo aceitos; Pokémon sem data contam como
  "nunca alterados" e perdem para qualquer versão datada.
