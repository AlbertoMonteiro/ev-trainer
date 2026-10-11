# UI Contract: Pokémon identificado pela espécie

Contrato observável pelos testes bUnit. Nomes de chaves referem-se a `Localization/strings.pt-BR.json`.

## Ajustes (`SettingsForm` + `SpeciesPicker`)

- Não existe `#settings-name` nem a chave `Settings.Name`.
- `SpeciesPicker` recebe `Line`, `Current` e `OnPick`/`OnCurrent`.
- Cada item da linha (`li.species-chip`) contém um `button.species-current` com
  `aria-pressed="true"` só na forma atual e `aria-label` = `Settings.Species.SetCurrent` ("{0} como forma
  atual"). Tocar dispara `OnCurrent(nome)`.
- Escolher uma espécie na busca ou tocar em `button.evolution-option` define a forma atual do formulário
  como a espécie tocada (`SpeciesPick.Picked`).
- `button.save` habilitado sem nenhum texto digitado, desde que metas e linha sejam válidas.
- Salvar chama `TrainerStore.SaveSettings(current, nature, targets, species)`.

## Resumo de EVs (`EvSummary`)

```html
<div class="card-head">
  <h2 class="summary-who">
    <span role="img" aria-label="EVs de Kirlia">   <!-- Evs.Summary.Label -->
      <span class="sprite" aria-hidden="true" style="--sprite-size: 64px; …">…</span>
    </span>
  </h2>
  <span class="totals num muted">…</span>
</div>
```

- O texto do nome da espécie NÃO aparece como texto visível no `card-head`.
- A chave `Evs.Summary.Title` é removida.
- Sem espécie: o selo mostra "?" e o `aria-label` usa `Pokemon.NoSpecies`.

## Menu (`PokemonMenu`)

- Cada `button.pokemon-item`: sprite de `CurrentSpecies` (40 px), o rótulo do Pokémon (espécie ou
  `Pokemon.NoSpecies`) e, quando houver, a nature em `span.nature`.

## Aba IVs, exclusão e avisos

- `Ivs.Title`, `Delete.Hint`, `Delete.Confirm` e `Toast.PokemonDeleted` recebem o rótulo do Pokémon
  (espécie atual ou `Pokemon.NoSpecies`).
- `LevelForm` pré-seleciona `CurrentSpecies` como espécie de uma anotação nova.

## Chaves de idioma

| Ação | Chave | pt-BR |
|---|---|---|
| remover | `Settings.Name` | — |
| remover | `Evs.Summary.Title` | — |
| nova | `Evs.Summary.Label` | `EVs de {0}` |
| nova | `Settings.Species.SetCurrent` | `{0} como forma atual` |
| nova | `Settings.Species.CurrentHint` | `Toque na forma em que o Pokémon está agora.` |
| nova | `Pokemon.NoSpecies` | `Pokémon sem espécie` |
