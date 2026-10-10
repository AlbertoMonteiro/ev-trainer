# Quickstart: validar a edição manual

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

1. **Ajustar mantendo histórico**: com batalhas registradas, aba EVs → **Editar EVs** → mudar
   SpAtk → **Salvar** ⇒ totais exatos; Histórico recente mostra "Ajuste manual"; registrar Caterpie
   soma +1 HP a partir do valor ajustado; **Desfazer** volta ao anterior.
2. **Apagar histórico**: **Editar EVs** → escolher **Apagar histórico…** → **Salvar** ⇒ aparece o aviso
   com o número de batalhas; **Cancelar** não muda nada; repetir e confirmar ⇒ histórico com um único
   ajuste, contagens `×N` zeradas; **Desfazer** restaura tudo.
3. **IVs conhecidos**: aba IVs → **IVs conhecidos** → Speed = 30 ⇒ linha Final mostra 30 "informado";
   Speed = 10 com anotações dizendo 26–30 ⇒ aviso "não bate com as anotações".
4. **Editar anotação**: tabela de IVs → **Editar** no nível 21 → mudar HP → **Salvar alterações** ⇒
   linha e Final recalculadas; **Desfazer** restaura.
5. **Backup**: exportar e importar ⇒ ajustes e IVs informados preservados.
