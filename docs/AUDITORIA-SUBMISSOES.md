# Auditoria das submissões 0.8.3

Foram examinadas as 836 etapas das 226 missões do guia 1–60. O manifesto `data/quest_objective_bindings.json` registra 102 associações explícitas em 30 cadeias, nas quatro rotas raciais e em COMMON. IDs de progresso e ordens de rota foram preservados. Os IDs, requisitos e parentesco foram cruzados com o catálogo do `tasks.data` v124; descrições extraídas localmente ajudaram a distinguir objetivos homônimos.

## Padrões corrigidos

- Visitas aos artesãos: cada conversa aponta para seu filho, inclusive nos cinco NPCs da Cidade das Tormentas.
- Coletas filhas: Fazer Armas, Livro de Fundição, Remédio Útil, Cortar Montanhas e Espinho Venenoso apontam para o objetivo real.
- Abates filhos: Primeira Viagem, Mais Urgente, Vantagens sem esforço, Emoção da Caça, Conspiração e Limpar a Área do Arado.
- Materiais em um mesmo ID: nove etapas de Garota Solitária, Flauta de Chun, Doutrina Secreta e Parentes dos Mortos usam seletores independentes de item.
- Homônimos raciais: O Equilíbrio Celestial usa os IDs específicos de cada raça.
- Conversas e retornos: A Promessa e Garganta dos Ventos Ancestrais usam a submissão correspondente.
- Conversa e entrega repetidas: a mesma transição observada serve para etapas consecutivas do mesmo ID. Uma nova ativação limpa a evidência anterior.
- Recuperação após reinício: o histórico explícito da cadeia confirma a entrega, desde que não exista tarefa ativa dessa mesma cadeia. Confirmado no cliente com Bambu dos Selvagens, ID 938.

Para coletas, o seletor deve pertencer aos requisitos do ID informado; item desconhecido, quantidade incompleta ou ausência da cadeia ativa impedem o avanço. Sem seletor, todos os materiais continuam obrigatórios. As quantidades das etapas revisadas foram alinhadas ao catálogo.

Objetivos filhos de abate que terminam automaticamente exigem desaparecimento após observação e a presença de um ancestral ativo. A ausência inicial não basta. Etapas de entrega continuam exigindo evidência própria.

## Limitações que não são o mesmo defeito

O auditor também informa 35 etapas de objetivo com ID ausente/ambíguo ou vários contadores de abate em uma única entrada. Elas não foram forçadas a usar um material ou contador incorreto. Exemplos: Limpar Monstros e Gelo e Fogo precisam do layout dos demais contadores; etapas compostas de DG e nomes genéricos de moeda exigem associações adicionais. Nessas etapas, o avanço manual continua disponível. Esta revisão não afirma automação integral de todos os tipos de missão.

## Verificar e manter

1. `node scripts/bind-nested-objectives.mjs --write` aplica os IDs e seletores conferidos.
2. `node scripts/audit-nested-objectives.mjs --summary` verifica todas as etapas; sem `--summary`, lista os casos manuais para diagnóstico.
3. `scripts/test.ps1` verifica materiais independentes, ancestralidade, limiares, histórico, reaceite e integridade das rotas.
4. `node scripts/sync-route-workbook.mjs --write` sincroniza a planilha com o runtime de planilhas.

O auditor termina com erro se um objetivo de coleta/abate resolvido ainda apontar para um pai com filhos rastreáveis, ou se etapas de materiais compartilhados estiverem sem seletor. Sua verificação estática não substitui o teste das cadeias no cliente.
