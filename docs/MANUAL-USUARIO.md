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

Siglas aceitas: `WR`, `WB`, `WF`, `EA`, `EP` e `MC`.

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

As etapas seguem uma ordem global otimizada. O overlay pode mandar pegar uma missão, depois outra missão próxima, realizar os dois objetivos e somente mais tarde voltar às entregas. O nome da missão muda conforme a ação atual, mas cada missão mantém seu progresso independente.

Do nível 1 ao 60, a sequência foi organizada por região e dependências. Até a Cidade do Dragão, cada raça segue seu circuito; depois, todas usam a rota compartilhada e fazem apenas desvios curtos para missões exclusivas. Pop-ups como **Aprenda sobre Habilidades de Forja** aguardam o retorno natural ao Ancião; não é necessário interromper o circuito apenas para entregá-los.

Nas faixas de DG, aceite primeiro cultivo, preparação e moeda que possam compartilhar a mesma entrada. A rota pode deixar uma cadeia longa aberta e retomá-la mais tarde; siga a ação exibida, não apenas o nível mínimo da missão.

## Atalhos

O menu do ícone na bandeja contém **Estado das missões por memória > Sincronizar e avançar automaticamente**. O atalho `Ctrl + Shift + A` alterna essa opção. Ela começa desligada em uma instalação nova e mantém sua preferência nas próximas sessões.

O avanço reconhece aceites, transições de submissões, coletas no inventário de missão e entregas previamente observadas, após duas leituras iguais. Cada cliente tem seu estado independente. Quando uma etapa não tiver um ID inequívoco ou contador suportado, use **Etapa feita**. Contadores de abates sem item de missão ainda precisam desse avanço manual. A leitura usa o perfil do cliente The Classic 1.4.4; outros executáveis podem exigir offsets próprios.

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
