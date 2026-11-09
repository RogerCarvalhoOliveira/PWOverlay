# Instalação e configuração em outro computador

## Requisitos

- Windows 7 SP1 ou mais recente, preferencialmente Windows 10/11 de 64 bits.
- Microsoft .NET Framework 4.8.
- Perfect World 1.4.4, PW Helper e os clientes já funcionando.
- Mesma escala de interface do jogo em todos os clientes. Se a resolução mudar, calibre o marcador novamente.

## Instalação recomendada

1. Copie `PWQuestOverlay-Setup-v0.7.1.exe` para o outro computador.
2. Execute o instalador. Não é necessário acesso de administrador: o programa é instalado somente para o usuário atual.
3. Escolha, se desejar, atalhos e inicialização automática. Deixe a inicialização direta do Overlay/PW Borderless desmarcada se quiser decidir a cada sessão.
4. Ao terminar, abra **PW Overlay Central**, selecione as ferramentas desejadas e use **Iniciar selecionados + PW**.

O instalador cria atalhos no menu Iniciar e registra um desinstalador. Atualizações podem ser instaladas por cima da versão anterior.

## Abrir com ou sem o overlay

- **Iniciar selecionados + PW:** abre Overlay e/ou Windowed Fullscreen conforme as caixas marcadas e depois abre o launcher configurado.
- **Abrir somente o PW:** encerra nossas duas ferramentas e abre o jogo normalmente.
- **Aplicar ao PW já aberto:** inicia ou encerra as ferramentas conforme as caixas, sem abrir outro cliente.
- **Parar Overlay e Borderless:** encerra somente nossas ferramentas; os clientes e o PW Helper continuam abertos.

Na primeira utilização, a Central tenta localizar o launcher automaticamente. Se não encontrar, clique em **Procurar...** e selecione o launcher original do servidor, não um atalho.

## Configuração inicial

1. No PW Helper, dê a cada janela um título como `Personagem[EP]`. Se isso não for possível, abra **Perfis** no overlay e associe manualmente o título à classe.
2. Desbloqueie **Mover**, arraste o painel para uma área que não cubra o painel `TODOS` do PW Helper e bloqueie novamente.
3. Use o botão `−` para ativar a interface compacta; o botão `+` restaura a interface completa.
4. Abra o menu do ícone do overlay ao lado do relógio e use **Calibrar marcador...**. Siga cada ponto com o painel de Marcadores do jogo aberto.
5. Teste **Marcar** em uma etapa com coordenadas e confirme que somente o marcador `PWOverlay` foi substituído.
6. Alterne entre todos os clientes e confirme que personagem, classe e progresso mudam junto com o foco.

## Levar progresso e perfis para o novo computador

No computador antigo, use **Exportar backup de perfis...** no ícone ao lado do relógio. Copie o JSON exportado e, no computador novo, use **Restaurar backup de perfis...**.

O backup inclui progresso, associações de classe, posições, transparência, modo compacto e calibração. Se o novo computador tiver outra resolução, restaure o backup e calibre o marcador novamente.

## Versão portátil

Extraia `PWQuestOverlay-Portatil-v0.7.1.zip` para uma pasta em que o usuário tenha permissão de escrita e execute `PW Overlay Central.exe`. Não mova apenas um EXE: as três ferramentas e `quest_routes.json` devem permanecer juntas.

## Dados locais e desinstalação

O estado fica em `%LOCALAPPDATA%\PWQuestOverlay`. A desinstalação remove o programa, mas preserva esse estado para permitir reinstalação sem perder o progresso. Para remover tudo, desinstale o aplicativo e apague essa pasta manualmente somente depois de fazer um backup.

O aplicativo não inclui nem modifica arquivos do Perfect World ou do PW Helper.
