# Manual do usuário

## Iniciar

1. Abra `PW Overlay Central.exe`.
2. Marque **Overlay** e use o botão **Windowed Fullscreen: ATIVADO/DESATIVADO** conforme desejar.
3. Clique em **Iniciar selecionados + PW**. Também é possível abrir os clientes normalmente e usar **Aplicar ao PW já aberto**.
4. Se desejar detecção automática, nomeie cada janela no PW Helper com a classe no final, como `Nome[EP]` ou `Nome[WB]`.
5. Coloque um cliente em foco. O painel aparecerá automaticamente quando o Overlay estiver ativado.

O overlay fica oculto quando outro aplicativo, como navegador ou Discord, está em foco.

O toggle de Windowed Fullscreen é aplicado imediatamente, sem precisar clicar em outro botão. Quando ligado, alcança clientes atuais e futuros. Quando desligado, restaura bordas, tamanho e posição anteriores dos clientes que o monitor acompanhou.

Para jogar sem nenhuma ferramenta, use **Abrir somente o PW** no PW Overlay Central. Esse botão encerra apenas `PW Quest Overlay` e `PW Borderless`; não fecha clientes, PW Helper nem altera o jogo. Você também pode abrir o launcher original do PW diretamente, desde que o instalador não tenha sido configurado para iniciar o overlay automaticamente.

## Identificação de personagem

O formato recomendado é:

```text
Personagem[CLASSE]
```

Siglas aceitas: `WR` (Guerreiro), `MG` (Mago), `WB` (Bárbaro), `WF` (Feiticeira), `EA` (Arqueiro), `EP` (Sacerdote), `MC` (Mercenário), `ES` (Espiritualista), `SK` (Arcano) e `MS` (Místico).

Exemplos:

- `Cauton[EP]`
- `Cauton[WB]`
- `Arqueira[EA]`

O identificador interno combina personagem e classe. Portanto, `Cauton[EP]` e `Cauton[WB]` mantêm progressos separados.

Se o nome não tiver a sigla, clique em **Perfis** na barra superior. A tabela mostra os clientes abertos e a classe detectada. Selecione a classe correta na coluna **Associação manual** e clique em **Salvar**.

Também é possível digitar manualmente um título ou nome na última linha. A associação fica salva e será reaplicada sempre que uma janela com o mesmo título for encontrada. Para voltar à detecção pelo nome, selecione **Automático**.

O menu do ícone ao lado do relógio também possui **Gerenciar perfis...** e uma opção rápida para definir a classe do cliente atual.

## Botões

- **Etapa feita:** marca somente a etapa exibida. Na última etapa, conclui a missão e mostra a próxima.
- **Voltar:** desfaz a última etapa concluída ou pulada.
- **Pular etapa:** ignora somente a etapa atual.
- **Marcar:** substitui o `PWOverlay` anterior e cria o novo na coordenada da etapa atual em uma única ação, preservando os demais marcadores. Fica desativado quando a fonte não fornece coordenada.
- **Etapas:** abre a lista completa da classe para escolher uma missão e uma etapa. A alteração pede confirmação e ajusta o progresso anterior.
- **Mover/Fixado:** alterna entre permitir arraste e bloquear a posição.
- **−/+**: reduz o painel para o modo compacto ou restaura a interface completa.
- **Perfis:** abre a associação entre título/nome do cliente e classe.
- **X:** oculta o painel sem encerrar o programa.

Para deixar o overlay em outro monitor, clique em **Mover**, mantenha o botão esquerdo pressionado na barra superior e arraste até a tela desejada. Solte o botão esquerdo para encerrar o arraste e salvar o monitor e a posição. Clique em **Fixado** para impedir novos arrastes. Alternar entre os clientes do PW continua atualizando o personagem, sem mover o painel de tela.

Para voltar a acompanhar a tela do cliente em foco, marque **Acompanhar monitor do PW** no menu do ícone ao lado do relógio. Se o monitor escolhido for desconectado, o painel aparece temporariamente na tela do jogo e recupera a preferência quando o monitor voltar.

