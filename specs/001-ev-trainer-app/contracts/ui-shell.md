# Contract: interface, armazenamento e interop

Contrato observável da UI (verificado com bUnit) e das fronteiras com o browser. O layout visual
detalhado segue HANDOFF §5; aqui ficam os pontos que mudam em relação a ele e os que os testes
verificam.

## Estrutura de tela (rota única `/`)

- **Header fixo**: botão ☰ (`aria-label="Menu"`), título "EVs Trainer", indicador de status,
  botões alternáveis Pokérus e Macho Brace (`aria-pressed`; mostram "×2" quando ativos).
- **Menu ☰**: Pokémon por `Order` com `total/510` e marcador no selecionado; "+ Adicionar
  Pokémon"; "Exportar backup"; "Importar backup"; "Tema: Sistema / Claro / Escuro"; "Idioma"
  (só aparece com 2+ idiomas disponíveis). **Não** há
  "Tela cheia" (substituída pelo app instalável). Fecha ao escolher, ao tocar fora e com Esc.
- **Abas**: EVs · IVs · Ajustes (estado do componente, sem rotas).
- **Estado vazio**: "Nenhum Pokémon ainda." + "Adicionar Pokémon" + "Importar backup".

## Indicador de status (substitui o de sincronização)

| Estado | Emoji | Texto (tooltip e `aria-label`) |
|---|---|---|
| Salvo | 📱 | "Salvo neste aparelho" |
| Salvando | 🔄 | "Salvando…" |
| Erro | ⚠️ | "Não foi possível salvar neste aparelho. Exporte um backup." |

## Avisos (toast)

Fixo embaixo, some em 4 s, botão "Desfazer" opcional. Mensagens-chave:
`"<Nome>: +1 HP"`, `"<Nome>: não deu EV (limite atingido)"`, `"Backup exportado"`,
`"Importado: X novos, Y substituídos"`, `"<Nome> apagado"` (com Desfazer).

## Abas EVs, IVs, Ajustes

Como HANDOFF §5.2–§5.4. Em Ajustes há também a seção **Backup**: "Exportar backup",
"Importar backup" (seletor de arquivo `.json` → prévia → "Confirmar importação") e o texto "Seus
dados ficam só neste aparelho. Exporte um backup para não perdê-los."

## `IPokemonRepository` (fronteira de armazenamento)

```csharp
interface IPokemonRepository
{
    IReadOnlyDictionary<string, TrainingPokemon> LoadAll();               // vazio se nada salvo
    SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all);  // grava imediatamente
    UiPreferences LoadPreferences();
    void SavePreferences(UiPreferences prefs);
}
enum SaveResult { Saved, Failed }
```

Implementações: `LocalStorageRepository` (produção, via `IJSInProcessRuntime`) e
`InMemoryRepository` (testes; pode simular `Failed`).

## Chaves do `localStorage`

| Chave | Conteúdo |
|---|---|
| `evtrainer:v1:data` | Documento no [formato de backup](./backup-format.md), JSON compacto. |
| `evtrainer:v1:prefs` | `{ "selectedId": "...", "theme": "system", "filter": "useful", "language": "pt-BR" }` |

Todos os textos citados neste contrato são o conteúdo em pt-BR das chaves de idioma; os
componentes nunca os contêm literalmente (ver [localization.md](./localization.md)).

## Interop JS (`wwwroot/js/interop.js`, único arquivo JS próprio)

| Função | Uso |
|---|---|
| `evt.storageGet(key)` / `evt.storageSet(key, value)` | Retorna string/null; `storageSet` retorna `false` em exceção (cota/modo privado). |
| `evt.downloadText(fileName, text)` | Exportar backup (Blob + `<a download>`). |
| `evt.setTheme(theme)` | Ajusta `data-theme` no `<html>`. |
| `evt.setLanguage(tag)` | Ajusta `lang` no `<html>`. |

O `index.html` tem um script inline que aplica o tema e o `lang` salvos antes do Blazor iniciar
(`<html lang="pt-BR">` por padrão).

## PWA

`manifest.webmanifest`: `name` "EVs Trainer", `short_name` "EVs", `display` "standalone",
`start_url` "./", `scope` "./", cores de `--bg` / `--accent`, ícones 192 e 512. Service worker do
template `--pwa` com cache de todos os assets publicados.
