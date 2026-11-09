# Arquitetura

## Visão geral

```text
elementclient em foco
        |
        v
Monitor de janelas Win32
        |
        +--> lê título Personagem[CLASSE] ou associação manual
        +--> mantém último cliente durante interação com PW Helper
        +--> controla visibilidade e ordem TOPMOST
        |
        v
Perfil PERSONAGEM|CLASSE
        |
        +--> filtra data/quest_routes.json
        +--> lê estados de missão e de etapa
        |
        +--> botão Marcar envia a coordenada à UI pública do cliente
        v
Overlay WinForms sem ativação
```

## Componentes

### `Program.cs`

- Garante uma única instância com `Local\PWQuestOverlay`.
- Ativa DPI awareness.
- Carrega rotas e estado.
- Inicia o loop WinForms.

### `Models.cs`

- Define o formato das missões.
- Define etapas com título de submissão, ação, coordenada opcional e `routeOrder` global.
- Carrega `quest_routes.json`.
- Filtra missões por classe.
- Lê e grava `%LOCALAPPDATA%\PWQuestOverlay\state.json`.
- Mantém o log local.

### `NativeMethods.cs`

Contém somente a integração Win32 necessária:

- enumeração de janelas;
- identificação de processos;
- título e retângulo das janelas;
- janela em primeiro plano;
- `TOPMOST` sem ativação;
- atalhos globais;
- DPI awareness.
- cliques e teclas usados exclusivamente na janela pública de Marcadores.

Nenhuma função lê memória de outro processo.

### `PWLauncher.cs`

- Salva somente as escolhas Overlay/Windowed Fullscreen em `%LOCALAPPDATA%\PWQuestOverlay\launcher.json`; o nome legado do arquivo foi mantido para preservar as preferências.
- Inicia ou encerra Overlay e PW Borderless sem interferir nos clientes do jogo.
- Aplica as caixas selecionadas pelo botão Aplicar e solicita um encerramento cooperativo do monitor por evento nomeado para restaurar as janelas.
- Não procura executáveis do jogo nem abre clientes: a abertura é manual ou pelo PW Helper.

### `PWBorderlessMonitor.cs`

- Memoriza estilo, estilo estendido, posição, tamanho e estado maximizado antes de modificar cada cliente.
- Ao receber `Local\PWBorderlessDisableEvent`, restaura os estados memorizados e encerra de forma limpa.
- Continua detectando novos clientes enquanto o toggle estiver ativado.

### `OverlayForm.cs`

- Constrói a interface.
- Monitora o foco em intervalos de 100 ms.
- Atualiza a rota quando o cliente muda.
- Processa botões e atalhos.
- Achata as etapas de todas as missões e as ordena por `routeOrder`, permitindo intercalar missões.
- Exibe `Missão › Submissão` e avança/desfaz por ação global; a missão só fecha quando todas as próprias etapas forem resolvidas.
- Cria o marcador único `PWOverlay` no cliente ativo.
- Salva posição e progresso.
- Reeleva o tracker e, em seguida, todos os painéis flutuantes visíveis do PW Helper.
- Exibe a escolha God/Evil na etapa correspondente, salva por personagem e filtra as cadeias seguintes.

### `QuestMemoryMonitor.cs` / `ProcessMemoryReader.cs`

- Consultam o cliente x86 somente para leitura, com layout e catálogo versionados.
- Confirmam duas leituras estáveis antes de atualizar evidências de missões, itens e abates.
- Mantêm sessões independentes por PID; somente o perfil do cliente ativo avança no guia.

### `CultivationChoiceForm.cs` / `CultivationRouting`

- A janela apresenta God e Evil sem escolha inicial automática.
- A preferência fica em `CharacterProgress.CultivationPath`, separada por personagem/classe.
- Sem escolha, a rota pausa na decisão de 89; concluir/pular essa etapa abre a escolha.
- Depois da escolha, missões compartilhadas e somente a cadeia escolhida ficam visíveis.
- O menu permite corrigir a preferência; o jogo continua exigindo a escolha manual no NPC.

## Identidade do perfil

A chave persistente é:

```text
NOME_NORMALIZADO|CLASSE
```

Essa escolha é necessária porque mais de um cliente pode usar o mesmo nome base, como `Cauton[EP]` e `Cauton[WB]`.

O PID e o `HWND` são usados apenas na sessão atual. Eles mudam quando o cliente é reiniciado.

As associações manuais são guardadas em `ClassAssociations`, indexadas pelo título completo normalizado da janela. Elas têm precedência sobre a sigla detectada. Assim, uma janela chamada apenas `Cauton` pode ser associada a `EP`, enquanto títulos diferentes continuam independentes.

## Classes e rotas

| Classe | `routeKey` | Filtro adicional |
| --- | --- | --- |
| WR / MG | `WR` | Nenhum. |
| WB | `WBWF` | Somente `WB/WF`. |
| WF | `WBWF` | `WB/WF` e `WF`. |
| EA | `EAEP` | Nenhum. |
| EP | `EAEP` | Nenhum. |
| MC / ES | `MC` | Nenhum. |
| SK | `SKMS` | `SK/MS` e `SK`. |
| MS | `SKMS` | `SK/MS` e `MS`. |

Cada rota recebe `COMMON`. `cultivationPath` define os ramos God/Evil e `cultivationChoice` identifica a decisão. As etapas continuam ordenadas por `routeOrder`; o filtro não altera os IDs nem as marcações salvas.

## Foco e ordem Z

O overlay usa `WS_EX_NOACTIVATE`, por isso um clique no painel não deve roubar o foco do jogo.

O monitor considera três situações:

1. **Cliente em foco:** troca para o perfil do `elementclient` atual.
2. **PW Helper ou o próprio overlay em foco:** preserva o último cliente ativo.
3. **Outro aplicativo em foco:** oculta o overlay.

A cada elevação:

1. o tracker vai para `TOPMOST`;
2. todos os painéis flutuantes do PW Helper vão para `TOPMOST` por último; a janela principal é ignorada.

Assim o Helper permanece visível sem receber foco de teclado.

## Persistência

`state.json` contém:

- classe manual ou automática;
- caminho de cultivo por personagem (`GOD`, `EVIL` ou ainda não escolhido);
- associações entre título da janela e classe;
- missões e etapas `done` ou `skipped` (as etapas usam a chave `ID_DA_MISSAO|STEP|ID_DA_ETAPA`);
- posição por monitor;
- bloqueio de posição;
- transparência;
- estado oculto.

A gravação usa arquivo temporário antes de substituir o estado principal.
