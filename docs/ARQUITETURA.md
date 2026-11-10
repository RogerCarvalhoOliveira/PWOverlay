# Arquitetura

## Visão geral

```text
elementclient em foco
        |
        v
Monitor de janelas Win32
        |
        +--> associação manual, identidade por memória ou título Personagem[CLASSE]
        +--> recusa títulos genéricos, repetidos e perfis duplicados
        +--> integra PW Helper somente quando estiver presente
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
- Oferece desinstalação da instalação corrente, sem remover perfis, e saída cooperativa para o instalador.

### `AppShutdown.cs` / `ToolMaintenance.cs` / `MaintenanceProgram.cs`

- Eventos de saída derivados da pasta de instalação evitam sinalizar outra cópia da Central/overlay.
- O auxiliar verifica o caminho completo dos processos antes de encerrar apenas executáveis conhecidos da pasta alvo. Não inclui elementclient nem PW Helper.
- A atualização solicita a restauração do monitor sem bordas, aguarda saídas cooperativas e usa encerramento de compatibilidade para versões antigas/sem resposta. Só continua se os arquivos estiverem liberados.
- Instalador e desinstalador usam uma cópia temporária do auxiliar; perfis em LOCALAPPDATA não pertencem à lista de arquivos removidos.

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
- `PlayerIdentityMemory.cs` lê ID, nome UTF-16 e classe no objeto do jogador e confirma duas amostras iguais antes de publicar a identidade. Essa identificação permanece ativa com o avanço de missões desativado.
- Mantêm sessões independentes por PID; somente o perfil do cliente ativo avança no guia.
- Falhas de leitura descartam o snapshot e as transições antigas. Mudança do ponteiro do jogador reabre a sessão; buscas que falham têm intervalo de dois segundos para não repetir a varredura a cada tick.
- Um token identifica cada sessão de leitura. A troca desse token invalida apenas a associação manual da janela correspondente. Ao associar outro perfil, a sessão de leitura anterior é descartada.

### `ClientIdentity.cs` / `ClientProfilesForm.cs`

- O jogo é enumerado por processo `elementclient`, sem depender de título, Helper ou arquivos dele.
- A associação manual usa PID + início do processo + HWND somente em memória; o progresso usa NOME|CLASSE e continua persistido.
- A interface lista individualmente clientes de títulos iguais, permite nome/classe e reutiliza perfis salvos. Não renomeia janelas nem grava memória do jogo.
- O resolvedor recusa títulos genéricos/duplicados e dois clientes associados ao mesmo perfil. Ações manuais e avanço automático conferem a identidade do cliente antes de alterar o progresso.
- Um personagem sem título reconhecível é identificado pela memória. Quando a leitura está indisponível ou se deseja um nome de perfil personalizado, a associação manual continua disponível. A origem/classe detectada aparece na dica da coluna Classe.

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

O PID, início do processo e HWND identificam a janela apenas durante a execução; não se restaura um PID salvo. Nome/classe detectados por memória retomam o perfil automaticamente; associações manuais personalizadas são refeitas ao reiniciar. Progresso e cultivo são restaurados pelo perfil identificado.

As associações por título anteriores continuam em `ClassAssociations` para compatibilidade e têm precedência sobre a sigla do título. A associação manual da sessão tem prioridade, seguida da identidade por memória e do título. Títulos padrão ou repetidos não geram um perfil compartilhado. A mudança de identidade por memória invalida as evidências anteriores e o token da sessão. Consulte `IDENTIDADE-MEMORIA.md` para offsets, migração de nomes e validação pendente.

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
