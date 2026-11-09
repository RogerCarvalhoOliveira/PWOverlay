# Revisão das rotas 0.8.0

A revisão mantém as 226 missões, 836 etapas, IDs de progresso, recompensas e níveis previstos existentes. Foram reordenadas 215 etapas. A rota comum continua idêntica entre classes depois da Cidade do Dragão; desvios raciais seguem associados ao mesmo ponto lógico da rota comum.

## Percursos locais

O script `scripts/optimize-routes.mjs` compara deslocamentos em janelas com coordenadas explícitas no texto. Preserva a sequência interna de cada missão, a primeira ação que desbloqueia uma faixa de nível e as ações nas extremidades da janela. Ações sem coordenadas, viagens por instâncias, teleportes, pop-ups, missões cronometradas e cadeias especiais interrompem essas janelas.

| Grupo | Distância antes | Distância depois |
| --- | ---: | ---: |
| Humanos | 3005 | 3005 |
| Selvagens | 2753 | 2499 |
| Alados | 3827 | 3812 |
| Abissais | 703 | 688 |
| Comum | 10520 | 10302 |

São distâncias euclidianas entre coordenadas conhecidas, arredondadas, apenas das janelas analisadas. Não representam tempo, terreno, obstáculos, voos, distância total ou XP disponível para alcançar cada nível. O multiplicador de XP 3x e os níveis previstos existentes foram preservados; não foi simulada a progressão real dos personagens.

## Visitas às DGs e à cidade

- DG29: confirmar moeda antes da entrada; objetivos de moeda e cultivo consecutivos; entregas ao Imortal no mesmo retorno.
- DG39: confirmar moedas no aceite; concluir a DG após o circuito do cultivo 39, em vez de esperar a faixa do nível 50.
- DG59: completar os cinco chefes do cultivo e da moeda na mesma visita e entregar ambos no retorno.
- Missão de voo: abandonar o pop-up na primeira visita do nível 30 ao Oficial Graduado.
- DG49: preservados os desbloqueios distintos de preparação no nível 49 e moeda no 51, sem antecipar o requisito.

## Manutenção

Execute `node scripts/optimize-routes.mjs` para analisar ou acrescente `--write` para gravar. Revise o diff, execute `scripts/test.ps1` e sincronize a coluna Etapas da planilha com `node scripts/sync-route-workbook.mjs --write` usando o runtime documentado de planilhas. Os scripts não alteram o progresso local dos personagens.
