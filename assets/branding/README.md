# Ícone do PW Quest Overlay

Conceito escolhido pelo responsável pelo projeto: **Órbita de progresso**, segunda opção apresentada em 09/11/2026. O check representa uma etapa concluída; os pontos representam os próximos passos; os anéis conectam o percurso e remetem ao overlay.

`icon-source.png` preserva a arte aprovada, gerada pela ferramenta integrada imagegen. `PWOverlay.ico` contém versões de 16, 20, 24, 32, 40, 48, 64, 128 e 256 pixels, empacotadas sem alterar o desenho. O build incorpora o ICO nos executáveis e como recurso da interface; o instalador usa o mesmo arquivo.

Para regenerar o ICO, use Python com Pillow e execute `python scripts/build-icon.py`. Isso só é necessário ao trocar a arte; quem compila normalmente não precisa do Python ou do Pillow.

A fonte aprovada tem fundo branco, preservado também no ICO. Não contém marcas ou arquivos gráficos extraídos do jogo.
