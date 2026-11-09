# Histórico de alterações

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
