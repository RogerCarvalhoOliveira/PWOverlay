# Leitura de memória experimental

Esta branch implementa a sincronização experimental do estado de missões diretamente no cliente x86 do Perfect World 1.4.4. O leitor abre o `elementclient.exe` com permissão de consulta e leitura, resolve o personagem por ponteiro configurado ou RTTI (`CECHostPlayer`) e valida nível, HP e MP.

O leitor localiza `CECTaskInterface`, a lista ativa, o histórico e o inventário de missão. Os IDs são cruzados com `task_catalog_144.tsv`, catálogo derivado do `tasks.data` desta instalação, com 24.277 missões e submissões. O catálogo inclui os itens e quantidades exigidos por cada objetivo de coleta.

## Limites desta fase

- usa somente `PROCESS_VM_READ` e `PROCESS_QUERY_LIMITED_INFORMATION`;
- não escreve na memória, não injeta DLL e não altera o processo do jogo;
- só avança etapas quando encontra uma evidência validada e estável em duas leituras consecutivas;
- não depende do PW Helper em execução;
- aceita apenas o perfil `The Classic 1.4.4 x86` documentado em `data/memory_offsets.json`;
- offsets inválidos são rejeitados pela validação de nível, HP, MP, quantidade de missões e tamanho do inventário de missão.

O arquivo de offsets é versionado separadamente das rotas. Isso evita misturar IDs de missão com endereços específicos de uma build do cliente.

## Executar o diagnóstico

Com pelo menos um cliente aberto:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\probe-memory.ps1
```

Para cada cliente, a ferramenta mostra PID, título da janela, nível, HP, MP, IDs de missões ativas, itens do inventário de missão e IDs registrados no histórico de missões concluídas. Nenhuma credencial ou linha de comando do jogo é registrada.

Para acompanhar alterações de todos os clientes abertos:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\probe-memory.ps1 -Watch
```

O modo contínuo usa uma leitura a cada 500 ms e registra somente mudanças: missão ativada, alteração dos bits de estado, desaparecimento depois de ter sido observada e inclusão no histórico. O intervalo pode ser alterado, por exemplo com `-Interval 250`.

O byte de estado é mantido em formato bruto. Valores diferentes foram observados conforme o tipo de submissão (`0`, `3`, `6` e `9`), portanto nenhum bit isolado é interpretado como “objetivo concluído”. A automação usa somente evidências independentes:

- aparecimento do ID para aceite;
- item e quantidade exigidos presentes no inventário de missão para coleta;
- desaparecimento depois de presença confirmada para conversa ou entrega;
- próxima submissão da mesma cadeia ativa para recuperar uma transição ocorrida antes de abrir o overlay.

O toggle fica em `Estado por memória (experimental) > Sincronizar e avançar automaticamente`. O atalho `Ctrl + Shift + A` liga ou desliga essa função. O modo é separado por cliente e usa o PID associado à janela de jogo em foco.

No teste validado de `Aldeões Feridos`, a submissão `Besouro de Chifres` (ID 936) avançou ao encontrar o item 3502 na quantidade 10. A entrega avançou quando o ID 936, previamente observado, desapareceu da lista ativa.

## Próximas etapas

As próximas validações devem ampliar a cobertura sem inferências arriscadas:

1. identificar contadores de abates que não criam uma submissão sucessora;
2. validar objetivos com mais de um item ou mais de um tipo de condição;
3. mapear explicitamente IDs para etapas cujos nomes são duplicados no catálogo;
4. testar todas as raças e mais de um cliente simultaneamente;
5. manter o modo desligado por padrão até a cobertura das rotas ser maior.

Ausência de um ID nunca será tratada sozinha como missão entregue: o leitor precisará distinguir “nunca aceita”, “ativa” e “já finalizada” por estruturas independentes do cliente.
