# Leitura de memória experimental

Esta branch pesquisa a sincronização do estado de missões diretamente no cliente x86 do Perfect World 1.4.4. O código atual é somente um diagnóstico: ele abre o `elementclient.exe` com permissão de consulta e leitura, resolve o personagem por ponteiro configurado ou RTTI (`CECHostPlayer`) e valida nível, HP e MP.

O leitor já localiza `CECTaskInterface`, a lista ativa e o histórico. Os IDs são cruzados com `task_catalog_144.tsv`, catálogo derivado do `tasks.data` desta instalação, com 24.277 missões e submissões. O código do editor utilizado para interpretar o arquivo não faz parte do projeto.

## Limites desta fase

- usa somente `PROCESS_VM_READ` e `PROCESS_QUERY_LIMITED_INFORMATION`;
- não escreve na memória, não injeta DLL e não altera o processo do jogo;
- não avança etapas do overlay;
- não depende do PW Helper em execução;
- aceita apenas o perfil `The Classic 1.4.4 x86` documentado em `data/memory_offsets.json`;
- offsets inválidos são rejeitados pela validação de nível, HP e MP.

O arquivo de offsets é versionado separadamente das rotas. Isso evita misturar IDs de missão com endereços específicos de uma build do cliente.

## Executar o diagnóstico

Com pelo menos um cliente aberto:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\probe-memory.ps1
```

Para cada cliente, a ferramenta mostra PID, título da janela, nível, HP, MP, IDs de missões ativas e IDs registrados no histórico de missões concluídas. Nenhuma credencial ou linha de comando do jogo é registrada.

Para acompanhar alterações de todos os clientes abertos:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\probe-memory.ps1 -Watch
```

O modo contínuo usa uma leitura a cada 500 ms e registra somente mudanças: missão ativada, alteração dos bits de estado, desaparecimento depois de ter sido observada e inclusão no histórico. O intervalo pode ser alterado, por exemplo com `-Interval 250`.

O byte de estado é mantido também em formato bruto. Os bits observados correspondem às operações `IsSuccess` e `IsFinished` da estrutura original: no teste atual, `1` representa uma entrada ativa com sucesso preservado e `3` representa sucesso mais objetivo finalizado. A automação ainda não depende desse significado até uma transição completa ser reproduzida no cliente.

## Próximas etapas

Já estão identificados por RTTI o `CECHostPlayer`, o `CECTaskInterface`, a coleção de tarefas ativas e o histórico de concluídas.

1. Associar os IDs dataminados às etapas canônicas da rota, sem escolher resultados ambíguos apenas pelo nome.
2. Reproduzir no cliente as transições de aceitar, concluir objetivo e entregar.
3. Classificar os eventos validados como ativa, pronta para entrega e entregue.
4. Integrar o rastreador ao overlay com leituras estáveis por cliente.
5. Habilitar a sincronização somente por toggle, desligado por padrão.

Ausência de um ID nunca será tratada sozinha como missão entregue: o leitor precisará distinguir “nunca aceita”, “ativa” e “já finalizada” por estruturas independentes do cliente.
