# Histórico de alterações

## 0.13.0 - Calibração, nível previsto e rota reduzida

- A calibração abre pelo menu da bandeja e aguarda o PW em foco. Mostra cliente/resolução, captura com F8 global, mantém os pontos no mesmo cliente/tamanho e permite voltar pelo Alt+Tab. Corrigido Restaurar padrão e validação antes de salvar.
- O nível previsto passa a somar as recompensas XP ×3 das missões concluídas do personagem e aplicar a curva do cliente. Etapas puladas e seleção manual de um ponto posterior deixam de gerar XP fictícia. Apresentações duplicadas de uma missão contam uma vez.
- Rota reduzida por padrão nas dez classes. Há 107 missões independentes de XP elegíveis para dispensa. O planejador conserva conteúdo protegido, dependências e missões iniciadas. Antes do próximo requisito de nível, escolhe a maior recompensa disponível. O menu permite exibir o roteiro completo.
- Histórico importado em toda a rota, inclusive ao abrir perfis existentes com o avanço de etapas desativado. Há vínculos de conclusão para 573 entradas do guia. Receber uma moeda de DG comprova a aquisição da moeda. A conclusão da instância exige evidência dos objetivos.
- Consulta do histórico em bloco e cache validado da interface de missões reduzem leituras por cliente. A versão inclui a identificação de nome/classe por memória da 0.12.0.
- Testes locais de calibração em quatro resoluções, previsão de XP, planejamento e restauração nas dez classes. Validação com personagens conectados permanece pendente.

## 0.12.0 - Identificação do personagem por memória

- Nome real UTF-16, ID e classe lidos diretamente do personagem, sem renomear a janela e sem PW Helper. Duas amostras iguais confirmam a identidade; títulos iguais continuam separados por cliente.
- A identificação continua ativa quando o avanço de missões está desativado. Perfis manuais têm prioridade; memória precede o título da janela. A troca de personagem descarta as evidências anteriores.
- Perfis antigos que perderam um sufixo de classe no título são migrados para o nome real quando há correspondência exata de classe, preservando o progresso. Nomes de perfil personalizados continuam disponíveis em Perfis.
- Testes com ReadProcessMemory em processo x86 de teste, troca de ID/logout, dez classes, nomes acentuados, dados inválidos e interface. Offsets inspecionados no executável local; validação com personagens conectados ainda pendente.

## 0.11.1 - Correção da atualização e desinstalação pelo aplicativo

- Corrigido o erro do Restart Manager ao tentar fechar ferramentas em segundo plano. O instalador usa um auxiliar temporário para liberar os executáveis antes da substituição.
- Saída cooperativa do overlay/Central por eventos específicos da instalação; o monitor sem bordas restaura as janelas ao encerrar. Compatibilidade com o evento do monitor antigo e encerramento limitado a executáveis conhecidos na pasta exata da instalação.
- Removida a verificação antecipada de AppMutex no instalador, permitindo que a preparação encerre as ferramentas. Aplicações mantêm seus próprios mutexes.
- Opção Desinstalar programa na Central e na bandeja do overlay. Abre o desinstalador da instalação; a versão portátil informa que não possui desinstalador. Perfis e progresso em LOCALAPPDATA são preservados.
- Testadas atualização da instalação real, instalação isolada, atualização com versões antigas/novas abertas e desinstalação isolada com ferramentas abertas, todas com código de saída zero. state.json foi preservado.

## 0.11.0 - Uso independente do PW Helper

