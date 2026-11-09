# Leitura de memória experimental

Esta branch pesquisa a sincronização do estado de missões diretamente no cliente x86 do Perfect World 1.4.4. O código atual é somente um diagnóstico: ele abre o `elementclient.exe` com permissão de consulta e leitura, resolve o personagem por ponteiro configurado ou RTTI (`CECHostPlayer`) e valida nível, HP e MP.

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

## Próximas etapas

Já estão identificados por RTTI o `CECHostPlayer`, o `CECTaskInterface`, a coleção de tarefas ativas e o histórico de concluídas.

1. Correlacionar IDs com `data/tasks.data` e com os IDs canônicos da rota.
2. Interpretar os campos de progresso dentro de cada `ActiveTaskEntry`.
3. Mapear estados distintos: ativa, objetivo concluído, pronta para entrega e entregue.
4. Exigir leituras estáveis e implementar primeiro um painel de diagnóstico.
5. Somente depois habilitar sincronização do overlay, sempre com toggle desligado por padrão.

Ausência de um ID nunca será tratada sozinha como missão entregue: o leitor precisará distinguir “nunca aceita”, “ativa” e “já finalizada” por estruturas independentes do cliente.
