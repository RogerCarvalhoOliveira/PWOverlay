# Estado das missões por memória

A versão 0.8.0 incorpora a sincronização opcional do estado de missões no cliente x86 The Classic Perfect World 1.4.4. O leitor abre o `elementclient.exe` com permissão de consulta e leitura, resolve o personagem por ponteiro configurado ou RTTI (`CECHostPlayer`) e valida nível, HP e MP. O nome deste arquivo foi preservado para manter links antigos.

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
- em coletas filhas aninhadas, item completo mais presença da própria missão ou de um ancestral real ativo; o filho pode já ter desaparecido automaticamente;
- primeiro contador de abates para objetivos dataminados com um único monstro, sem drops, até 255 abates (a partir da versão 0.8.1);
- desaparecimento depois de presença confirmada para conversa ou entrega;
- próxima submissão da mesma cadeia ativa para recuperar uma transição ocorrida antes de abrir o overlay.

O toggle fica em `Estado das missões por memória > Sincronizar e avançar automaticamente`. O atalho `Ctrl + Shift + A` liga ou desliga essa função. O modo é separado por cliente e usa o PID associado à janela de jogo em foco.

No teste validado de `Aldeões Feridos`, a submissão `Besouro de Chifres` (ID 936) avançou ao encontrar o item 3502 na quantidade 10. A entrega avançou quando o ID 936, previamente observado, desapareceu da lista ativa.

## Próximas etapas

As próximas validações devem ampliar a cobertura sem inferências arriscadas:

1. ampliar a leitura de abates para objetivos com vários tipos de monstro;
2. validar objetivos com mais de um item ou mais de um tipo de condição;
3. mapear explicitamente IDs para etapas cujos nomes são duplicados no catálogo;
4. testar todas as raças e mais de um cliente simultaneamente;
5. manter o modo desligado por padrão até a cobertura das rotas ser maior.

Ausência de um ID nunca será tratada sozinha como missão entregue: o leitor precisará distinguir “nunca aceita”, “ativa” e “já finalizada” por estruturas independentes do cliente.

Na versão 0.8.1, `Confiança dos Guardas` (ID 2714) foi validada com 10 abates do monstro 1037. O primeiro contador ocupa dois bytes no offset `0x15` da entrada ativa de 32 bytes. A assinatura de estabilidade inclui essa contagem, de modo que uma mudança no contador seja confirmada mesmo sem alteração na lista de IDs. Somente uma ação exclusivamente de matar avança por essa evidência; entregar ao NPC exige sua própria transição.

Na versão 0.8.2, a cadeia `Livro de Fundição` é resolvida como 80 → 82 → 85/86. A lenha usa item 2377 (10 unidades), e o ferro usa item 2375 (10 unidades). O título genérico do pai não identifica o objetivo de cada material; os títulos das etapas usam os filhos dataminados. A presença dos itens, sem a missão ou um ancestral correspondente ativo, nunca basta para concluir a coleta.

Na versão 0.8.4, o catálogo registra `children_in_order`, extraído de `m_bExeChildInOrder`. Se o filho de objetivo já desapareceu antes da conexão, a presença de um irmão posterior só comprova sua conclusão quando o mesmo pai exige ordem sequencial. Filhos paralelos não servem como prova. A validação reproduz o WB com pai 1177 e sucessor 1179 ativos, sem o objetivo 1178; as duas sessões de cliente são independentes por PID.
