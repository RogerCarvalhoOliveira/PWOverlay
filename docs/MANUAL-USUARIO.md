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
- **Marcar:** remove todos os marcadores do cliente ativo e cria um único marcador chamado `PWOverlay` na coordenada da etapa atual. Fica desativado quando a fonte não fornece coordenada.
- **Mover/Fixado:** alterna entre permitir arraste e bloquear a posição.
- **Perfis:** abre a associação entre título/nome do cliente e classe.
- **X:** oculta o painel sem encerrar o programa.

## Atalhos

- `Ctrl + Alt + Seta Direita`: concluir.
- `Ctrl + Alt + Seta Esquerda`: voltar.
- `Ctrl + Alt + Seta para Baixo`: pular.

Os atalhos avançam, voltam ou pulam uma etapa. Eles só modificam o progresso quando um cliente está ativo e o overlay não foi ocultado pelo usuário.

## Etapas e marcador

O título mostra `Etapa X/Y`. A linha principal descreve a ação corrente: pegar a missão, falar com um NPC, matar/coletar, aguardar ou entregar. Concluir uma etapa não pula o restante da mesma missão.

Ao clicar em **Marcar**, não mova o mouse nem troque de cliente por cerca de um segundo. O overlay:

1. abre o painel **Marcadores** do cliente em foco;
2. usa **Excluir todos os marcadores**;
3. digita a coordenada no formato aceito pelo cliente, como `433, 887`;
4. cria `PWOverlay` e fecha o painel.

O fluxo foi calibrado com o cliente 1.4.4 em 1920×1080 borderless e o PW Helper aberto. Outras resoluções são escaladas proporcionalmente; escala de interface personalizada pode exigir ajuste das coordenadas de referência em `OverlayForm.cs`.

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
