# Histórico de alterações

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
