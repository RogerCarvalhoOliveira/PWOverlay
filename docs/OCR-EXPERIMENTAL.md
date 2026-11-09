# OCR experimental do quest log

Esta função está isolada na branch `feature/ocr-quest-state`. Ela serve para medir a qualidade e o tempo de leitura antes de permitir qualquer avanço automático de etapas.

## Como testar

1. Abra um cliente do Perfect World e deixe o diário de missões visível.
2. Coloque esse cliente em foco para o overlay associar o personagem correto.
3. No ícone do **PW Quest Overlay** ao lado do relógio, abra **OCR experimental**.
4. Use **Selecionar área e testar...** e arraste somente sobre a lista ou descrição das missões.
5. Aguarde a janela de resultado. Ela mostra o texto reconhecido, o tempo total e a coincidência com a etapa esperada da rota.
6. Para repetir sem recalibrar, use **Testar área salva**.

O teste não conclui, pula nem volta etapas. A calibração é salva proporcionalmente à resolução do cliente e faz parte do backup do estado.

## Diagnóstico

A última imagem enviada ao OCR fica em `%LOCALAPPDATA%\PWQuestOverlay\ocr-last.png`. O log registra duração, quantidade de caracteres e região capturada.

Para uma leitura confiável:

- selecione somente a área útil do quest log;
- evite incluir minimapa, chat e nomes de personagens;
- mantenha a mesma escala de interface entre os clientes;
- deixe o texto totalmente visível, sem outro painel sobreposto.

## Runtime

O protótipo usa Tesseract 5 com `por.traineddata`. Durante o empacotamento, `scripts\build.ps1` copia o executável, as DLLs necessárias, o idioma português e os avisos de licença para `dist\ocr`. O aplicativo também aceita uma instalação em `C:\Program Files\Tesseract-OCR` durante o desenvolvimento.

## Próxima etapa

Depois de testar diferentes tipos de missão, será possível decidir um limite de confiança. Somente correspondências acima desse limite deverão sugerir ou aplicar mudanças de progresso; resultados ambíguos continuarão pedindo confirmação.
