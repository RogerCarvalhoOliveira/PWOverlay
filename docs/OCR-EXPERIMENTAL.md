# OCR experimental do quest log

Esta função está isolada na branch `feature/ocr-quest-state`. Ela serve para medir a qualidade e o tempo de leitura antes de permitir qualquer avanço automático de etapas.

## Como testar

1. Abra um cliente do Perfect World e deixe o diário de missões visível.
2. Coloque esse cliente em foco para o overlay associar o personagem correto.
3. Com o jogo ainda em foco, pressione **Ctrl + Shift + O**.
4. Arraste somente sobre a lista ou descrição das missões.
5. Aguarde a janela de resultado. Ela mostra o texto reconhecido, o tempo total e a coincidência com a etapa esperada da rota.
6. Para repetir sem recalibrar e sem sair do jogo, pressione **Ctrl + Alt + O**.

Os mesmos comandos continuam disponíveis no ícone do overlay ao lado do relógio, em **OCR experimental**.

## Sincronização e avanço automáticos

Pressione **Ctrl + Shift + A** para ativar ou desativar o modo automático. Quando ativo e o cliente estiver em foco, o overlay lê silenciosamente a área salva a cada dois segundos. Um resultado é descartado se o usuário trocar de cliente durante a captura.

O avanço exige duas leituras interpretadas iguais. Nesta fase, são concluídas automaticamente somente:

- a etapa de aceitar quando a missão canônica aparece no quest log;
- uma etapa de objetivo quando o contador único chega ao total esperado;
- uma etapa de objetivo quando `Missão Concluída` aparece dentro do bloco canônico correto.

Etapas de conversa usam também os nomes reais das submissões, como `Visite o Ferreiro`, `Visite o Negociante` e `Visite o Boticário`. Isso permite sincronizar cadeias cujo nome principal não aparece no tracker.

Uma entrega somente avança por desaparecimento quando o OCR já havia visto a missão pronta para entregar. Depois disso, o bloco precisa desaparecer em duas leituras estáveis enquanto outra entrada conhecida comprova que o tracker continua legível. Ausência sem presença anterior nunca é tratada como missão feita. Objetivos com vários contadores também exigem `Missão Concluída`, evitando concluir a etapa quando somente um dos alvos terminou.

O teste não conclui, pula nem volta etapas. A calibração é salva proporcionalmente à resolução do cliente e faz parte do backup do estado.

## Diagnóstico

A captura original fica em `%LOCALAPPDATA%\PWQuestOverlay\ocr-last-source.png`. A imagem filtrada realmente enviada ao Tesseract fica em `ocr-last.png`. O filtro preserva as cores de texto do quest log e remove o cenário visto através do painel transparente. O log registra duração, quantidade de caracteres e região capturada.

O protótipo faz duas leituras em paralelo: a imagem filtrada fornece os nomes sem ruído e a captura original recupera contadores como `(1/10)`. Títulos numerados com alta semelhança são normalizados contra os nomes da rota. Quando a ação indica um total, mas o valor atual permanece ambíguo, o resultado usa `?/total` em vez de adivinhar.

Antes de cada OCR são capturados três quadros com intervalo de 80 ms. O filtro mantém pixels de texto presentes em pelo menos dois deles e usa a mediana para a leitura original, reduzindo mobs, jogadores e efeitos em movimento. Por personagem, um estado somente fica **confirmado** depois de duas leituras interpretadas idênticas em até 30 segundos; uma leitura isolada nunca deve avançar progresso automaticamente.

Para uma leitura confiável:

- selecione somente a área útil do quest log;
- evite incluir minimapa, chat e nomes de personagens;
- mantenha a mesma escala de interface entre os clientes;
- deixe o texto totalmente visível, sem outro painel sobreposto.

## Runtime

O protótipo usa Tesseract 5 com `por.traineddata`. Durante o empacotamento, `scripts\build.ps1` copia o executável, as DLLs necessárias, o idioma português e os avisos de licença para `dist\ocr`. O aplicativo também aceita uma instalação em `C:\Program Files\Tesseract-OCR` durante o desenvolvimento.

## Próxima etapa

Depois de testar diferentes tipos de missão, será possível decidir um limite de confiança. Somente correspondências acima desse limite deverão sugerir ou aplicar mudanças de progresso; resultados ambíguos continuarão pedindo confirmação.