As etapas seguem uma ordem global otimizada. O overlay pode mandar pegar uma missão, depois outra missão próxima, realizar os dois objetivos e somente mais tarde voltar às entregas. O nome da missão muda conforme a ação atual, mas cada missão mantém seu progresso independente.

Do nível 1 ao 105, a sequência foi organizada por região e dependências. Até a Cidade do Dragão, cada raça segue seu circuito; depois, todas usam a rota compartilhada e fazem os desvios exclusivos indicados. Pop-ups como **Aprenda sobre Habilidades de Forja** aguardam o retorno natural ao Ancião.

No ponto de escolha do cultivo 89, o overlay pergunta **God ou Evil** e salva a resposta somente para aquele personagem/classe. Sem resposta, o guia aguarda nessa etapa. Os botões Concluir/Pular abrem a escolha novamente. Após salvar, aparecem as missões compartilhadas e apenas as cadeias de 89, 99 e 100 do caminho escolhido. O menu **Escolher/alterar cultivo God ou Evil...** permite escolher antes ou corrigir a preferência. O jogo continua exigindo que você escolha o cultivo no NPC.

Na planilha, a coluna **Cultivo** permite manter **Ambos + God** ou **Ambos + Evil**. No overlay, o filtro é automático. Entre 100 e 105, complemente a história com as atividades repetíveis elegíveis e XP de combate; use Etapas para reabrir um circuito quando necessário. O nível previsto organiza a rota e a etapa final é confirmada manualmente ao atingir 105.

O cultivo 100 exige acesso às áreas do Passado. Como Dusk/Crepúsculo ficou reservado para outra etapa, o guia inclui uma conferência desse pré-requisito. Se os acessos estiverem pendentes, adie o cultivo pelo menu Etapas e continue a rota de XP. Veja `GUIA-61-105.txt`.

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

O título mostra `Missão › Submissão`, por exemplo `Pérola de Lágrimas › Reportar ao Guarda Lufu`. A etiqueta colorida informa o tipo e a linha seguinte descreve exatamente a ação corrente. O canto inferior mostra **Etapa X/Y**, nível previsto e posição global; o overlay não pede nem salva o nível real do personagem. Concluir uma etapa não pula o restante da missão.

Ao clicar em **Marcar**, não mova o mouse nem troque de cliente por cerca de um segundo. O overlay:

1. abre o painel **Marcadores** do cliente em foco;
2. seleciona e exclui somente o `PWOverlay` anterior, quando ele existe;
3. digita a coordenada no formato aceito pelo cliente, como `433, 887`;
4. cria `PWOverlay` e fecha o painel.

O fluxo padrão foi calibrado com o cliente 1.4.4 em 1920×1080 borderless e o PW Helper aberto. Outras resoluções são escaladas proporcionalmente. Remoção, confirmação da exclusão por teclado e criação são feitas dentro do mesmo clique; o painel é normalizado entre as fases.

Versões anteriores a 0.7.1 usavam uma área vazia como referência da lista e podiam acumular `PWOverlay`. A configuração padrão é migrada automaticamente. Marcadores duplicados já existentes devem ser removidos uma única vez; depois disso, cada clique mantém somente o marcador mais recente.

Para ajustar uma escala diferente, abra o menu do ícone ao lado do relógio e escolha **Calibrar marcador...**. Selecione uma linha, deixe o painel de Marcadores aberto, posicione o mouse sobre o controle indicado e pressione `F8`. Repita os controles e salve. O menu **Ao criar marcador** alterna entre substituir somente `PWOverlay` (recomendado) ou apagar todos.

## Selecionar uma etapa

Clique em **Etapas**, escolha a missão e a etapa e confirme. Use essa função quando o personagem já avançou no jogo ou quando uma cadeia começou em uma etapa posterior. O programa marca as etapas anteriores como concluídas para manter os botões **Voltar** e **Etapa feita** coerentes.

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
