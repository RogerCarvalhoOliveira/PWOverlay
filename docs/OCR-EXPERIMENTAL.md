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

O teste não conclui, pula nem volta etapas. A calibração é salva proporcionalmente à resolução do cliente e faz parte do backup do estado.

## Diagnóstico

A captura original fica em `%LOCALAPPDATA%\PWQuestOverlay\ocr-last-source.png`. A imagem filtrada realmente enviada ao Tesseract fica em `ocr-last.png`. O filtro preserva as cores de texto do quest log e remove o cenário visto através do painel transparente. O log registra duração, quantidade de caracteres e região capturada.

O protótipo faz duas leituras em paralelo: a imagem filtrada fornece os nomes sem ruído e a captura original recupera contadores como `(1/10)`. Títulos numerados com alta semelhança são normalizados contra os nomes da rota. Quando a ação indica um total, mas o valor atual permanece ambíguo, o resultado usa `?/total` em vez de adivinhar.

Para uma leitura confiável:

- selecione somente a área útil do quest log;
- evite incluir minimapa, chat e nomes de personagens;
- mantenha a mesma escala de interface entre os clientes;
- deixe o texto totalmente visível, sem outro painel sobreposto.

## Runtime

O protótipo usa Tesseract 5 com `por.traineddata`. Durante o empacotamento, `scripts\build.ps1` copia o executável, as DLLs necessárias, o idioma português e os avisos de licença para `dist\ocr`. O aplicativo também aceita uma instalação em `C:\Program Files\Tesseract-OCR` durante o desenvolvimento.

## Próxima etapa

Depois de testar diferentes tipos de missão, será possível decidir um limite de confiança. Somente correspondências acima desse limite deverão sugerir ou aplicar mudanças de progresso; resultados ambíguos continuarão pedindo confirmação.
