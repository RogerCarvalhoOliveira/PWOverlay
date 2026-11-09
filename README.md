# PW Quest Overlay

Overlay portátil para acompanhar rotas de missões do Perfect World 1.4.4 em multiclient.

O programa identifica cada cliente pelo título criado pelo PW Helper, por exemplo `Cauton[EP]`, seleciona a rota da classe e mostra missão, submissão e ação atuais. Cada personagem e classe possui progresso independente.

## Download

Para usar sem compilar, abra a [versão mais recente](https://github.com/RogerCarvalhoOliveira/PWOverlay/releases/latest) e baixe:

- `PWQuestOverlay-Setup-v0.10.0.exe`: instalador recomendado para Windows;
- `PWQuestOverlay-Portatil-v0.10.0.zip`: pacote portátil;
- `SHA256SUMS-v0.10.0.txt`: hashes para conferir a integridade dos arquivos.

Depois da instalação, execute **PW Overlay Central** para escolher se deseja ativar o Overlay e/ou o Windowed Fullscreen antes de abrir o jogo. O botão azul/cinza permite ligar ou desligar o Windowed Fullscreen imediatamente, inclusive com os clientes já abertos.

## Funcionalidades

- Dez classes: `WR`, `MG`, `WB`, `WF`, `EA`, `EP`, `MC`, `ES`, `SK` (Arcano) e `MS` (Místico).
- Detecção automática do padrão `Personagem[CLASSE]`.
- Tela **Perfis** para associar manualmente qualquer título ou nome a uma classe.
- Suporte a personagens com o mesmo nome em classes diferentes.
- Rotas do nível 1 ao 105 para as dez classes, preservando as etapas e o progresso anteriores.
- Título explícito em todas as submissões para facilitar comparação com o questlog e diagnóstico de progresso.
- Ordem global por ação do nível 1 ao 105: o overlay pode pegar várias missões de um polo, executar objetivos próximos e entregá-las somente no retorno planejado.
- Após a Cidade do Dragão, as dez classes percorrem o mesmo circuito e se separam nas missões exclusivas de raça/classe. A rota do Místico inclui as quatro invocações e seis plantas até o nível 59.
- Escolha God/Evil ao chegar à decisão do cultivo 89, salva por personagem. As cadeias de 89, 99 e 100 seguem somente o caminho escolhido.
- Cultivos, DGs 19/29/39/49/59/69/79/89/99, Moedas Sangrentas, Pilar Esquisito e cargos militares prioritários.
- Acessos ao Passado sinalizados como pré-requisito do cultivo 100; Dusk/Crepúsculo continua reservado para outra etapa. Consulte [guia 61–105](docs/GUIA-61-105.md).
- Missões de voo ficam fora da rota; o guia inclui a etapa para abandonar o pop-up no questlog.
- Aviso de produção para falar com o Ancião: nível 5 nas quatro raças anteriores e 6 nos Guardiões, conforme o cliente. Missões específicas de ofício ficam fora.
- Nível previsto da rota exibido em cada missão, sem exigir o nível real do personagem.
- Botões para concluir, voltar e pular uma etapa.
- Tipo da etapa destacado: pegar, falar, matar, coletar, entregar, ir, esperar ou ação.
- Botão **Etapas** para saltar deliberadamente para uma missão/etapa da rota.
- Botão **Marcar** e atalho `Ctrl + Alt + M` (ou `Ctrl + Shift + M` se houver conflito). Uma única ação substitui somente o `PWOverlay` anterior; o modo de apagar todos continua opcional.
- Assistente de calibração do marcador para outras escalas de interface e resoluções.
- Editor visual de rotas e etapas, com sincronização do JSON e da planilha.
- Exportação e restauração de backup dos perfis e do progresso.
- Atalhos globais configurados no código.
- Arraste segurando o botão esquerdo na barra superior; solte para salvar a posição. Bloqueio de posição, transparência e restauração disponíveis.
- Botão `−/+` e menu da bandeja para alternar para uma interface compacta sem perder as ações essenciais.
- Transparência ajustável entre 65% e 100%, com 82% como novo padrão recomendado.
- Posição salva por monitor e resolução.
- Monitor escolhido ao arrastar é preservado, inclusive em uma segunda tela; a opção Acompanhar monitor do PW restaura o modo automático.
- Compatibilidade com o PW Borderless Monitor e os painéis flutuantes do PW Helper.
- **PW Overlay Central** permite abrir o PW com Overlay/Windowed Fullscreen, aplicar as ferramentas depois ou abrir somente o jogo.
- Todos os painéis flutuantes visíveis do PW Helper são mantidos acima do jogo, independentemente do título.
- Ocultação automática quando o usuário muda para outro aplicativo.
- Estado salvo em `%LOCALAPPDATA%\PWQuestOverlay`.

## Limites de segurança

A partir da versão 0.8.0, o overlay oferece sincronização opcional por leitura de memória do cliente The Classic 1.4.4 x86. O recurso começa desligado, nunca escreve no processo do jogo e só avança etapas com evidências estáveis. Etapas sem evidência suportada continuam disponíveis para avanço manual. Consulte [documentação da leitura de memória](docs/MEMORIA-EXPERIMENTAL.md).

A única automação da versão estável dentro do cliente é acionada manualmente pelo botão **Marcar** e reproduz cliques/teclas na janela pública de Marcadores; não movimenta o personagem, luta nem entrega missões.

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
- `PW Overlay Central.exe`
- `PW Memory Probe.exe` (diagnóstico opcional)
- `quest_routes.json`
- `memory_offsets.json`
- `task_catalog_144.tsv`
- `rota_leveling_pw_144.xlsx`
- `LEIA-ME.txt`
- `INSTALACAO.txt`;
- `PWQuestOverlay-Portatil-v0.10.0.zip`, quando `-Package` for usado.

Para gerar também o instalador, instale o Inno Setup 6 e execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package -Installer
```

O resultado adicional é `PWQuestOverlay-Setup-v0.10.0.exe`. Ele instala por usuário, sem exigir privilégios administrativos, e oferece atalhos e inicialização automática como opções desmarcadas.

Para compilar e iniciar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1
```

## Estrutura

| Caminho | Conteúdo |
| --- | --- |
| `src/` | Código C# do aplicativo. |
| `src/PWLauncher.cs` | Inicializador que permite escolher quais ferramentas usar em cada sessão. |
| `src/ProcessMemoryReader.cs` | Leitor externo somente leitura usado pelo modo experimental. |
| `src/QuestMemoryReader.cs` | Leitura validada das listas ativa e histórica e do inventário de missão. |
| `src/QuestStateTracker.cs` | Detecta transições sem confundir ausência com missão nunca aceita. |
| `src/QuestMemoryMonitor.cs` | Mantém sessões estáveis e independentes para cada cliente aberto. |
| `src/TaskCatalog.cs` | Relaciona os IDs da memória aos nomes dataminados do cliente. |
| `data/quest_routes.json` | Rotas e missões usadas pelo overlay. |
| `data/memory_offsets.json` | Perfil versionado de offsets do cliente x86. |
| `data/rota_leveling_pw_144.xlsx` | Planilha editável com cinco abas, uma por raça. Humanos inclui WR/MG; Abissais inclui MC/ES. |
| `data/guardian_quest_facts.json` | Fatos dataminados usados para gerar a rota SK/MS sem precisar instalar o jogo. |
| `data/endgame_quest_facts.json` | Fatos das missões 61–105 e dos caminhos God/Evil. |
| `docs/GUIA-61-105.md` | Escolha do cultivo, organização das DGs, pré-requisitos e manutenção das novas rotas. |
| `scripts/build.ps1` | Compila e empacota o projeto. |
| `installer/PWQuestOverlay.iss` | Definição do instalador para Inno Setup 6. |
| `scripts/validate-data.ps1` | Valida IDs, campos e contagens das rotas. |
| `scripts/test.ps1` | Executa validação e smoke tests das rotas e do estado. |
| `scripts/inspect-state.ps1` | Exibe o progresso local sem alterá-lo. |
| `docs/MANUAL-USUARIO.md` | Manual de uso. |
| `docs/INSTALACAO.md` | Instalação e configuração em outro computador. |
| `docs/ARQUITETURA.md` | Componentes e fluxo do aplicativo. |
| `docs/DESENVOLVIMENTO.md` | Tutorial de criação e manutenção. |
| `docs/ROTAS.md` | Como editar ou adicionar missões. |
| `docs/DATAMINE-MISSOES-1-20.md` | Comparação dos nomes do guia com o `tasks.data` nos níveis 1 a 20. |
| `docs/DATAMINE-MISSOES-21-60.md` | Comparação dos nomes do guia com o `tasks.data` nos níveis 21 a 60. |
| `docs/TESTES.md` | Roteiro de testes, inclusive multiclient. |
| `docs/MEMORIA-EXPERIMENTAL.md` | Escopo, segurança e plano da leitura de estado de missões. |
| `docs/SOLUCAO-DE-PROBLEMAS.md` | Diagnóstico de erros comuns. |
| `docs/RELEASE.md` | Preparação de uma nova versão. |

O repositório reúne os dois componentes: o tracker de missões e o monitor que mantém os clientes em janela sem bordas. Eles são executáveis separados para que um possa ser atualizado sem interromper o outro.

## Arquivos locais

O aplicativo cria:

```text
%LOCALAPPDATA%\PWQuestOverlay\state.json
%LOCALAPPDATA%\PWQuestOverlay\overlay.log
```

Esses arquivos não pertencem ao Git. O primeiro guarda progresso, posição, modo compacto e preferências. O segundo registra inicialização, identificação de clientes e alterações de missão.

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
