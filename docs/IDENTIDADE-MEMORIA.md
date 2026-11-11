# Nome e classe por memória — 0.12.0

## Layout inspecionado

Executável local `elementclient.exe`, SHA-256 `689189D9808A2D8B76F12883FA32B9AB8F48A1F9511B915477765364DE79F35E`. Endereços abaixo são VAs da imagem com base `0x400000`, para documentar a análise estática; o leitor usa offsets relativos ao objeto e não chama essas funções.

| Campo de CECHostPlayer | Offset | Evidência no executável |
| --- | --- | --- |
| ID do personagem | `0x4B8` | Inicialização em `0x4FAE69–0x4FAE6C` copia o ID do pacote recebido. |
| Nome AWString | `0x6D8` | `0x4FC538–0x4FC560` constrói a string UTF-16 dos dados de login e atribui ao campo. |
| Profissão/classe | `0x6DC` | `0x4FC4BD–0x4FC4C4` copia a profissão dos dados de login. |

AWString contém um ponteiro para UTF-16 terminado em zero. Em `nome-12` há a referência, em `nome-8` o comprimento em caracteres e em `nome-4` a capacidade. A atribuição em `0xBA0A10` e a construção em `0xBA07A0` confirmam a organização. O leitor limita comprimento a 64, valida capacidade/terminador, usa decodificação estrita e relê ID/classe/ponteiro/comprimento para detectar alteração durante a amostra.

Profissões suportadas: `0 WR`, `1 MG`, `2 ES`, `3 WF`, `4 WB`, `5 MC`, `6 EA`, `7 EP`, `8 SK`, `9 MS`. Outros valores não geram identidade.

## Fluxo e isolamento

`PlayerIdentityReader` reutiliza o handle somente de leitura já aberto para o cliente. `QuestMemoryMonitor` confirma duas amostras iguais por PID. ID/nome/classe formam a identidade da sessão, enquanto NOME|CLASSE permanece sendo a chave persistente do progresso. Mudanças invalidam evidências de missões imediatamente e geram um novo token ao confirmar a nova identidade; mudança do ponteiro ou logout também invalida a sessão.

O monitor recebe separadamente a opção de acompanhar objetivos em andamento. A identificação continua com essa opção desativada. A importação do histórico é sob demanda, em **Etapas → Importar missões concluídas**.

O resolvedor usa associação manual, depois memória e por último título identificável. Não usa título padrão/repetido. Recusa identidades que resultem no mesmo perfil em dois clientes. **Perfis** mostra o nome automático, a classe/origem na dica da coluna e permite definir um alias manual para servidores diferentes.

Nomes reais com sufixo de classe podem corresponder a perfis antigos cujo título removia esse sufixo. Só se migra a chave antiga quando o sufixo corresponde à classe lida, o perfil antigo tem essa classe e a chave completa ainda não existe. Nenhum ID ou estado de etapa é alterado. Aliases arbitrários não são adivinhados.

## Validação e pendências

- Testes executam a leitura nativa em processo x86 isolado com buffers controlados: UTF-16 acentuado, dez classes, dados inválidos e mudança durante a leitura.
- O monitor real é exercitado com missões desligadas, mudança de ID no mesmo objeto e raiz zerada simulando logout.
- Testes de estado/resolução cobrem estabilidade, descarte de evidências, token, títulos iguais, precedência, duplicatas e migração de sufixo.
- Testes WinForms exercitam preenchimento automático, retorno do modo manual e salvamento.

O teste com personagens conectados, com e sem Helper, duas instâncias e troca de personagem permanece pendente.

## The Classic 1.2.6

O perfil `the_classic_126_identity` usa o executável local com SHA-256 `0A707A7060728C4A991CC0735FCFE15A4E752ED1C60379B33CDE131AF9014B21`. A rotina de seleção lê a raiz em RVA `0xA39F5C` e percorre `0x1C` e `0x2C`. A tabela virtual de `CECHostPlayer` fica em RVA `0x93B6C4`. O executável copia o ID para `0x4B8` em `0x7DF3C0`, atribui a string UTF-16 em `0x6E0` em `0x7E0C6E–0x7E0C7B` e armazena a profissão em `0x6E4` em `0x7E0BC4`.

O perfil verifica o executável, a tabela virtual e duas amostras iguais antes de publicar a identidade. A importação sob demanda valida a interface em `CECHostPlayer + 0x14D8`, confirma a tabela virtual em RVA `0x947208` e a identidade nas duas amostras. A lista concluída usa entradas de quatro bytes a partir de `+0x4`, com ID de 16 bits e bit 0 do terceiro byte indicando falha. O mapa `task_history_126.tsv` cruza as duas edições e conserva apenas equivalências verificáveis.

Se o 1.2.6 estiver aberto como administrador, use **Perfis → Permitir leitura do PW elevado** e confirme o aviso do Windows. O leitor é reaproveitado ao reabrir o overlay na mesma sessão do Windows. Com o overlay fechado, aguarda sem consultar o jogo. Uma nova sessão do Windows ou o comando **Encerrar leitor elevado** exigem nova autorização. A leitura da lista ativa segue a opção de avanço automático. O histórico é consultado pelo botão de importação.
