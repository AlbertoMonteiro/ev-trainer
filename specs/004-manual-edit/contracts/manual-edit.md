# Contract: edição manual (backup, domínio e UI)

## Backup / localStorage (campos novos, opcionais)

```json
{
  "events": [
    { "o": "Caterpie", "n": 8, "m": 1 },
    { "o": "", "n": 1, "m": 1, "set": [12, 0, 2, 26, 0, 4] }
  ],
  "knownIvs": [null, null, null, null, null, 30]
}
```

- Leitura: `set` com tamanho ≠ 6 ⇒ completa/trunca para 6 e limita cada valor a 0–255; `knownIvs`
  ausente ⇒ 6 `null`; valores fora de 0–31 ⇒ `null`.
- Ida e volta: `Read(Write(x)) == x` (SC-004).

## Domínio

```csharp
public sealed class BattleEvent { /* … */ public int[]? SetTotals { get; set; } public bool IsManualAdjustment => SetTotals is not null; }
public sealed class TrainingPokemon { /* … */ public int?[] KnownIvs { get; set; } = new int?[6]; }
public sealed record FinalKnown(int Value, bool MatchesNotes) : FinalCell;
```

## UI (pt-BR, todas via chaves de idioma)

| Onde | Elemento | Comportamento |
|---|---|---|
| Aba EVs, card de resumo | botão **Editar EVs** (`Evs.Edit`) | abre `EvEditor` com os totais atuais |
| `EvEditor` | 6 campos numéricos, soma ao vivo, opção **Manter histórico** (padrão) / **Apagar histórico e começar destes valores**, **Salvar** | validação 0–255 e soma ≤ 510 |
| `EvEditor`, etapa de confirmação | `role="alertdialog"`: "Isso apaga N batalhas…", botão **Exportar backup**, **Apagar N batalhas** (danger), **Cancelar** | só apaga no segundo toque |
| Histórico recente | item **Ajuste manual · 12/0/2/26/0/4** com × | remover com Desfazer |
| Aba IVs | seção recolhível **IVs conhecidos**: 6 campos 0–31 (vazio = não sei), **Salvar** | linha Final mostra o valor com `*` (legenda "IV informado"; "informado" para leitores de tela) e aviso se não bate |
| Tabela de IVs | o rótulo **Nv N** de cada linha é o botão de editar (dica "Toque no nível para editar a anotação") | abre o formulário de anotação em modo edição (foco no campo Nível), **Salvar alterações** / **Cancelar edição** |
