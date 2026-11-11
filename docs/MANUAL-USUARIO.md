# Manual do usuário

## Iniciar

1. Abra `PW Overlay Central.exe`.
2. Abra os clientes manualmente ou pelo PW Helper e marque **Overlay** e/ou **Windowed Fullscreen** na Central.
3. Clique em **Aplicar ao PW já aberto**. A Central apenas controla as ferramentas; não abre o jogo.
4. Na 0.12.0, aguarde a identificação de nome/classe pela memória. Use **Perfis** para corrigir manualmente ou aplicar um perfil salvo. Títulos como `Nome[EP]` continuam sendo uma alternativa. O PW Helper é opcional.
5. Coloque um cliente em foco. O painel aparecerá automaticamente quando o Overlay estiver ativado.

O overlay fica oculto quando outro aplicativo, como navegador ou Discord, está em foco.

As caixas escolhem o que será ativado ao clicar em **Aplicar ao PW já aberto**. Para desligar somente Windowed Fullscreen, desmarque essa opção e aplique. Isso restaura bordas, tamanho e posição anteriores dos clientes que o monitor acompanhou. Enquanto ativas, as ferramentas acompanham também novos clientes.

Para jogar sem nenhuma ferramenta, use **Parar Overlay e Borderless** se estiverem ativas e abra o PW manualmente ou pelo Helper. Esse botão encerra apenas nossas ferramentas; não fecha clientes nem o PW Helper.

Para remover a instalação, use **Desinstalar programa...** no rodapé da Central ou no menu da bandeja do overlay. O desinstalador preserva perfis e progresso. Na versão portátil, não há desinstalador: feche as ferramentas e remova a pasta extraída. Salve edições de rotas antes de atualizar ou desinstalar.

## Identificação de personagem

Nome e classe são lidos da memória na 0.12.0, sem depender do título ou do PW Helper, após duas leituras iguais. A identificação continua ligada ao desativar o avanço automático. Duas janelas com o mesmo título padrão permanecem separadas. Em **Perfis**, o nome detectado é preenchido e a dica da coluna Classe informa a classe e a origem da identificação. Essa versão ainda aguarda teste com personagens conectados.

Para corrigir manualmente, selecione a linha pelo PID/título, digite nome e classe ou aplique um perfil salvo. Essa associação tem prioridade sobre a leitura automática. Nomes personalizados de perfil continuam úteis para distinguir servidores diferentes.

Quando a janela já possui um título identificável, o formato reconhecido é:

```text
Personagem[CLASSE]
```

Siglas aceitas: `WR` (Guerreiro), `MG` (Mago), `WB` (Bárbaro), `WF` (Feiticeira), `EA` (Arqueiro), `EP` (Sacerdote), `MC` (Mercenário), `ES` (Espiritualista), `SK` (Arcano) e `MS` (Místico).

Exemplos:

- `Cauton[EP]`
- `Cauton[WB]`
- `Arqueira[EA]`

O identificador interno combina personagem e classe. Portanto, `Cauton[EP]` e `Cauton[WB]` mantêm progressos separados.

O modo automático retoma o progresso pelo nome real/classe. Se o nome real tiver um sufixo como `[EP]` que o título antigo removia, o perfil anterior da mesma classe é migrado quando não existe um perfil com o nome completo. Perfis manuais personalizados precisam ser reaplicados ao reiniciar; a troca de personagem ou de sessão de leitura remove a associação anterior.

Uma janela sem identificação, título genérico ou associação duplicada não pode concluir/pular etapas nem criar marcadores. Não associe duas janelas simultaneamente ao mesmo perfil. O nome/classe é a chave do progresso, independente de PID ou posição da janela.

A opção **Automático** remove a associação manual e usa primeiro a identidade por memória; na ausência dela, tenta um título identificável. As associações antigas continuam no menu **Associações por título (compatibilidade)...**; títulos genéricos ou repetidos nunca são usados como identidade.

O menu do ícone ao lado do relógio também possui **Gerenciar perfis...** e uma opção rápida para definir a classe do cliente atual.

## Botões