- Perfis por sessão da janela: nome/classe manual ou seleção de perfil salvo, mesmo com vários clientes usando o título padrão. Nenhuma instalação ou processo PW Helper é exigido.
- Preservada a detecção por título e as associações antigas no menu de compatibilidade. Títulos genéricos/repetidos e perfis duplicados em dois clientes são recusados para evitar mistura de progresso.
- Progresso e God/Evil continuam por nome/classe; PID, início do processo e HWND servem apenas à associação da sessão atual. Reiniciar o overlay/cliente exige selecionar novamente o perfil salvo.
- Alterar uma associação descarta a sessão anterior de leitura. Leituras inválidas limpam snapshot/transições, mudança do ponteiro do jogador reabre a sessão e troca de token invalida a associação anterior. Falhas ao abrir têm nova tentativa após dois segundos.
- Atualizados manual, instalação, arquitetura e testes. Leitura automática de nome/classe não foi incluída; não havia clientes reais abertos para validar offsets adicionais ou a troca de conta no servidor.

## 0.10.2 - Ícone e preparação da distribuição

- Central reduzida às seleções Overlay/Windowed Fullscreen e aos botões Aplicar ao PW já aberto e Parar Overlay e Borderless. Removidas procura de launcher e abertura do jogo; o usuário abre pelo Helper ou manualmente. As caixas passam a ser aplicadas pelo botão, sem iniciar ferramentas apenas ao mudar a seleção.
- Aplicada a opção aprovada Órbita de progresso como ícone dos quatro executáveis, da Central, da bandeja do overlay e do instalador/desinstalador. ICO com nove tamanhos, de 16 a 256 pixels, e fonte preservada no Git.
- Build centraliza a versão em VERSION e grava metadados nos executáveis. O menu da bandeja mostra a versão para facilitar o suporte.
- Instalador verifica .NET Framework 4.8, conforme o requisito documentado, e inclui links de suporte/atualização.
- Corrigida a instrução de uso portátil, que ainda citava um ZIP antigo e omitia arquivos necessários.
- Acrescentado verificador do pacote e documento com os testes de campo/decisões ainda pendentes antes da 1.0. Nenhuma missão ou ID de progresso foi alterado nesta versão.

## 0.10.1 - Liberação do Passado e Dusk necessária

- Incluídas 24 entradas e 129 etapas de acesso à Dor Celestial, Floresta Kylin e Vale da Cicatriz, comuns a God/Evil e anteriores ao cultivo 100. Total: 585 entradas e 2.026 etapas.
- Acrescentados os chefes e modos de Dusk exigidos pela cadeia, suas submissões, retornos e referências de NPCs. Dusk opcional de farm continua fora desta alteração.
- Introdução pela variante 90+ de Plano dos Monstros, sem exigir também as variantes 70–79/80–89. Desvios alternativos da história foram selecionados explicitamente, sem confundir essas decisões com God/Evil.
- Mantidos os IDs de progresso anteriores; reordenada a extensão para respeitar os acessos. O prazo de permanência na Floresta Kylin é explicado sem mandar esperar duas horas.
- Atualizados planilha, manual, fatos dataminados e verificador de dependências. A nova cadeia ainda precisa de validação integral no servidor.

## 0.10.0 - Guia até 105 e escolha de cultivo

- Acrescentadas 281 entradas e 877 etapas dos níveis 61–105, com nomes e submissões dataminados. Total: 561 entradas e 1.897 etapas, incluindo os dois ramos alternativos.
- Escolha explícita God/Evil no cultivo 89, salva por personagem e em backups. O guia aguarda a resposta e acompanha os cultivos específicos 89, 99 e 100.
- Preparação conjunta de cultivo e moedas nas DG69, 79, 89 e 99; seleção de missões de XP, cargos militares condicionais e atividades repetíveis da faixa final.
- Cinco abas atualizadas até 105, com coluna Cultivo e preservação das marcações. Todos os IDs e etapas anteriores foram mantidos.
- Removidos os módulos, atalhos, configurações e dependências de OCR/Tesseract. A leitura de memória continua disponível. O instalador limpa a antiga pasta OCR na atualização.
- Dusk permanece fora do escopo anterior; uma etapa explicita os acessos ao Passado exigidos pelo cultivo 100.
- Verificados requisitos, níveis máximos, nomes, ramos e eventos de interface. O percurso 61–105 ainda precisa de validação integral no servidor.

