# Manual do usuário

## Iniciar

1. Abra o PW Borderless Monitor e o PW Helper normalmente.
2. Abra um ou mais clientes do Perfect World.
3. Se desejar detecção automática, nomeie cada janela no PW Helper com a classe no final, como `Nome[EP]` ou `Nome[WB]`.
4. Execute `PW Quest Overlay.exe`.
5. Coloque um cliente em foco. O painel aparecerá automaticamente.

O overlay fica oculto quando outro aplicativo, como navegador ou Discord, está em foco.

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
- **Perfis:** abre a associação entre título/nome do cliente e classe.
- **X:** oculta o painel sem encerrar o programa.

## Atalhos

- `Ctrl + Alt + Seta Direita`: concluir.
- `Ctrl + Alt + Seta Esquerda`: voltar.
- `Ctrl + Alt + Seta para Baixo`: pular.
- `Ctrl + Alt + M`: criar o marcador da etapa atual. Se esse atalho já pertencer a outro programa, o overlay usa `Ctrl + Shift + M`; o menu do ícone informa qual ficou ativo.

Os atalhos avançam, voltam ou pulam uma etapa. Eles só modificam o progresso quando um cliente está ativo e o overlay não foi ocultado pelo usuário.

## Etapas e marcador

O título mostra `Etapa X/Y`. A etiqueta colorida informa o tipo da etapa e a linha principal descreve a ação corrente. O canto inferior mostra o **nível previsto** para aquela missão; o overlay não pede nem salva o nível real do personagem. Concluir uma etapa não pula o restante da mesma missão.

Ao clicar em **Marcar**, não mova o mouse nem troque de cliente por cerca de um segundo. O overlay:

1. abre o painel **Marcadores** do cliente em foco;
2. seleciona e exclui somente o `PWOverlay` anterior, quando ele existe;
3. digita a coordenada no formato aceito pelo cliente, como `433, 887`;
4. cria `PWOverlay` e fecha o painel.

O fluxo padrão foi calibrado com o cliente 1.4.4 em 1920×1080 borderless e o PW Helper aberto. Outras resoluções são escaladas proporcionalmente. Remoção e criação são feitas dentro do mesmo clique; o painel é normalizado entre as duas fases.

Para ajustar uma escala diferente, abra o menu do ícone ao lado do relógio e escolha **Calibrar marcador...**. Selecione uma linha, deixe o painel de Marcadores aberto, posicione o mouse sobre o controle indicado e pressione `F8`. Repita os controles e salve. O menu **Ao criar marcador** alterna entre substituir somente `PWOverlay` (recomendado) ou apagar todos.

## Selecionar uma etapa

Clique em **Etapas**, escolha a missão e a etapa e confirme. Use essa função quando o personagem já avançou no jogo ou quando uma cadeia começou em uma etapa posterior. O programa marca as etapas anteriores como concluídas para manter os botões **Voltar** e **Etapa feita** coerentes.

## Backup e edição das rotas

No menu do ícone ao lado do relógio:

- **Exportar backup de perfis...** salva progresso, associações, posições e preferências em um JSON.
- **Restaurar backup de perfis...** substitui o estado atual após confirmação.
- **Editar rotas...** permite alterar missão, nível mínimo, nível previsto, textos, ordem das etapas e coordenadas. **Salvar JSON + Excel** cria cópias `.bak` e sincroniza os dois arquivos. A sincronização da planilha exige Microsoft Excel instalado.

## Orientação de forja do nível 5

Ao chegar ao nível 5, o jogo abre uma missão automática para falar com o Ancião sobre as habilidades de forja. Ela está na rota como a primeira tarefa prevista desse nível. As missões específicas de Ferreiro, Alfaiate, Artesão e Boticário continuam fora do guia.

## Mover o painel

1. Clique em **Fixado** para mudar o botão para **Mover**, caso necessário.
2. Arraste pela barra azul.
3. Clique em **Mover** para fixar novamente.

A posição é salva separadamente para cada monitor e resolução. Use **Restaurar posição** no menu do ícone ao lado do relógio se o painel ficar fora da área desejada.

## PW Helper

Enquanto o painel `TODOS` estiver aberto, o tracker o reeleva periodicamente para que permaneça acima do jogo. Isso não transfere o foco de teclado ao Helper; os comandos continuam indo para o cliente ativo.

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

Faça uma cópia desse arquivo para transferir o progresso para outro computador.
