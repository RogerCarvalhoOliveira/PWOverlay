# Instalação e configuração em outro computador

## Requisitos

- Windows 7 SP1 ou mais recente, preferencialmente Windows 10/11 de 64 bits.
- Microsoft .NET Framework 4.8.
- Perfect World 1.4.4 com os clientes funcionando. O PW Helper é opcional e pode estar ausente do computador.
- Mesma escala de interface do jogo em todos os clientes. Se a resolução mudar, calibre o marcador novamente.

## Instalação recomendada

1. Baixe `PWQuestOverlay-Setup-v0.11.1.exe` na página **Releases** do GitHub e copie-o para o outro computador.
2. Execute o instalador. Não é necessário acesso de administrador: o programa é instalado somente para o usuário atual.
3. Escolha, se desejar, atalhos e inicialização automática. Deixe a inicialização direta do Overlay/PW Borderless desmarcada se quiser decidir a cada sessão.
4. Abra o PW manualmente ou pelo PW Helper. Na **PW Overlay Central**, selecione as funções desejadas e use **Aplicar ao PW já aberto**.

O instalador cria atalhos no menu Iniciar e registra um desinstalador. Atualizações podem ser instaladas por cima da versão anterior. Salve edições de rotas antes de atualizar: o instalador encerra as ferramentas daquela pasta antes de substituir os arquivos. O monitor sem bordas restaura as janelas; jogo e PW Helper continuam abertos.

## Abrir com ou sem o overlay

- **Aplicar ao PW já aberto:** inicia ou encerra as ferramentas conforme as caixas marcadas. As funções ativadas também acompanham os próximos clientes abertos.
- **Parar Overlay e Borderless:** encerra somente nossas ferramentas; os clientes e o PW Helper continuam abertos.

Para desligar somente Windowed Fullscreen, desmarque essa função e clique em **Aplicar ao PW já aberto**. O monitor devolve a cada cliente acompanhado o estilo, o tamanho e a posição registrados antes da aplicação. As seleções são aplicadas pelo botão, não ao marcar/desmarcar uma caixa.

A Central não abre o PW e não tem configuração de caminho do jogo. Para jogar sem as ferramentas, use **Parar Overlay e Borderless** caso estejam ativas e abra o PW normalmente pelo método que preferir.

## Configuração inicial

1. Entre no personagem e coloque o cliente em foco. Na 0.12.0, nome e classe são identificados automaticamente pela memória, mesmo com título padrão. Se necessário, abra **Perfis**: cada linha mostra uma janela/PID; informe nome/classe ou aplique um perfil salvo. O PW Helper é opcional. A release pública 0.11.1 ainda exige Perfis ou título identificável.
2. Desbloqueie **Mover**, segure o botão esquerdo na barra superior, arraste e solte onde quiser, inclusive em outro monitor. Depois bloqueie novamente.
3. Use o botão `−` para ativar a interface compacta; o botão `+` restaura a interface completa.
4. Abra o menu do ícone do overlay ao lado do relógio e use **Calibrar marcador...**. Siga cada ponto com o painel de Marcadores do jogo aberto.
5. Teste **Marcar** em uma etapa com coordenadas e confirme que somente o marcador `PWOverlay` foi substituído.
6. Alterne entre todos os clientes e confirme que personagem, classe e progresso mudam junto com o foco.

No cultivo 89, escolha God ou Evil quando o overlay perguntar. Cada personagem guarda sua própria opção; o menu **Escolher/alterar cultivo God ou Evil...** permite corrigi-la. Essa opção configura o guia: escolha o mesmo cultivo no NPC do jogo. Consulte `GUIA-61-105.txt` para os acessos exigidos no cultivo 100 e as atividades repetíveis da faixa final.

A identificação por memória funciona mesmo com o avanço automático de missões desativado. Ao reiniciar, personagens detectados retomam o perfil pelo nome/classe. Uma troca de personagem descarta as evidências anteriores e exige duas leituras iguais. Associações manuais continuam sendo feitas por sessão; selecione novamente um nome personalizado ao reabrir o overlay. A validação da 0.12.0 com personagens conectados ainda está pendente.

A associação de uma janela não é gravada por PID para a próxima execução: o Windows pode reutilizar esses identificadores. Isso evita atribuir o personagem errado a um cliente novo. Duas janelas não podem usar simultaneamente o mesmo perfil; para personagens distintos de mesmo nome/classe em servidores diferentes, informe um identificador diferente no nome do perfil.

## Levar progresso e perfis para o novo computador

No computador antigo, use **Exportar backup de perfis...** no ícone ao lado do relógio. Copie o JSON exportado e, no computador novo, use **Restaurar backup de perfis...**.

O backup inclui os perfis, progresso, cultivo, associações antigas por título, posições, transparência, modo compacto e calibração. As associações das janelas abertas são da sessão atual e devem ser refeitas no computador novo. Se a resolução mudar, calibre o marcador novamente.

## Versão portátil

Baixe e extraia `PWQuestOverlay-Portatil-v0.11.1.zip` para uma pasta em que o usuário tenha permissão de escrita e execute `PW Overlay Central.exe`. Não mova apenas um EXE: mantenha juntos todos os arquivos do pacote, incluindo rotas, catálogo, offsets e planilha.

O símbolo do check dentro dos anéis identifica o projeto na bandeja do Windows, nos atalhos e no instalador. O menu da bandeja informa a versão. O ícone está incorporado: não é necessário copiar um ICO separado para a instalação.

## Dados locais e desinstalação

Na Central, use **Desinstalar programa...**, no rodapé. A mesma opção está no menu do ícone do overlay ao lado do relógio. Ela fecha as ferramentas da instalação e abre o desinstalador padrão, com sua confirmação. Também é possível usar Aplicativos Instalados do Windows ou o atalho do menu Iniciar.

Na versão portátil, o link fica desativado porque não há desinstalador. Feche as ferramentas e remova a pasta extraída quando quiser deixar de usá-las.

O estado fica em `%LOCALAPPDATA%\PWQuestOverlay`. A desinstalação remove o programa, mas preserva esse estado para permitir reinstalação sem perder o progresso. Para remover tudo, desinstale o aplicativo e apague essa pasta manualmente somente depois de fazer um backup.

O aplicativo não inclui nem modifica arquivos do Perfect World ou do PW Helper.
