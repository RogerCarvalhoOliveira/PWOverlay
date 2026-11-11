# Funcionamento sem PW Helper

PW Helper é opcional. O overlay, o marcador, a leitura de missões e o Windowed Fullscreen usam diretamente os clientes `elementclient`, sem consultar arquivos ou APIs do Helper.

## Identificação automática (0.12.0)

1. Entre no personagem e aplique o overlay pela Central.
2. Coloque o cliente em foco e aguarde duas leituras iguais do nome e da classe.
3. Alterne entre clientes: mesmo com títulos padrão iguais, cada janela usa sua própria identidade e progresso.

A identificação funciona mesmo com o avanço automático de missões desligado. Lê nome real UTF-16, ID e classe do jogador; não altera o nome da janela nem grava na memória do jogo. O ID só identifica trocas na sessão; o progresso continua salvo por NOME|CLASSE.

Ao reiniciar, a identificação automática retoma o perfil com o mesmo nome/classe. Se um título antigo retirava um sufixo como `[EP]` do nome real, o perfil correspondente da mesma classe é migrado, preservando suas marcações, quando não existe um perfil com o nome completo.

A versão 0.13.0 inclui a identificação por memória e a importação das missões concluídas registradas no histórico. O teste com personagens conectados está pendente.

## Correção manual e perfis personalizados

Em **Perfis**, identifique a linha pelo PID/título, informe nome e classe ou escolha um perfil salvo. A associação manual tem prioridade sobre memória e título. **Automático** remove essa escolha e volta a usar a memória, com título como alternativa. A dica da coluna Classe informa a classe e a origem detectadas.

Para personagens de mesmo nome/classe em servidores diferentes, use nomes personalizados de perfil. A associação manual usa PID, início do processo e HWND apenas na sessão atual; selecione novamente o perfil personalizado após reiniciar. Esses identificadores não são gravados para reutilização, pois o Windows pode reciclá-los.

## Proteção de progresso

- Memória tem prioridade sobre títulos antigos do Helper; títulos genéricos/duplicados não criam perfis.
- A troca do ID/nome/classe ou do objeto do jogador invalida as evidências de missão e a associação da sessão anterior. Duas leituras confirmam a nova identidade antes de ler suas missões.
- Leituras inválidas do nome não publicam identidade automática. Falhas de missão invalidam as evidências, preservando a identidade confirmada enquanto a sessão permanece válida.
- Dois clientes resolvidos para o mesmo NOME|CLASSE são bloqueados. Use perfis personalizados para diferenciá-los.
- Sem identidade, não é possível concluir/pular etapas nem criar marcadores. A leitura nunca renomeia janelas ou modifica a memória.

## Validação

`scripts/test.ps1` testa ReadProcessMemory em um processo x86 de teste, nomes acentuados, dez classes, comprimento/terminador UTF-16 inválidos, troca durante a leitura, confirmação dupla, mudança de ID no mesmo objeto, logout e identificação com missões desativadas. Também cobre títulos iguais, precedência manual/memória/título, perfis duplicados e compatibilidade do progresso.

`scripts/test-overlay-interactions.ps1` verifica preenchimento do nome e dica da classe em **Perfis**, retorno a **Automático**, salvamento sem criar associação manual e regressões de controles/arraste. `scripts/test-package.ps1` confere a distribuição. Consulte `docs/IDENTIDADE-MEMORIA.md` para os offsets e limites da validação.
