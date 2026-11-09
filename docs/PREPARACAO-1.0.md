# Preparação para distribuição geral

## O que já está disponível

- Instalador por usuário, desinstalador, ZIP portátil e hashes SHA-256.
- Atualização da instalação existente e ciclo de instalar/atualizar/desinstalar com ferramentas abertas validados no PC atual, com teste isolado de AppId próprio. Link de desinstalação disponível na Central e na bandeja.
- Central para aplicar ou parar Overlay/Windowed Fullscreen; o PW é aberto manualmente ou pelo Helper.
- Associação manual de classe, configuração de transparência, posição e calibração de marcador.
- Uso independente do PW Helper, com associação por cliente e recuperação de perfis salvos. Helper, quando utilizado, mantém a integração de títulos/painéis.
- Progresso e cultivo por personagem, exportação/restauração de backup e manutenção dos IDs nas atualizações.
- Guias para dez classes até 105, ramos God/Evil e liberação dos acessos ao Passado com Dusk.
- Ícone Órbita de progresso nos executáveis, bandeja, Central e instalador.
- Versão de build em `VERSION`, metadados dos executáveis e versão visível no menu da bandeja.
- Verificações de rotas, estado, controles e pacote. A distribuição não precisa de Python, Node, Excel ou Tesseract para funcionar; o editor sincroniza a planilha pelo Excel quando disponível.

## Verificações pendentes antes de chamar de 1.0

| Prioridade | Trabalho | Critério para concluir |
| --- | --- | --- |
| Alta | Instalação em Windows sem o ambiente de desenvolvimento | Instalar como usuário comum em computador sem PW Helper, abrir dois clientes de títulos iguais, associar perfis, importar backup, atualizar e desinstalar preservando o progresso. Repetir com Helper opcional. |
| Alta | Percursos 61–105 e Guardiões no servidor | Jogar os novos roteiros, especialmente Dusk/Passado e cultivos God/Evil 89/99/100; registrar missão, submissão e ID de qualquer divergência. |
| Alta | Resoluções e escalas diferentes | Conferir marcador e interface em 100%, 125% e 150%, dois monitores e troca de cliente. Recalibrar o marcador na resolução alvo. |
| Alta | Escopo de compatibilidade | Identificar publicamente o cliente/servidor testado. Outra instalação chamada 1.4.4 pode usar traduções, missões e offsets diferentes; não anunciar compatibilidade universal sem testar. |
| Decisão do responsável | Divulgação e licença | Registrar a resposta da equipe do servidor sobre a divulgação e escolher a licença dos arquivos próprios do projeto. Esta revisão não escolhe uma licença nem presume aprovação da equipe. |

Os testes locais e sintéticos não equivalem a uma instalação em outro computador nem a um percurso completo jogado. Na sessão desta revisão, esses casos não foram executados em um Windows limpo.

## Melhorias posteriores

- Assinatura digital dos executáveis/instalador, se o projeto tiver distribuição ampla.
- Execução automática das verificações a cada alteração no Git.
- Assistente inicial que reúna associação de classe e calibração, caso testes com usuários mostrem dificuldade nessa configuração.

O projeto pode continuar sendo distribuído como versão anterior à 1.0, com seus limites descritos, enquanto a validação de campo acontece.

## Conferir um pacote

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
powershell -ExecutionPolicy Bypass -File scripts/test-overlay-interactions.ps1
powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Package -Installer
powershell -ExecutionPolicy Bypass -File scripts/test-package.ps1 -Installer
```

Para relatar problemas, informe versão do overlay, cliente/servidor, resolução/escala e nome/ID da missão e submissão. Logs podem conter nomes de personagens; revise o conteúdo antes de anexá-los publicamente. Não é necessário publicar o backup inteiro dos perfis.