## 0.9.0 - Dez classes e arraste com botão pressionado

- Arraste mantém a captura no controle que recebeu o clique. Segurar o esquerdo move o painel; soltar encerra e salva, inclusive em outro monitor. Perder a captura também encerra o arraste.
- Mago (MG) compartilha o guia humano com WR; Espiritualista (ES) compartilha o abissal com MC. IDs e 836 etapas anteriores preservados.
- Adicionados Arcano (SK) e Místico (MS), quinta aba dos Guardiões e suporte às novas siglas na detecção, associação manual e editor.
- 54 novas entradas de guia e 184 etapas, com nomes, níveis máximos, NPCs, requisitos e recompensas extraídos do cliente. Total: 280 entradas e 1.020 etapas.
- Percurso inicial dos Guardiões encurtado para XP ×3; aceite antecipado de A Reunião e Memórias evita perder limites de nível. Aviso de produção correto no nível mínimo 6.
- Incluídos os quatro contratos de invocação e os seis aprendizados de plantas do Místico até o nível 59, com submissões e IDs próprios; após a capital, a sequência comum vai até 60.
- Testes de rotas para dez classes, auditoria de todos os IDs novos e eventos sintéticos de arraste aprovados. Os novos percursos ainda precisam de validação completa em personagens SK/MS no jogo.

## 0.8.5 - Overlay em outro monitor

- Arrastar o painel para outra tela agora salva o monitor escolhido e a posição, sem trazê-lo de volta ao monitor do jogo.
- A troca de cliente em foco continua selecionando o personagem sem mudar a tela escolhida para o overlay.
- Adicionada a opção Acompanhar monitor do PW na bandeja para voltar ao comportamento automático.
- Se a tela escolhida for desconectada, o painel usa temporariamente o monitor do jogo e retoma a preferência quando ela voltar.
- Verificados monitores à esquerda, à direita e acima da tela principal, persistência da preferência e limite da área útil.

## 0.8.4 - Recuperação ao conectar outro cliente

- Adicionado ao catálogo o indicador real de execução sequencial dos filhos.
- Objetivos automáticos podem ser reconhecidos após conexão/reinício quando um irmão posterior está ativo e o pai exige execução em ordem.
- Filhos paralelos, objetivos ainda ativos e sucessores de outras cadeias não permitem essa recuperação.
- Caso motivador: WB em Primeira Viagem, com Exposição de Talento (1178) já ausente e Triunfo (1179) ativo antes da leitura inicial.
- Associação explícita de A Cidade das Feras ao ID 1204, evitando o homônimo de outra missão. Auditoria: 105 associações em 31 cadeias.

## 0.8.3 - Revisão das cadeias e materiais independentes

- Revisadas as 836 etapas: 102 associações explícitas em 30 cadeias, incluindo as visitas aos artesãos das quatro raças.
- Materiais do mesmo ID agora podem ser verificados por etapa: Ossos e Chifres, Pó e Antenas, Doutrina Secreta e Vingança dos Parentes.
- Corrigidos pais genéricos nas cadeias Fazer Armas, Remédio Útil, Cortar Montanhas, Conspiração, A Promessa e outras.
- Recuperação por histórico de entregas, sem confundir ausência ou reaceite com missão concluída.
- Uma transição observada pode concluir as etapas redundantes de conversar/entregar no mesmo NPC. Validado ao vivo em Bambu dos Selvagens.
- Adicionados auditor e manifesto versionados para prevenir novas associações incorretas; quantidades das etapas revisadas ajustadas aos requisitos dataminados.

## 0.8.2 - Coletas em submissões aninhadas

