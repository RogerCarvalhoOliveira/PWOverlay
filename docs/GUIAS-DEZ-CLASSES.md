# Guias para as dez classes

| Raça | Classes | Rota interna | Aba da planilha |
| --- | --- | --- | --- |
| Humanos | WR / Guerreiro; MG / Mago | WR | Humanos - WR |
| Selvagens | WB / Bárbaro; WF / Feiticeira | WBWF | Selvagens - WB-WF |
| Alados | EA / Arqueiro; EP / Sacerdote | EAEP | Alados - EA-EP |
| Abissais | MC / Mercenário; ES / Espiritualista | MC | Abissais - MC |
| Guardiões | SK / Arcano; MS / Místico | SKMS | Guardiões - SK-MS |

Os nomes antigos das quatro abas foram mantidos para o editor continuar encontrando-as. Os títulos e a coluna Classe identificam as novas classes. No overlay, selecione a sigla em Perfis ou use `Personagem[MG]`, `[ES]`, `[SK]` ou `[MS]` no PW Helper. As missões comuns são acrescentadas à rota automaticamente, até o nível 105. O [guia 61–105](GUIA-61-105.md) descreve os ramos God/Evil, a coluna Cultivo e os pré-requisitos finais.

## Humanos e Abissais

MG usa as mesmas missões raciais de WR; ES usa as mesmas de MC. O progresso continua separado por personagem e classe. Os IDs e as 836 etapas anteriores foram preservados. Textos de recompensas iniciais passaram a indicar a arma adequada à classe, e a orientação do amuleto humano permite escolher HP ou MP conforme a necessidade.

## Guardiões: percurso e XP ×3

As duas classes têm começos distintos na Torre do Despertar. O guia mantém os NPCs e objetivos próprios de cada uma, inclusive o conserto do emblema do Arcano e o cristal do Místico. A primeira invocação do Místico entra antes da saída dessa região, na passagem pela Sacerdotisa da Alma. Maestria de Habilidades inclui as visitas aos dois treinadores: o cliente define dois filhos paralelos obrigatórios, sem indicador de escolha ou conclusão antecipada do pai.

Depois da primeira ida a Tellus, as recompensas de quests ×3 já colocam o personagem muito acima dos níveis mínimos. Por isso, o roteiro evita a sequência longa de missões 10–19 e alguns aceites que já estariam bloqueados pelo nível máximo. Ele usa Memórias e dois objetivos da região do Lago da Árvore Oca para completar essa faixa, mantendo os cultivos próprios da raça.

Dois aceites foram antecipados em relação às entregas:

- **A Reunião:** aceitar com o Ancião Star antes de receber a recompensa de Visitando o Ancião. Limite de aceite 11.
- **Memórias:** aceitar com o Oficial Qine antes de entregar O Guardião do Templo no mesmo NPC. Limite de aceite 16.

Pisando em Ovos e Ameaças Veladas são aceitas antes de receber as recompensas das duas. O aviso Habilidades Produção tem mínimo 6 para os Guardiões e é entregue na passagem pelo Ancião, antes do retorno à Fenda. Missões para aprender os ofícios específicos continuam excluídas.

O nível previsto dos Guardiões foi calculado usando a curva do jogador ID 202 do elements.data e as recompensas de quest ×3. XP de abates, eventos e ajustes do servidor não entram nessa previsão. A chegada à Cidade do Dragão pode ocorrer acima de 20 após as recompensas de cultivo. O trecho comum preserva seus níveis alvo anteriores.

## Habilidades importantes do Místico

| Nível mínimo | Aviso / cadeia | Recompensa importante |
| --- | --- | --- |
| 7 | Guia - Aliança·Demônio / Treinamento Demoníaco | Primeiro contrato de invocação |
| 11 | Aprenda a Ferroada / Ferroada da Punição | Livro de planta |
| 15 | Trepadeira da Névoa | Livro de planta |
| 21 | Aprenda a Vinha / Vinha de Aranha | Livro de planta |
| 23 | Guia da Aliança·Dardo / Raio e Trovão | Segundo contrato |
| 29 | Aprenda a Flor / Flor Indiferente | Livro de planta |
| 33 | Guia da Aliança·Vidro / Esperança Renascida | Terceiro contrato |
| 39 | Aprenda a Erva de Cura / Erva de Cura | Livro de planta |
| 49 | Aprenda a Erva Vital / Erva Vital | Livro de planta |
| 59 | Guia da Aliança·Chamas / O Desastre se Aproxima | Quarto contrato |

Os nomes são os do cliente local. As cadeias posteriores à capital entram nos intervalos entre ações comuns da faixa correspondente. O Místico faz o desvio exclusivo e retorna ao grupo; as tarefas não são exibidas para Arcano ou outra classe.

## Dados e manutenção

`data/guardian_quest_facts.json` contém os fatos selecionados do tasks.data v124, os nomes do elements.data v145 e os pontos de ocorrência de configs/coord_data.txt. A instalação local se apresenta como The Classic PW 1.4.4 e contém essas tarefas dos Guardiões. Os binários do jogo e os extratores externos não são distribuídos.

As coordenadas usam o mapa `world`: `X=round(worldX/10+400)` e `Y=round(worldZ/10+550)`. NPCs usam seus pontos; monstros e recursos usam uma ocorrência próxima ao trecho. Uma ocorrência não representa toda a área de aparecimento. Quando não há ponto confirmado, o marcador fica desabilitado nessa etapa.

Para editar os fatos e reconstruir:

```powershell
node scripts/build-guardian-routes.mjs --write
node scripts/build-endgame-routes.mjs --write
node scripts/verify-guardian-routes.mjs
node scripts/verify-endgame-routes.mjs
node scripts/audit-nested-objectives.mjs --summary
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
node scripts/sync-route-workbook.mjs --extend-105 --write
powershell -ExecutionPolicy Bypass -File scripts/test-overlay-interactions.ps1
```

O modo opcional `--import-local` do gerador espera os JSONs intermediários documentados no próprio script, em `work/`. Ele só é necessário para renovar a extração; o build e a reconstrução normal usam os fatos versionados. Depois de editar diretamente uma rota gerada, não rode o gerador antes de levar a alteração aos fatos ou ao script.

As 184 etapas novas possuem IDs explícitos de tarefa. Objetivos com múltiplos contadores, gatilhos de mapa, esperas e interações especiais podem exigir Etapa feita; a sincronização por memória só avança com evidência suportada. A auditoria estática e os testes passaram; o percurso completo ainda precisa ser validado em personagens SK/MS no servidor.
