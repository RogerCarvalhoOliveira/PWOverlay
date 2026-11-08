# Roteiro de testes

## Teste automático mínimo

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
```

Confirme que `dist` contém os dois executáveis, as rotas, o manual e o ZIP.

## Um cliente

1. Abra o monitor borderless, o PW Helper e um cliente.
2. Dê ao cliente um título como `Teste[EP]`.
3. Inicie o overlay e coloque o cliente em foco.
4. Confirme personagem, classe, missão, texto e contador da etapa.
5. Avance uma etapa, volte e pule; reinicie o overlay e confirme a persistência.
6. Em uma missão com várias etapas, confirme que a missão seguinte só aparece após a última.

## Marcador

1. Use uma etapa com coordenadas e deixe o painel **Marcadores** fechado.
2. Crie manualmente um marcador de teste no jogo.
3. Clique em **Marcar** e não mova o mouse por cerca de um segundo.
4. Abra o painel do jogo e confirme que há somente `PWOverlay` com a coordenada da etapa atual.
5. Troque de cliente, clique em **Marcar** e confirme que o marcador foi criado apenas no cliente em foco.
6. Use uma etapa sem coordenadas e confirme que o botão fica desativado.

## Associação manual

1. Use um título sem sigla, como `TesteClerigo`.
2. Abra **Perfis**, associe a `EP` e salve.
3. Troque o foco e confirme a rota de EP.
4. Reinicie o overlay e confirme que a associação permaneceu.
5. Volte a associação para **Automático** e confirme que o perfil fica sem classe até o título fornecer uma sigla.

## Multiclient

1. Abra dois clientes com títulos diferentes e classes diferentes.
2. Marque uma etapa apenas no primeiro.
3. Alterne o foco várias vezes e confirme que missão e progresso acompanham o cliente correto.
4. Clique no overlay e no painel `TODOS`; confirme que o teclado continua direcionado ao jogo ao voltar.
5. Confirme que o Helper permanece visível acima do cliente e que o overlay não o cobre indevidamente.

## Posição e monitores

Desbloqueie, arraste e fixe o painel. Reinicie e confira a posição. Se houver outro monitor, mova o cliente e verifique que a posição é salva separadamente. Teste também **Restaurar posição**.

## Compatibilidade de estado

Faça backup de `%LOCALAPPDATA%\PWQuestOverlay\state.json`, teste com uma cópia anterior à propriedade `ClassAssociations` e confirme que o programa inicia sem apagar as missões concluídas.