- **Etapa feita:** marca somente a etapa exibida. Na última etapa, conclui a missão e mostra a próxima.
- **Voltar:** desfaz a última etapa concluída ou pulada.
- **Pular etapa:** ignora somente a etapa atual.
- **Marcar:** substitui o `PWOverlay` anterior e cria o novo na coordenada da etapa atual em uma única ação, preservando os demais marcadores. Fica desativado quando a fonte não fornece coordenada.
- **Etapas:** abre a lista completa da classe para escolher uma missão e uma etapa. A alteração pede confirmação e ajusta o progresso anterior.
- **Mover/Fixado:** alterna entre permitir arraste e bloquear a posição.
- **−/+**: reduz o painel para o modo compacto ou restaura a interface completa.
- **Perfis:** associa cada cliente ao personagem/classe ou a um perfil salvo, com ou sem PW Helper.
- **X:** oculta o painel sem encerrar o programa.

Para deixar o overlay em outro monitor, clique em **Mover**, mantenha o botão esquerdo pressionado na barra superior e arraste até a tela desejada. Solte o botão esquerdo para encerrar o arraste e salvar o monitor e a posição. Clique em **Fixado** para impedir novos arrastes. Alternar entre os clientes do PW continua atualizando o personagem, sem mover o painel de tela.

Para voltar a acompanhar a tela do cliente em foco, marque **Acompanhar monitor do PW** no menu do ícone ao lado do relógio. Se o monitor escolhido for desconectado, o painel aparece temporariamente na tela do jogo e recupera a preferência quando o monitor voltar.

As etapas seguem uma ordem global otimizada. O overlay pode mandar pegar uma missão, depois outra missão próxima, realizar os dois objetivos e somente mais tarde voltar às entregas. O nome da missão muda conforme a ação atual, mas cada missão mantém seu progresso independente.

A opção **Rota reduzida (XP ×3)** fica ligada por padrão no menu da bandeja. Missões independentes de XP podem ser dispensadas quando o personagem já pode seguir o próximo trecho protegido. Quando falta nível, o planejador escolhe a maior recompensa disponível entre as candidatas verificadas. Cultivos, DGs/moedas, desbloqueios, pré-requisitos, recompensas importantes e missões já iniciadas permanecem no roteiro. Desmarque a opção para percorrer a lista completa ou selecione uma missão específica em **Etapas**.

O histórico confirmado é importado ao identificar o personagem, mesmo com o avanço de etapas desligado. A importação percorre a rota inteira. O controle **Avançar etapas em andamento automaticamente** governa o acompanhamento de aceites, objetivos e entregas enquanto você joga. Missões repetíveis e etapas sem evidência suficiente precisam acompanhar o ciclo atual ou receber confirmação manual.

Do nível 1 ao 105, a sequência foi organizada por região e dependências. Até a Cidade do Dragão, cada raça segue seu circuito; depois, todas usam a rota compartilhada e fazem os desvios exclusivos indicados. Pop-ups como **Aprenda sobre Habilidades de Forja** aguardam o retorno natural ao Ancião.

No ponto de escolha do cultivo 89, o overlay pergunta **God ou Evil** e salva a resposta somente para aquele personagem/classe. Sem resposta, o guia aguarda nessa etapa. Os botões Concluir/Pular abrem a escolha novamente. Após salvar, aparecem as missões compartilhadas e apenas as cadeias de 89, 99 e 100 do caminho escolhido. O menu **Escolher/alterar cultivo God ou Evil...** permite escolher antes ou corrigir a preferência. O jogo continua exigindo que você escolha o cultivo no NPC.

Na planilha, a coluna **Cultivo** permite manter **Ambos + God** ou **Ambos + Evil**. No overlay, o filtro é automático. Entre 100 e 105, complemente a história com as atividades repetíveis elegíveis e XP de combate; use Etapas para reabrir um circuito quando necessário. O nível previsto organiza a rota e a etapa final é confirmada manualmente ao atingir 105.

O cultivo 100 exige acesso às áreas do Passado. A cadeia de liberação, incluindo as etapas necessárias de Dusk/Crepúsculo, está no nível previsto 90 e é comum a God/Evil. Se já começou essa cadeia, retome sua submissão pelo menu Etapas. A introdução do guia usa Plano dos Monstros da faixa 90+; não repita a introdução se já concluiu uma das versões anteriores. Veja `GUIA-61-105.txt`.