- Corrigida a associação de lenha e ferro em `Livro de Fundição`: as etapas agora apontam para as submissões filhas `Eliminando Pulador-Arp` (85) e `Livrar-se dos Duendes` (86).
- Coletas concluídas são reconhecidas pelos itens mesmo quando o filho já desapareceu automaticamente, desde que a missão ou um ancestral real da cadeia continue ativo.
- Os contadores dos materiais são independentes: lenha 10/10 não conclui ferro 2/10.
- Testados dois níveis de hierarquia, filho desaparecido, contagem insuficiente e itens sem uma cadeia ativa correspondente.

## 0.8.1 - Contador de abates

- Integrado o primeiro contador de abates da lista de missões ativas ao avanço automático, com confirmação em duas leituras estáveis.
- Adicionados ao catálogo os requisitos de objetivos com um único tipo de monstro, sem coleta de itens, até 255 abates.
- Etapas de aceite, entrega e ações mistas não avançam pelo contador de abates.
- Validada no cliente a etapa dos 10 Filhotes de Ouriço de `Confiança dos Guardas` (ID 2714, monstro 1037), que avançou automaticamente em 10/10.
- Testados os limites 0/10, 9/10, 10/10, contagem acima do limite, missão inativa, coleta e ID divergente.

## 0.8.0 - Rotas revisadas e estado por memória

- Promovida a integração de memória à versão estável, com ativação opcional e avanço manual para objetivos ainda sem evidência suportada.
- Reordenadas etapas locais com coordenadas conhecidas, preservando dependências, limites de desbloqueio e progresso salvo.
- Agrupadas moeda e cultivo nas DG29 e DG59 para evitar repetir a entrada; DG39 agora é concluída na faixa do cultivo 39.
- Antecipada a remoção do pop-up de voo para a primeira visita do nível 30 à cidade.
- Incluídos catálogo de missões e offsets no instalador.

- Criada a branch experimental `feature/memory-quest-state`, com leitura externa somente leitura do cliente x86.
- Identificados `CECHostPlayer`, `CECTaskInterface`, lista ativa, relação pai/filho, histórico e inventário de missão.
- Adicionado catálogo derivado do `tasks.data` com 24.277 missões e submissões e os itens exigidos pelos objetivos de coleta, sem incorporar o parser de terceiros.
- O diagnóstico agora exibe nomes, IDs, código bruto de estado, itens de missão e monitora transições em todos os clientes abertos.
- Ausência só gera evento depois que a missão foi observada ativa, evitando confundir missão nunca aceita com missão entregue.
- O overlay ganhou toggle e atalho para sincronização por memória, desligados por padrão e separados por cliente.
- Aceites, conversas, coletas e entregas usam evidências estáveis em duas leituras consecutivas.
- Validada a cadeia `Aldeões Feridos`: item 3502 em `10/10` avançou a coleta e o desaparecimento do ID 936 confirmou a entrega.

## 0.7.6 - Toggle de Windowed Fullscreen

- Transformado o controle de Windowed Fullscreen da Central em um toggle visual e imediato.
- Ao ativar, o modo é aplicado aos clientes abertos e aos clientes iniciados depois.
- Ao desativar, bordas, tamanho, posição e estado maximizado originais são restaurados antes de encerrar o monitor.
- A preferência continua salva para a próxima execução.

## 0.7.5 - Datamine completo até o nível 60

- Estendido o datamine do `tasks.data` para todas as missões restantes dos níveis 21 a 60.
- Corrigidos 39 títulos de missão e 321 títulos de etapas, preservando grafias e truncamentos do cliente.
- Identificada a antiga `Quest sem Tradução` como a cadeia `Segredo da Família` até `Salvação`.
- Atualizadas as submissões de cultivos, DGs, Pilar Esquisito e cadeias longas.
- Sincronizados JSON e planilha sem alterar IDs de progresso.
- Mantida a instrução de remover a missão de voo como etapa operacional do overlay.

## 0.7.4 - Nomes oficiais do cliente 1.4.4

