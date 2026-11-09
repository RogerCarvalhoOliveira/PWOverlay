# Solução de problemas

## O overlay não aparece

- Coloque um cliente `elementclient` em foco.
- Verifique se o programa não foi ocultado pelo botão X; use o ícone ao lado do relógio.
- Use **Restaurar posição** se ele puder estar fora da tela.
- Consulte `%LOCALAPPDATA%\PWQuestOverlay\overlay.log`.

## A classe está errada ou vazia

Abra **Perfis**, selecione a linha pelo PID/título e informe nome e classe ou aplique um perfil salvo. PW Helper não é necessário. Se várias janelas tiverem o mesmo título padrão, associe cada linha separadamente. O modo Automático exige um título identificável e único.

## O cliente aparece sem associação

Ao reabrir o jogo ou reiniciar o overlay, escolha novamente o perfil salvo em **Perfis**. Progresso e cultivo são retomados pelo nome/classe. Trocar de personagem ou detectar uma nova sessão de leitura pode invalidar a associação anterior. Não use o mesmo perfil em duas janelas simultaneamente. Nenhuma etapa será avançada enquanto a identidade estiver indefinida.

## A missão não avança

O botão só altera o perfil quando um cliente está ativo. Confirme também se a rota da classe foi reconhecida. Use `scripts\inspect-state.ps1` para consultar o estado sem alterá-lo.

## O marcador não foi criado

- Deixe o painel **Marcadores** fechado antes de clicar em **Marcar**.
- Não mova o mouse nem troque de cliente durante cerca de um segundo.
- Confirme que a etapa possui coordenadas; sem elas, o botão fica desativado.
- O fluxo foi calibrado em 1920×1080 borderless. Escala personalizada da interface pode exigir ajuste dos pontos de referência em `OverlayForm.cs`.
- Consulte `overlay.log` e procure por `Falha ao criar marcador`.

## O botão Marcar está acumulando PWOverlay antigos

A versão 0.7.1 corrige o ponto de seleção da lista e passa a substituir o marcador
anterior na mesma ação. Instale ou execute essa versão com o painel **Marcadores**
fechado.

Se uma versão antiga já tiver acumulado mais de um item chamado `PWOverlay`, feche o
overlay e informe a quantidade existente para o perfil com:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\repair-marker-count.ps1 -CharacterKey "NOMEDOPERSONAGEM|CLASSE" -Count 3
```

O script preserva os demais marcadores do jogo e cria antes o backup
`state.json.before-marker-repair.bak`. No próximo clique em **Marcar**, somente essa
quantidade de itens do overlay será removida e um marcador novo será criado.

## O PW Helper some atrás do jogo

Mantenha o painel desejado aberto. O overlay e o monitor borderless elevam todas as janelas flutuantes visíveis do processo `PW Helper`, sem depender do título e sem transferir o foco. A janela principal intitulada exatamente `PW Helper` é excluída de propósito.

## O jogo perdeu a janela sem bordas

Reinicie apenas `PW Borderless.exe`. Ele espera o cliente estabilizar antes de ajustar tamanho e bordas. O log do monitor fica em `%LOCALAPPDATA%\PWQuestOverlay\PWBorderless.log`.

## O programa não compila

Confirme a existência de `csc.exe` em uma das pastas do .NET Framework indicadas em `docs\DESENVOLVIMENTO.md`. Execute o build na raiz do repositório e leia a primeira mensagem de erro do compilador.

## Recuperar progresso

Feche o overlay e restaure o backup de `%LOCALAPPDATA%\PWQuestOverlay\state.json`. Não edite esse arquivo enquanto o programa estiver aberto, porque ele poderá salvar o estado em memória por cima da edição.
