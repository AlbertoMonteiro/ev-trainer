# Research: Planejador da Elite Four

Fonte: pret/pokefirered@037335f4c725d7c9aecdac87066f2002b4bd7e14.

## R1 — Times da Liga

- `src/data/trainers.h`: `TRAINER_ELITE_FOUR_{LORELEI,BRUNO,AGATHA,LANCE}` (primeira luta) e `..._2` (revanche);
  `TRAINER_CHAMPION_FIRST_{SQUIRTLE,BULBASAUR,CHARMANDER}` e `TRAINER_CHAMPION_REMATCH_*`. Todos com
  `ITEM_CUSTOM_MOVES`: espécie, nível, item e 4 golpes fixos em `src/data/trainer_parties.h`.
- O sufixo do Campeão é o **inicial do rival**. `PalletTown_ProfessorOaksLab/scripts.inc`: jogador Bulbasaur → rival
  Charmander; Squirtle → Bulbasaur; Charmander → Squirtle. O plano guarda o inicial do **jogador**.
- Os times são iguais em FireRed e LeafGreen (sem `#ifdef` nesses trechos).

## R2 — Habilidade dos Pokémon de treinador

`CreateNPCTrainerParty` (`src/battle_main.c`): `personality = 0x88 | 0x78 | 0x80` (byte baixo par) `+ (nameHash << 8)`;
o slot de habilidade é `personality & 1` = 0. **Todo Pokémon de treinador usa a primeira habilidade da espécie.**

## R3 — Golpes e dano

- `src/data/battle_moves.h`: `effect`, `power`, `type`, `accuracy`, `pp`. Poder 0 = status. Poder 1 = dano especial:
  - **fixo**: `EFFECT_DRAGON_RAGE`, `EFFECT_SONICBOOM`, `EFFECT_LEVEL_DAMAGE` (Seismic Toss, Night Shade),
    `EFFECT_SUPER_FANG`, `EFFECT_PSYWAVE`, `EFFECT_ENDEAVOR`, `EFFECT_OHKO`; acertam neutro salvo imunidade de tipo;
  - **variável**: o resto (Return, Frustration, Low Kick, Magnitude, Present, Flail, Reversal, Hidden Power, Counter,
    Mirror Coat, Bide…). Efetividade pelo tipo; poder nominal para a sugestão: Return/Frustration 102, Low Kick 60,
    Magnitude 71, Hidden Power 70 (tipo Normal, spec); os demais não são sugeridos.
- Categoria Gen 3 por tipo: físicos Normal, Fighting, Flying, Poison, Ground, Rock, Bug, Ghost, Steel; especiais Fire,
  Water, Grass, Electric, Psychic, Ice, Dragon, Dark.
- Nomes: do identificador `MOVE_THUNDER_PUNCH` → "Thunder Punch" (com hífens oficiais: Double-Edge, Self-Destruct,
  Soft-Boiled, Will-O-Wisp, Lock-On, Mud-Slap); habilidades idem (`ABILITY_VOLT_ABSORB` → "Volt Absorb").

## R4 — Aprendizado

- Nível: `level_up_learnsets.h` (`LEVEL_UP_MOVE(lvl, move)`) via `level_up_learnset_pointers.h`.
- TM/HM: `tmhm_learnsets.h` (`TMHM(TM24_THUNDERBOLT)`), número e golpe vêm do nome.
- Tutor: `tutor_learnsets.h` (`TUTOR(MOVE_X)`) + Frenzy Plant/Blast Burn/Hydro Cannon só para Venusaur/Charizard/
  Blastoise (`party_menu.c`).
- Espécies da decompilação mapeadas pelo nome normalizado para o Pokédex do app (como `BuildVsSeekerGuide`).

## R5 — Efetividade com habilidades

Defensor: Levitate anula Ground; Water Absorb anula Water; Volt Absorb anula Electric; Flash Fire anula Fire; Thick Fat
× ½ em Fire e Ice; Wonder Guard só recebe super efetivo. Golpe de dano fixo: ignora fraquezas/resistências, respeita
imunidade de tipo (Normal/Fighting × Ghost, Ghost × Normal, Ground × Flying, Electric × Ground) e as habilidades acima.

## R6 — Sugestão

Guloso determinístico sobre as vagas livres (não fixadas) dos membros escolhidos. A cada rodada escolhe o par
(membro, golpe aprendível, ainda não no membro) que mais melhora, lexicograficamente: (1) nº de adversários com golpe
super efetivo; (2) nº com golpe ≥ neutro; (3) soma, por adversário, do melhor "valor" da equipe
(efetividade × força); empate: maior força do golpe para o membro, depois nome. Força = poder × precisão/100 ×
1,5 (mesmo tipo) × (atributo base da categoria ÷ maior entre Ataque e Sp. Atk base) × ½ para golpes com turno de
carga/recarga (Hyper Beam, Solar Beam, Frenzy Plant…) × ¾ com recuo (Double-Edge, Take Down, Submission). Golpes de
dano fixo/variável sem poder nominal não entram, nem os que desmaiam o usuário ou dependem de sono/condição (Explosion,
Self-Destruct, Dream Eater, Snore, Focus Punch, Fake Out, Spit Up). Na sugestão da equipe toda, golpes não fixados
são refeitos; na de um membro, só os dele. Para quando não houver vaga ou candidato.

## R7 — Sincronização do plano

O documento do Gist ganha a seção opcional `"plan": { ...LeaguePlan, "updatedAt": ISO }`. Merge: vence o
`updatedAt` mais recente; empate → maior JSON (ordinal), como os Pokémon; ausência perde para presença. Versões antigas
do app que reescrevam o Gist descartam a seção (o PWA se atualiza sozinho; risco aceito). O backup em arquivo não leva
o plano.