- Dataminado o `tasks.data` versão 124 do cliente instalado, com 24.277 missões e submissões extraídas integralmente.
- Corrigidos 58 títulos das rotas de nível 1 a 20 para a grafia usada pelo cliente.
- Atualizados 511 títulos de etapas com os nomes reais das missões ou submissões.
- Corrigidas cadeias traduzidas de forma divergente, incluindo `Conspiração`, `Informação do Inimigo`, `A Montanha Nebulosa`, `Atrição do Velho Sold.` e `O Equilíbrio Celestial`.
- Mantidos todos os IDs do overlay para preservar o progresso salvo.
- Adicionado relatório técnico do datamine e testes para as traduções críticas.

## 0.7.3 - Todos os painéis do PW Helper no topo

- Todos os painéis flutuantes visíveis do processo `PW Helper` agora são mantidos acima do jogo, sem depender do título `TODOS`.
- A janela principal intitulada exatamente `PW Helper` continua fora da regra para não ficar permanentemente presa no topo.
- O posicionamento automático do overlay evita colisão com qualquer um dos painéis flutuantes detectados.

## 0.7.2 - Distribuição portátil

- O log do PW Borderless agora usa `%LOCALAPPDATA%\PWQuestOverlay`, sem depender da pasta específica em que o jogo foi instalado.
- Instalador e pacote portátil preparados para publicação direta na página Releases do GitHub.

## 0.7.1 - Correção da substituição de marcadores

- Corrigido o ponto padrão da lista de marcadores de uma área vazia para a primeira linha selecionável.
- A automação agora clica em uma linha real, seleciona o último `PWOverlay` com `End`, exclui, confirma e cria o novo na mesma ação.
- Configurações antigas com o ponto padrão incorreto são migradas automaticamente sem alterar calibrações personalizadas.
- Adicionado registro no log após a fase de exclusão para facilitar diagnóstico.
- O estado agora registra a quantidade de `PWOverlay` por personagem, permitindo limpar duplicados acumulados sem apagar marcadores pessoais.

## 0.7.0 - Modo compacto e instalador

- Adicionado botão `−/+` para alternar entre a interface completa redesenhada e uma interface compacta de 470×122.
- O modo compacto mantém missão, ação/NPC, progresso e os botões Voltar, Etapa feita e Marcar.
- Reduzidos espaços e botões, refinadas as cores e alterada a opacidade recomendada de 92% para 82%.
- A preferência de interface passa a fazer parte do estado e dos backups.
- Criado instalador por usuário com atalhos opcionais e inicialização automática opcional do overlay e do PW Borderless.
- Criado pacote portátil versionado e guia para instalação, migração de perfis e calibração em outro computador.
- Adicionado acesso ao guia de configuração pelo menu do ícone ao lado do relógio.
- Todas as 836 etapas agora possuem título de submissão e o overlay mostra `Missão › Submissão`.
- Corrigida a cadeia Pérola de Lágrimas para incluir o Sapo, a Pérola do Homem Tubarão e **Reportar ao Guarda Lufu**.
- Adicionado **PW Overlay Central** para abrir o jogo com ferramentas selecionadas, aplicar ferramentas a clientes abertos ou iniciar somente o PW.

## 0.6.0 - Rota intercalada do nível 20 ao 60

- Reorganizada a rota compartilhada `COMMON` como um único circuito global do nível 20 ao 60.
- Cultivos, preparações, moedas e objetivos de DG foram agrupados para reduzir viagens e entradas repetidas.
- A cadeia do Pilar Esquisito e as missões longas podem permanecer abertas enquanto a rota aproveita objetivos próximos.
- Missões exclusivas de Humanos, Selvagens e Alados entram como desvios curtos e retornam à sequência compartilhada.
- Planilha e testes agora validam a ordem global também após a chegada à Cidade do Dragão.
- IDs de missão e etapa foram preservados para manter o progresso existente.