Nas faixas de DG, aceite primeiro cultivo, preparação e moeda que possam compartilhar a mesma entrada. A rota pode deixar uma cadeia longa aberta e retomá-la mais tarde; siga a ação exibida, não apenas o nível mínimo da missão.

Na planilha, as abas antigas **Humanos - WR** e **Abissais - MC** agora também atendem MG e ES. A quinta aba, **Guardiões - SK-MS**, deve ser filtrada por `SK/MS` e sua sigla; o overlay aplica esse filtro automaticamente. O guia inclui os cultivos próprios dos Guardiões; as moedas posteriores seguem a rota comum.

Nos Guardiões, aceite **A Reunião** antes de receber a recompensa de **Visitando o Ancião**, e **Memórias** antes de entregar **O Guardião do Templo**. Essas antecipações evitam perder o nível máximo de aceite com a XP ×3. O nível previsto considera XP das quests; abates e bônus do servidor podem elevar o nível real. Se já passou de um limite de aceite, use **Pular etapa** ou **Etapas** para reposicionar o guia.

## Atalhos

O menu do ícone na bandeja contém **Estado das missões por memória > Sincronizar e avançar automaticamente**. O atalho `Ctrl + Shift + A` alterna essa opção. Ela começa desligada em uma instalação nova e mantém sua preferência nas próximas sessões.

O avanço reconhece aceites, transições de submissões, coletas no inventário de missão, abates de um único tipo de monstro e entregas previamente observadas, após duas leituras iguais. Cada cliente tem seu estado independente. Quando uma etapa não tiver um ID inequívoco ou contador suportado, use **Etapa feita**. Objetivos de abate com vários tipos de monstro ou ações mistas de matar e entregar continuam manuais. A leitura usa o perfil do cliente The Classic 1.4.4; outros executáveis podem exigir offsets próprios.

- `Ctrl + Alt + Seta Direita`: concluir.
- `Ctrl + Alt + Seta Esquerda`: voltar.
- `Ctrl + Alt + Seta para Baixo`: pular.
- `Ctrl + Alt + M`: criar o marcador da etapa atual. Se esse atalho já pertencer a outro programa, o overlay usa `Ctrl + Shift + M`; o menu do ícone informa qual ficou ativo.

Os atalhos avançam, voltam ou pulam uma etapa. Eles só modificam o progresso quando um cliente está ativo e o overlay não foi ocultado pelo usuário.

## Etapas e marcador

O título mostra `Missão › Submissão`, por exemplo `Pérola de Lágrimas › Reportar ao Guarda Lufu`. A etiqueta colorida informa o tipo e a linha seguinte descreve a ação corrente. O canto inferior mostra **Nv. previsto** e **Etapa X/Y**. A previsão soma as recompensas XP ×3 cadastradas para missões concluídas e aplica a curva de evolução do cliente. Etapas puladas não geram XP. XP extra de combate ou diárias pode deixar o personagem acima dessa previsão. O nível lido do jogo orienta a escolha de missões disponíveis. Concluir uma etapa mantém as seguintes pendentes.

Ao clicar em **Marcar**, não mova o mouse nem troque de cliente por cerca de um segundo. O overlay:

1. abre o painel **Marcadores** do cliente em foco;
2. seleciona e exclui somente o `PWOverlay` anterior, quando ele existe;
3. digita a coordenada no formato aceito pelo cliente, como `433, 887`;
4. cria `PWOverlay` e fecha o painel.

O fluxo padrão foi calibrado com o cliente 1.4.4 em 1920×1080 borderless e o PW Helper aberto. Outras resoluções são escaladas proporcionalmente. Remoção, confirmação da exclusão por teclado e criação são feitas dentro do mesmo clique; o painel é normalizado entre as fases.

Versões anteriores a 0.7.1 usavam uma área vazia como referência da lista e podiam acumular `PWOverlay`. A configuração padrão é migrada automaticamente. Marcadores duplicados já existentes devem ser removidos uma única vez; depois disso, cada clique mantém somente o marcador mais recente.

