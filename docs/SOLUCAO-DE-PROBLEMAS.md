# Solução de problemas

## O overlay não aparece

- Coloque um cliente `elementclient` em foco.
- Verifique se o programa não foi ocultado pelo botão X; use o ícone ao lado do relógio.
- Use **Restaurar posição** se ele puder estar fora da tela.
- Consulte `%LOCALAPPDATA%\PWQuestOverlay\overlay.log`.

## A classe está errada ou vazia

Abra **Perfis**, localize o título exato mostrado pelo PW Helper e escolha a classe. Títulos diferentes, inclusive espaços e sufixos diferentes, são associações diferentes após a normalização.

## A missão não avança

O botão só altera o perfil quando um cliente está ativo. Confirme também se a rota da classe foi reconhecida. Use `scripts\inspect-state.ps1` para consultar o estado sem alterá-lo.

## O marcador não foi criado

- Deixe o painel **Marcadores** fechado antes de clicar em **Marcar**.
- Não mova o mouse nem troque de cliente durante cerca de um segundo.
- Confirme que a etapa possui coordenadas; sem elas, o botão fica desativado.
- O fluxo foi calibrado em 1920×1080 borderless. Escala personalizada da interface pode exigir ajuste dos pontos de referência em `OverlayForm.cs`.
- Consulte `overlay.log` e procure por `Falha ao criar marcador`.

## O PW Helper some atrás do jogo

Mantenha o painel `TODOS` aberto. O overlay e o monitor borderless elevam essa janela sem transferir o foco. Se o problema persistir, confira se o processo se chama `PW Helper` e se o título do painel continua `TODOS`.

## O jogo perdeu a janela sem bordas

Reinicie apenas `PW Borderless.exe`. Ele espera o cliente estabilizar antes de ajustar tamanho e bordas. O log do monitor fica em `userdata\PWBorderless.log` dentro da pasta do jogo.

## O programa não compila

Confirme a existência de `csc.exe` em uma das pastas do .NET Framework indicadas em `docs\DESENVOLVIMENTO.md`. Execute o build na raiz do repositório e leia a primeira mensagem de erro do compilador.

## Recuperar progresso

Feche o overlay e restaure o backup de `%LOCALAPPDATA%\PWQuestOverlay\state.json`. Não edite esse arquivo enquanto o programa estiver aberto, porque ele poderá salvar o estado em memória por cima da edição.
