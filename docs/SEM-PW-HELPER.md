# Funcionamento sem PW Helper

PW Helper é opcional. O overlay, o marcador, a leitura de missões e o Windowed Fullscreen usam diretamente os clientes `elementclient`, sem consultar arquivos ou APIs do Helper.

## Uso

1. Abra o jogo normalmente e entre nos personagens.
2. Aplique as funções desejadas na Central e coloque um cliente em foco.
3. Em **Perfis**, identifique a linha pelo PID/título. Informe nome e classe ou escolha um perfil salvo e use **Usar na linha selecionada**.
4. Salve e repita para as demais janelas. Alternar o foco troca a rota e o progresso do personagem associado.

O nome é o identificador do perfil informado por você, não uma leitura automática do nome real no jogo. Use sempre o mesmo nome/classe para retomar o progresso. Se houver personagens de mesmo nome/classe em servidores diferentes, adicione um identificador diferente ao nome do perfil.

Após reiniciar o overlay ou reabrir o cliente, selecione novamente os perfis salvos. A janela é identificada por PID, início do processo e HWND apenas na sessão atual. Não restauramos um PID salvo porque ele pode pertencer a outro cliente. Progresso, cultivo, posição e preferências continuam salvos.

Ao trocar de personagem na mesma janela, refaça a associação antes de continuar. Com a leitura ativada, uma nova sessão detectada também invalida a associação anterior; não há offsets novos, ainda não verificados, para descobrir nome/classe. Falhas repetidas de leitura podem exigir selecionar o perfil novamente. Sem leitura, a reassociação depende do usuário.

## Compatibilidade e proteção de progresso

- Títulos reconhecíveis como `Nome[EP]` continuam funcionando automaticamente, inclusive quando criados pelo Helper.
- Associações por título antigas continuam no menu de compatibilidade; títulos padrão e repetidos não são aceitos como identidade.
- Duas janelas com título igual têm linhas e associações independentes.
- Perfis duplicados em clientes simultâneos são recusados. Um cliente sem identidade não conclui/pula etapas, não cria marcadores e não avança por memória.
- Trocar a associação ou o personagem identificado pelo título descarta as evidências de memória anteriores daquele cliente. Uma falha de leitura deixa a sessão indisponível até a próxima confirmação estável; dados antigos não são usados para avançar missões.
- O jogo e suas janelas não são renomeados. O Helper não é instalado, iniciado ou exigido pelo programa.

## Revisão e validação

O código de enumeração/foco, associação, marcador, persistência e leitura foi revisado para localizar dependências do Helper. As rotinas que elevam seus painéis já retornam uma lista vazia quando ele não está aberto e continuam opcionais.

`scripts/test.ps1` cobre títulos genéricos/iguais, perfis distintos, troca de ordem, PID/HWND reutilizados, títulos das dez classes, aliases antigos, duplicidade de perfil, troca de token e descarte de evidências antigas. `scripts/test-overlay-interactions.ps1` cobre a linha do cliente ativo, aplicação de perfil salvo somente na linha escolhida, liberação do modo manual, resultado independente por janela, token nativo de processo e regressões de arraste/cultivo. `scripts/test-package.ps1` confere a distribuição.

Essa validação não substitui o teste de campo. Nesta revisão não havia clientes reais abertos: a troca de foco/conta e a leitura de memória com o jogo precisam ser confirmadas no servidor, com e sem Helper. As rotas e seus IDs de progresso não foram alterados.
