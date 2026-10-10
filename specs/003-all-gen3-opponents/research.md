# Research: Adversários de todas as espécies do FireRed

**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Date**: 2026-10-09

## R1. Fonte dos EV yields de #152–386

- **Decision**: valor **atual** de `stats[].effort` de `pokeapi.co/api/v2/pokemon/{id}`, gravado no
  `pokedex-gen3.json` pelo `scripts/BuildPokedex.cs` (que já consulta esse endpoint para os stats
  base). O histórico `past_stats` **não** é aplicado aos EVs.
- **Rationale**: verificação feita durante o planejamento (script descartável sobre as 386 espécies):
  - `effort` atual == `reference-data.json` nos **151/151** de Kanto (curadoria do HANDOFF).
  - Aplicar `past_stats` ao `effort` gera **3 erros** em Kanto: Raichu e Electrode ficariam sem EV
    e Alakazam ganharia 3 SpDef. O campo `effort` dentro de `past_stats` não é confiável (é uma
    cópia de quando só o stat base mudou).
  - Exemplos conferidos: Milotic `+2 SpDef`, Zigzagoon `+1 Speed`, Shedinja `+2 HP`.
- **Risco conhecido**: em 8 espécies de #152–386 (Yanma, Slowking, Misdreavus, Blissey, Delcatty,
  Roselia, Duskull, Dusclops) a PokéAPI registra um `effort` diferente no histórico; pelo mesmo motivo
  acima ele é ignorado. Um teste fixa os valores dessas 8 para que qualquer correção futura seja
  consciente. **Blissey confirmado** (2026-10-09): a PokémonDB mostra "EV yield 3 HP" e a seção
  "Blissey changes" lista só mudanças de experiência base e amizade por geração, sem mudança de EV.
- **Alternatives considered**: aplicar `past_stats` (erra Kanto); digitar a partir de wikis (não
  reproduzível); Bulbapedia por scraping (frágil, licença diferente).

## R2. Onde ficam os dados e como viram adversários

- **Decision**: `pokedex-gen3.json` ganha o campo `evYield` (int[6]) para as 386 espécies.
  `ReferenceData.LoadEmbedded()` lê os dois recursos embutidos e monta a lista de adversários:
  #1–151 do `reference-data.json` (yields curados, locais, treinadores), #152–386 da Pokédex
  (`WildLocation` vazio, sem treinadores) e, no fim, as 6 vitaminas.
- **Rationale**: o `reference-data.json` continua intacto (pacote do HANDOFF, Princípio IV); uma única
  fonte gerada para #152–386; o domínio segue puro (só lê recursos embutidos, como já faz). Todas as
  regras (busca, Úteis, Recentes, contagem, importação, selo do sprite) usam `Opponents`, então passam
  a cobrir as espécies novas sem mudanças.
- **Alternatives considered**: acrescentar 235 entradas ao `reference-data.json` (mistura dado curado
  com gerado e altera o arquivo do HANDOFF); um terceiro arquivo só de yields (duplica a lista de
  espécies).

## R3. Consistência entre as duas fontes

- **Decision**: teste que compara, para #1–151, o `evYield` gerado no `pokedex-gen3.json` com o do
  `reference-data.json` (devem ser idênticos) e garante que todo adversário #1–386 tem yield > 0.
- **Rationale**: SC-002 e FR-002; pega regressões se o gerador for rodado de novo.

## R4. Desempenho com 392 cards

- **Decision**: calcular as contagens `×N` uma vez por renderização (`Evs.CountsByOpponent`, um
  dicionário nome canônico → total) em vez de varrer o histórico por card; manter o carregamento
  preguiçoso dos sprites (já existente).
- **Rationale**: com 392 cards e um histórico longo, o `CountFor` por card seria O(cards × eventos)
  a cada toque; o dicionário torna O(eventos + cards) (SC-003, FR-005).
- **Alternatives considered**: `Virtualize` (só renderiza o visível; ganho desnecessário para ~400
  itens leves e complicaria a grade de 2 colunas e o teste de texto fixo).

## R5. Fora do escopo

- Locais selvagens e treinadores das Sevii Islands / espécies de outras gerações (pendência do
  HANDOFF §7); a busca por nome e número cobre o uso (FR-003, Edge Cases).
- Selo do sprite com 3 cores: algumas espécies dão EV em 3 stats; o selo continua usando as 2 de
  maior rendimento (regra da feature 002).