## 0.5.0 - Rotas intercaladas por região

- Adicionada ordem global por etapa, permitindo alternar entre missões sem exigir a conclusão imediata de cada uma.
- Otimizadas as rotas de Humanos, Selvagens, Alados e Abissais do nível 1 ao 20 por proximidade e dependências.
- Pop-ups como a orientação de forja do nível 5 agora aguardam o retorno natural ao Ancião.
- **Etapa feita**, **Voltar**, **Pular** e o seletor de etapas passaram a acompanhar a sequência global.
- Editor e planilha exibem a ordem `R...` de cada ação.
- Progresso salvo anteriormente continua usando os mesmos IDs de missão e etapa.

## 0.4.0 - Rota compartilhada até o nível 60

- Expandido o guia de 138 para 226 missões únicas e de 493 para 836 etapas.
- Criada a rota `COMMON`, compartilhada por WR, WB, WF, EA, EP e MC após a Cidade do Dragão.
- Mantidos desvios separados somente para missões exclusivas de Humanos, Selvagens, Alados e WF.
- Incluídos cultivos 29/39/49/59, DGs e Moedas Sangrentas 29/39/49/59, Pilar Esquisito e Cargo Militar Nível 4.
- Missões de voo foram excluídas; o nível 30 contém uma etapa para abandonar o pop-up no questlog.
- Missões de Dusk continuam fora desta versão.
- Planilha ampliada e sincronizada nas quatro abas, com XP ×3 e nível previsto.

## 0.3.0 - Navegação, calibração e nível previsto

- Adicionada a missão automática do nível 5 para falar com o Ancião sobre forja, mantendo fora as missões específicas dos quatro ofícios.
- Adicionado **Nível previsto** às 138 missões, à planilha, ao editor e ao overlay; nenhum nível real é armazenado.
- Total atualizado para 493 etapas.
- Adicionados seletor de missão/etapa e indicador colorido do tipo de ação.
- Adicionados atalho `Ctrl + Alt + M` com alternativa automática, calibração visual e modo de apagar todos ou substituir somente `PWOverlay`.
- O botão **Marcar** agora remove o `PWOverlay` anterior e cria o novo na mesma ação, sem exigir dois cliques e sem apagar os demais marcadores por padrão.
- Corrigida a confirmação intermediária do cliente 1.4.4 que fazia exclusão e criação acontecerem em cliques separados; a confirmação agora usa teclado e não depende da posição da caixa.
- Adicionado editor visual que sincroniza JSON e Excel com backup `.bak`.
- Adicionadas exportação e restauração de backup dos perfis.

## 0.2.0 - Etapas e marcadores

- Adicionadas 485 etapas às 134 missões das seis classes.
- Missões encadeadas agora avançam por conversa, objetivo e entrega.
- Botões concluir, voltar e pular passaram a trabalhar por etapa.
- Adicionado botão **Marcar**, que substitui marcadores anteriores por `PWOverlay` na coordenada atual.
- Planilha atualizada com a coluna **Etapas** nas quatro abas.
- Mantida compatibilidade com progresso antigo marcado no nível da missão.

## 0.1.0 - Protótipo inicial

- Overlay de próxima missão para PW 1.4.4.
- Detecção automática de títulos como `Cauton[EP]`.
- Gerenciador **Perfis** para associar títulos sem sigla a `WR`, `WB`, `WF`, `EA`, `EP` ou `MC`.
- Rotas de WR, WB, WF, EA, EP e MC.
- Progresso independente usando `PERSONAGEM|CLASSE`.
- Painel arrastável e posição persistente.
- Botões e atalhos para concluir, voltar e pular.
- Compatibilidade com PW Borderless Monitor.
- Código e compilação do PW Borderless Monitor incluídos no mesmo repositório.
- Reelevação periódica do painel `TODOS` do PW Helper.
- Documentação de desenvolvimento, edição e testes.
