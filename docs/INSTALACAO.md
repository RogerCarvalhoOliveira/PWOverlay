# Instalação e configuração em outro computador

## Requisitos

- Windows 7 SP1 ou mais recente, preferencialmente Windows 10/11 de 64 bits.
- Microsoft .NET Framework 4.8.
- Perfect World 1.4.4, PW Helper e os clientes já funcionando.
- Mesma escala de interface do jogo em todos os clientes. Se a resolução mudar, calibre o marcador novamente.

## Instalação recomendada

1. Baixe `PWQuestOverlay-Setup-v0.10.2.exe` na página **Releases** do GitHub e copie-o para o outro computador.
2. Execute o instalador. Não é necessário acesso de administrador: o programa é instalado somente para o usuário atual.
3. Escolha, se desejar, atalhos e inicialização automática. Deixe a inicialização direta do Overlay/PW Borderless desmarcada se quiser decidir a cada sessão.
4. Abra o PW manualmente ou pelo PW Helper. Na **PW Overlay Central**, selecione as funções desejadas e use **Aplicar ao PW já aberto**.

O instalador cria atalhos no menu Iniciar e registra um desinstalador. Atualizações podem ser instaladas por cima da versão anterior.

## Abrir com ou sem o overlay

- **Aplicar ao PW já aberto:** inicia ou encerra as ferramentas conforme as caixas marcadas. As funções ativadas também acompanham os próximos clientes abertos.
- **Parar Overlay e Borderless:** encerra somente nossas ferramentas; os clientes e o PW Helper continuam abertos.

Para desligar somente Windowed Fullscreen, desmarque essa função e clique em **Aplicar ao PW já aberto**. O monitor devolve a cada cliente acompanhado o estilo, o tamanho e a posição registrados antes da aplicação. As seleções são aplicadas pelo botão, não ao marcar/desmarcar uma caixa.

A Central não abre o PW e não tem configuração de caminho do jogo. Para jogar sem as ferramentas, use **Parar Overlay e Borderless** caso estejam ativas e abra o PW normalmente pelo método que preferir.

## Configuração inicial

1. No PW Helper, dê a cada janela um título como `Personagem[EP]`. Se isso não for possível, abra **Perfis** no overlay e associe manualmente o título à classe.
2. Desbloqueie **Mover**, segure o botão esquerdo na barra superior, arraste e solte onde quiser, inclusive em outro monitor. Depois bloqueie novamente.
3. Use o botão `−` para ativar a interface compacta; o botão `+` restaura a interface completa.
4. Abra o menu do ícone do overlay ao lado do relógio e use **Calibrar marcador...**. Siga cada ponto com o painel de Marcadores do jogo aberto.
5. Teste **Marcar** em uma etapa com coordenadas e confirme que somente o marcador `PWOverlay` foi substituído.
6. Alterne entre todos os clientes e confirme que personagem, classe e progresso mudam junto com o foco.

No cultivo 89, escolha God ou Evil quando o overlay perguntar. Cada personagem guarda sua própria opção; o menu **Escolher/alterar cultivo God ou Evil...** permite corrigi-la. Essa opção configura o guia: escolha o mesmo cultivo no NPC do jogo. Consulte `GUIA-61-105.txt` para os acessos exigidos no cultivo 100 e as atividades repetíveis da faixa final.

## Levar progresso e perfis para o novo computador

No computador antigo, use **Exportar backup de perfis...** no ícone ao lado do relógio. Copie o JSON exportado e, no computador novo, use **Restaurar backup de perfis...**.

O backup inclui progresso, associações de classe, posições, transparência, modo compacto e calibração. Se o novo computador tiver outra resolução, restaure o backup e calibre o marcador novamente.

## Versão portátil

Baixe e extraia `PWQuestOverlay-Portatil-v0.10.2.zip` para uma pasta em que o usuário tenha permissão de escrita e execute `PW Overlay Central.exe`. Não mova apenas um EXE: mantenha juntos todos os arquivos do pacote, incluindo rotas, catálogo, offsets e planilha.

O símbolo do check dentro dos anéis identifica o projeto na bandeja do Windows, nos atalhos e no instalador. O menu da bandeja informa a versão. O ícone está incorporado: não é necessário copiar um ICO separado para a instalação.

## Dados locais e desinstalação

O estado fica em `%LOCALAPPDATA%\PWQuestOverlay`. A desinstalação remove o programa, mas preserva esse estado para permitir reinstalação sem perder o progresso. Para remover tudo, desinstale o aplicativo e apague essa pasta manualmente somente depois de fazer um backup.

O aplicativo não inclui nem modifica arquivos do Perfect World ou do PW Helper.