Para ajustar a resolução ou escala de interface, escolha **Calibrar marcador...** no menu da bandeja. A janela aguarda o PW em foco. Selecione uma linha, coloque o PW em foco, posicione o mouse sobre o controle indicado e pressione `F8`. O calibrador mostra o PID e o tamanho da área do jogo e avança para a próxima linha a cada captura. Abra Marcadores manualmente para capturar os controles internos. Depois volte pelo Alt+Tab e clique em **Salvar**. Os pontos ficam vinculados ao mesmo cliente e tamanho durante a captura. Se mudar a resolução no meio, restaure o padrão ou reabra a calibração. O menu **Ao criar marcador** permite substituir somente `PWOverlay` ou apagar todos.

## Selecionar uma etapa

Clique em **Etapas**, escolha a missão e a etapa e confirme. Etapas anteriores sem conclusão confirmada são marcadas como puladas. As posteriores são reabertas. Conclusões anteriores são preservadas no cálculo de XP. A escolha também permite abrir uma missão dispensada pela rota reduzida.

## Backup e edição das rotas

No menu do ícone ao lado do relógio:

- **Exportar backup de perfis...** salva progresso, associações, posições e preferências em um JSON.
- **Restaurar backup de perfis...** substitui o estado atual após confirmação.
- **Editar rotas...** permite alterar missão, nível mínimo, nível previsto, título da submissão, ação, ordem e coordenadas. **Salvar JSON + Excel** cria cópias `.bak` e sincroniza os dois arquivos. A sincronização da planilha exige Microsoft Excel instalado.

## Orientação de forja do nível 5

Ao chegar ao nível 5, o jogo abre uma missão automática para falar com o Ancião sobre as habilidades de forja. Ela está na rota como a primeira tarefa prevista desse nível. As missões específicas de Ferreiro, Alfaiate, Artesão e Boticário continuam fora do guia.

## Mover o painel

1. Clique em **Fixado** para mudar o botão para **Mover**, caso necessário.
2. Arraste pela barra azul.
3. Clique em **Mover** para fixar novamente.

A posição é salva separadamente para cada monitor e resolução. Use **Restaurar posição** no menu do ícone ao lado do relógio se o painel ficar fora da área desejada.

## Modo compacto

Clique em `−` na barra azul para reduzir o painel. A versão compacta mantém personagem/classe, próxima missão, ação ou NPC, nível previsto, progresso e os botões **Voltar**, **Etapa feita** e **Marcar**. Use `+` para restaurar o painel completo. A opção **Modo compacto** também fica disponível no menu do ícone ao lado do relógio e é preservada ao reiniciar ou restaurar um backup.

O modo compacto usa a mesma posição do painel completo. Se a mudança de tamanho aproximar o overlay do PW Helper, desbloqueie **Mover** antes de reduzir ou use **Restaurar posição**.

A transparência pode ser alterada no menu do ícone ao lado do relógio entre 65%, 75%, 82%, 90% e 100%. O padrão recomendado é 82%, cerca de 10 pontos mais transparente que o layout anterior.

## PW Helper

Enquanto estiverem abertos, todos os painéis flutuantes visíveis do PW Helper são reelevados periodicamente para permanecerem acima do jogo, qualquer que seja o título. A janela principal `PW Helper` não é fixada. Isso não transfere o foco de teclado ao Helper; os comandos continuam indo para o cliente ativo.

## Encerrar

O botão X somente oculta. Para encerrar de verdade:

1. Clique com o botão direito no ícone ao lado do relógio.
2. Escolha **Sair**.

## Reiniciar um personagem

Use **Reiniciar progresso deste personagem** no menu do ícone. O programa pede confirmação antes de apagar as marcações.

## Onde o progresso fica salvo

```text
%LOCALAPPDATA%\PWQuestOverlay\state.json
```

Prefira **Exportar backup de perfis...** e **Restaurar backup de perfis...** para transferir progresso e configuração. Consulte `INSTALACAO.txt`, incluído no pacote, para o procedimento completo.
