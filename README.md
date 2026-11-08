# PW Quest Overlay

Overlay portátil para acompanhar rotas de missões do Perfect World 1.4.4 em multiclient.

O programa identifica cada cliente pelo título criado pelo PW Helper, por exemplo `Cauton[EP]`, seleciona a rota da classe e mostra a missão e a etapa atuais. Cada personagem e classe possui progresso independente.

## Funcionalidades

- Classes `WR`, `WB`, `WF`, `EA`, `EP` e `MC`.
- Detecção automática do padrão `Personagem[CLASSE]`.
- Tela **Perfis** para associar manualmente qualquer título ou nome a uma classe.
- Suporte a personagens com o mesmo nome em classes diferentes.
- Rotas otimizadas do nível 1 ao 60, com 226 missões únicas e 836 etapas detalhadas.
- Ordem global por ação: o overlay pode pegar várias missões de um polo, executar objetivos próximos e entregá-las somente no retorno planejado.
- Após a Cidade do Dragão, as seis classes compartilham a mesma sequência e se separam somente nas missões exclusivas de raça.
- Cultivos, DGs 19/29/39/49/59, Moedas Sangrentas, Pilar Esquisito e cargos militares prioritários.
- Missões de voo ficam fora da rota; o guia inclui a etapa para abandonar o pop-up no questlog.
- Missão pop-up do nível 5 para falar com o Ancião sobre forja, sem incluir as missões específicas de ofício.
- Nível previsto da rota exibido em cada missão, sem exigir o nível real do personagem.
- Botões para concluir, voltar e pular uma etapa.
- Tipo da etapa destacado: pegar, falar, matar, coletar, entregar, ir, esperar ou ação.
- Botão **Etapas** para saltar deliberadamente para uma missão/etapa da rota.
- Botão **Marcar** e atalho `Ctrl + Alt + M` (ou `Ctrl + Shift + M` se houver conflito). Uma única ação substitui somente o `PWOverlay` anterior; o modo de apagar todos continua opcional.
- Assistente de calibração do marcador para outras escalas de interface e resoluções.
- Editor visual de rotas e etapas, com sincronização do JSON e da planilha.
- Exportação e restauração de backup dos perfis e do progresso.
- Atalhos globais configurados no código.
- Painel arrastável, bloqueio de posição, transparência e restauração de posição.
- Posição salva por monitor e resolução.
- Compatibilidade com o PW Borderless Monitor e o painel `TODOS` do PW Helper.
- O painel `TODOS` é mantido acima do jogo enquanto estiver aberto.
- Ocultação automática quando o usuário muda para outro aplicativo.
- Estado salvo em `%LOCALAPPDATA%\PWQuestOverlay`.

## Limites de segurança

O projeto não lê memória do jogo, não injeta DLL e não captura pacotes. Ele utiliza títulos e propriedades públicas das janelas do Windows. A única automação dentro do cliente é acionada manualmente pelo botão **Marcar** e reproduz cliques/teclas na janela pública de Marcadores; não movimenta o personagem, luta nem entrega missões.

## Compilação rápida

Requisitos:

- Windows.
- .NET Framework 4.x.
- Compilador `csc.exe` do .NET Framework.
- PowerShell 5 ou superior.

Na raiz do projeto:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
```

O resultado será criado em `dist\`:

- `PW Quest Overlay.exe`
- `PW Borderless.exe`
- `quest_routes.json`
- `rota_leveling_pw_144.xlsx`
- `LEIA-ME.txt`
- `PW Quest Overlay Prototipo.zip`, quando `-Package` for usado.

Para compilar e iniciar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1
```

## Estrutura

| Caminho | Conteúdo |
| --- | --- |
| `src/` | Código C# do aplicativo. |
| `data/quest_routes.json` | Rotas e missões usadas pelo overlay. |
| `data/rota_leveling_pw_144.xlsx` | Planilha editável com as quatro abas e todas as etapas. |
| `scripts/build.ps1` | Compila e empacota o projeto. |
| `scripts/validate-data.ps1` | Valida IDs, campos e contagens das rotas. |
| `scripts/test.ps1` | Executa validação e smoke tests das rotas e do estado. |
| `scripts/inspect-state.ps1` | Exibe o progresso local sem alterá-lo. |
| `docs/MANUAL-USUARIO.md` | Manual de uso. |
| `docs/ARQUITETURA.md` | Componentes e fluxo do aplicativo. |
| `docs/DESENVOLVIMENTO.md` | Tutorial de criação e manutenção. |
| `docs/ROTAS.md` | Como editar ou adicionar missões. |
| `docs/TESTES.md` | Roteiro de testes, inclusive multiclient. |
| `docs/SOLUCAO-DE-PROBLEMAS.md` | Diagnóstico de erros comuns. |
| `docs/RELEASE.md` | Preparação de uma nova versão. |

O repositório reúne os dois componentes: o tracker de missões e o monitor que mantém os clientes em janela sem bordas. Eles são executáveis separados para que um possa ser atualizado sem interromper o outro.

## Arquivos locais

O aplicativo cria:

```text
%LOCALAPPDATA%\PWQuestOverlay\state.json
%LOCALAPPDATA%\PWQuestOverlay\overlay.log
```

Esses arquivos não pertencem ao Git. O primeiro guarda progresso, posição e preferências. O segundo registra inicialização, identificação de clientes e alterações de missão.

## Fluxo Git recomendado

```powershell
git switch -c minha-alteracao
# editar e testar
git add .
git commit -m "Descrição da alteração"
```

Antes do commit:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
```

Leia [DESENVOLVIMENTO](docs/DESENVOLVIMENTO.md) antes de alterar o monitor de foco ou as chamadas Win32.
