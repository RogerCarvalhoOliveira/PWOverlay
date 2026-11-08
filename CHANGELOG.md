# Histórico de alterações

## 0.3.0 - Navegação, calibração e nível previsto

- Adicionada a missão automática do nível 5 para falar com o Ancião sobre forja, mantendo fora as missões específicas dos quatro ofícios.
- Adicionado **Nível previsto** às 138 missões, à planilha, ao editor e ao overlay; nenhum nível real é armazenado.
- Total atualizado para 493 etapas.
- Adicionados seletor de missão/etapa e indicador colorido do tipo de ação.
- Adicionados atalho `Ctrl + Alt + M` com alternativa automática, calibração visual e modo de apagar todos ou substituir somente `PWOverlay`.
- O botão **Marcar** agora remove o `PWOverlay` anterior e cria o novo na mesma ação, sem exigir dois cliques e sem apagar os demais marcadores por padrão.
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
