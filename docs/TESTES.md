# Roteiro de testes

## Teste automático mínimo

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
```

Execute também `node scripts/verify-endgame-routes.mjs`, `node scripts/audit-nested-objectives.mjs --summary`, `node scripts/verify-route-workbook.mjs` e `scripts/test-overlay-interactions.ps1`. O verificador da planilha usa `@oai/artifact-tool` no runtime de planilhas, como o sincronizador. Confirme que o pacote contém os executáveis, as rotas, a planilha de cinco abas e os guias, sem módulos OCR ou Tesseract.

## Cultivo e extensão 61–105

1. Em um perfil de teste, abra a escolha do cultivo: nenhuma opção deve estar pré-selecionada e confirmar deve permanecer desativado até escolher.
2. Cancele: a rota deve aguardar no cultivo 89. **Etapa feita**, **Pular** e avanço por memória não devem contornar a escolha.
3. Escolha God e confira as cadeias 89/99/100; troque para Evil e confira que somente o ramo escolhido aparece, além das missões compartilhadas.
4. Use dois personagens, um de cada caminho. Alterne clientes, reinicie e restaure um backup: cada escolha deve permanecer independente.
5. Confira a coluna Cultivo das cinco abas e os aceites das moedas antes dos objetivos das DG69/79/89/99.
6. Verifique os acessos ao Passado antes do cultivo 100. O guia não inclui a liberação por Dusk, ainda fora do escopo.
7. Confirme no jogo as condições das repetíveis e a chegada real ao 105. A rota não garante essa experiência apenas com as missões únicas.

## Um cliente

1. Abra o monitor borderless, o PW Helper e um cliente.
2. Dê ao cliente um título como `Teste[EP]`.
3. Inicie o overlay e coloque o cliente em foco.
4. Confirme personagem, classe, missão, título da submissão, ação e contador da etapa.
5. Avance uma etapa, volte e pule; reinicie o overlay e confirme a persistência.
6. Em uma missão com várias etapas, confirme que a missão seguinte só aparece após a última.
7. No início da rota WR, confirme a intercalação: pegar **Fazer Armas**, fazer **Teste de Salto**, falar com o Ancião sobre forja e somente depois sair para os objetivos externos.
8. Confirme que **Voltar** retorna à ação global anterior mesmo quando ela pertence a outra missão.
9. Salte para o nível 29 e confirme que cultivo, moeda e objetivos da DG29 aparecem intercalados na mesma preparação/entrada.
10. Repita a conferência nas DGs 39, 49 e 59 e percorra a cadeia do Pilar Esquisito até o retorno à Cidade das Tormentas.
11. Em WR, WB/WF e EA/EP, confirme que a missão racial do nível 50 aparece como desvio e que a ação seguinte retorna à rota comum.

## Modo compacto

1. Clique em `−` e confirme que o painel passa para 470×122 sem mudar o cliente ativo.
2. Confirme missão, ação/NPC, status, **Voltar**, **Etapa feita** e **Marcar**.
3. Arraste o painel compacto, bloqueie a posição, reinicie o aplicativo e confirme tamanho e posição.
4. Use `+` e a opção **Modo compacto** da bandeja para alternar nos dois sentidos.
5. Exporte e restaure um backup com o modo compacto ativo.

## PW Overlay Central

1. Configure o launcher do PW e deixe as duas caixas marcadas; use **Iniciar selecionados + PW**.
2. Com clientes abertos, desmarque Overlay e use **Aplicar ao PW já aberto**; confirme que somente o overlay encerrou.
3. Desative o toggle **Windowed Fullscreen** com um cliente aberto e confirme que bordas, tamanho e posição anteriores são restaurados sem fechar o cliente ou o PW Helper.
4. Ative o toggle novamente e confirme que o cliente aberto retorna ao modo Windowed Fullscreen sem precisar usar **Aplicar ao PW já aberto**.
5. Use **Abrir somente o PW** e confirme que Overlay/Borderless encerram sem fechar clientes ou PW Helper.
4. Reabra as ferramentas pela Central e confirme que progresso e posição foram preservados.

## Marcador

1. Use uma etapa com coordenadas e deixe o painel **Marcadores** fechado.
2. Crie manualmente um marcador de teste no jogo.
3. Clique em **Marcar** e não mova o mouse por cerca de um segundo.
4. Abra o painel do jogo e confirme que há somente `PWOverlay` com a coordenada da etapa atual.
5. Clique em **Marcar** novamente e confirme no log a exclusão anterior; a lista deve continuar com apenas um `PWOverlay`.
6. Troque de cliente, clique em **Marcar** e confirme que o marcador foi criado apenas no cliente em foco.
7. Use uma etapa sem coordenadas e confirme que o botão fica desativado.
8. Teste o atalho informado no menu (`Ctrl + Alt + M` ou o alternativo), os dois modos de exclusão e o assistente **Calibrar marcador...**.

## Associação manual

1. Use um título sem sigla, como `TesteClerigo`.
2. Abra **Perfis**, associe a `EP` e salve.
3. Troque o foco e confirme a rota de EP.
4. Reinicie o overlay e confirme que a associação permaneceu.
5. Volte a associação para **Automático** e confirme que o perfil fica sem classe até o título fornecer uma sigla.

## Multiclient

1. Abra dois clientes com títulos diferentes e classes diferentes.
2. Marque uma etapa apenas no primeiro.
3. Alterne o foco várias vezes e confirme que missão e progresso acompanham o cliente correto.
4. Abra dois ou mais painéis flutuantes do PW Helper com títulos diferentes, clique neles e confirme que o teclado continua direcionado ao jogo ao voltar.
5. Confirme que todos os painéis permanecem visíveis acima do cliente e que o overlay não os cobre indevidamente; a janela principal `PW Helper` não deve ficar presa no topo.

## Posição e monitores

Desbloqueie, segure o botão esquerdo na barra superior e mova o painel. Solte o esquerdo: o painel deve parar imediatamente, sem outro clique. Teste também sobre o nome do personagem, perda de captura e posição bloqueada. Reinicie e confira a posição. Se houver outro monitor, arraste para ele e alterne os clientes: o painel deve continuar na tela escolhida. Teste também **Restaurar posição** e **Acompanhar monitor do PW**.

`scripts/test-overlay-interactions.ps1` executa eventos sintéticos nos controles reais, sem mover o cursor ou enviar entrada ao jogo, usando estado isolado em `work/ui-regression-state`. Verifica arraste mantido, soltura do esquerdo, perda de captura, bloqueio, persistência do monitor e reconhecimento das dez siglas. `node scripts/verify-guardian-routes.mjs` verifica pré-requisitos, limites de aceite, antecipação das entregas e coordenadas dos Guardiões.

## Seleção, backup e editor

1. Em **Etapas**, salte para uma etapa intermediária, confirme e teste **Voltar**.
2. Exporte o backup, avance uma etapa e restaure o arquivo; confirme o retorno do progresso.
3. Em uma cópia de teste, altere texto e nível previsto no editor de rotas, salve e confirme o JSON, a planilha e os arquivos `.bak`.
4. Confirme **Habilidades Produção**: nível mínimo 5 para WR/MG, WB/WF, EA/EP e MC/ES; nível mínimo 6 para SK/MS, entregue na passagem planejada pelo Ancião Star.

## Compatibilidade de estado

Faça backup de `%LOCALAPPDATA%\PWQuestOverlay\state.json`, teste com uma cópia anterior à propriedade `ClassAssociations` e confirme que o programa inicia sem apagar as missões concluídas.

## Instalador em outro computador

1. Execute `PWQuestOverlay-Setup-v0.10.0.exe` como usuário comum e confirme que não pede elevação.
2. Teste atalhos do menu Iniciar, opções de área de trabalho e inicialização automática.
3. Confirme que o guia abre ao final e que overlay, rotas e planilha estão no diretório instalado.
4. Importe um backup, associe um cliente sem sigla e refaça a calibração do marcador.
5. Instale novamente por cima, confirme a atualização e depois teste a desinstalação; o estado em `%LOCALAPPDATA%\PWQuestOverlay` deve permanecer.
