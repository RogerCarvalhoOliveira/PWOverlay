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

- Salva escolhas e caminho do launcher em `%LOCALAPPDATA%\PWQuestOverlay\launcher.json`.
- Inicia ou encerra Overlay e PW Borderless sem interferir nos clientes do jogo.
- Permite abrir o launcher configurado sem nenhuma ferramenta.

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
| WR | `WR` | Nenhum. |
| WB | `WBWF` | Somente `WB/WF`. |
| WF | `WBWF` | `WB/WF` e `WF`. |
| EA | `EAEP` | Nenhum. |
| EP | `EAEP` | Nenhum. |
| MC | `MC` | Nenhum. |

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
- associações entre título da janela e classe;
- missões e etapas `done` ou `skipped` (as etapas usam a chave `ID_DA_MISSAO|STEP|ID_DA_ETAPA`);
- posição por monitor;
- bloqueio de posição;
- transparência;
- estado oculto.

A gravação usa arquivo temporário antes de substituir o estado principal.
