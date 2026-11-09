# Datamine de nomes das missões — níveis 21 a 60

Esta segunda etapa aplica às missões restantes o mesmo processo documentado para os níveis 1 a 20. A fonte de verdade é o `data/tasks.data` versão 124 instalado com o cliente The Classic PW 1.4.4. Códigos de cor foram removidos, mas grafia, capitalização, acentos, erros e truncamentos do cliente foram preservados.

## Resultado aplicado

- 86 registros de rota dos níveis 21 a 60 foram comparados.
- 39 títulos de missão foram corrigidos.
- 321 títulos de etapas foram substituídos por nomes reais de missões ou submissões.
- Todos os títulos aplicados, inclusive as partes separadas por `/`, foram validados literalmente contra as 24.277 entradas extraídas do `tasks.data`.
- Os IDs internos do overlay foram mantidos para preservar o progresso salvo.
- `Remover missão de voo do questlog` foi mantida como etapa operacional, pois não representa uma missão do `tasks.data`.

## Títulos corrigidos

| Nível | Antes | Nome no cliente |
|---:|---|---|
| 21 | Cultivo — Esperanças do General | As Esperanças do General |
| 21 | Por Amor (Continuação) | Amuleto de Aroma |
| 23 | Afeição pela Filha | Afeição Pela Filha |
| 23 | O Segredo do Ancião | Segredo do Ancião |
| 23 | Remédio para Alma | Remédio Para Alma |
| 24 | Linha de cultivo — O Líder Culpado | O Culpado |
| 25 | Ruolen – Flor do Coração Jovem | Ruolen |
| 28 | O Verdadeiro Pão Defumado | O Verdadeiro Pão Defum |
| 29 | Cultivo 29 — O Infante Primordial | Princípio Lúcido. |
| 29 | DG29 — Moeda Sangrenta de Ferro 2 Peão | Moeda Sangrenta de Fer |
| 30 | Cargo Militar Nível 1 | Cargo Militar de Honra |
| 31 | Árvore Monstro | Árvore-monstro |
| 32 | Cidade da Dor Celestial (Continuação) | Cidade da Dor Celestial |
| 32 | Missão Desafio – Memória do Herói | Memória do Herói |
| 34 | Uma Refeição Nada Suculenta | A Comida |
| 35 | Missão Desafio – Flauta de Chun | Flauta de Chun |
| 38 | Missão Desafio – Doutrina Secreta | Doutrina Secreta |
| 39 | Cultivo 39 — Aparição do Vazio de Hades | Compreensão Enigmática. |
| 39 | DG39 — Moeda Sangrenta de Ferro 3 Fantasma | Moeda Sangrenta de Fer |
| 42 | S.O.S | S. O. S |
| 43 | A Encomenda do Boticário | A Encomenda do Boticá |
| 43 | O Ataque do Cão Selvagem | O Ataque do Cão Selvag |
| 44 | Lâmina Sagrada de Meta | Lacre da Lâmina Mágica |
| 46 | Continuação da História | Continuação da históri |
| 47 | Auxiliar de Comerciante | Auxiliar de Comerciant |
| 49 | Cultivo 49 — Andando Sobre o Gelo Fino | Cultivo |
| 49 | Preparação DG49 — Lágrimas do Guardião | Lágrima do Gardião |
| 50 | Pilar Esquisito — cadeia completa | Pilar Esquisito |
| 50 | Os Melhores Amigos — Humanos/Selvagens/Alados | Os Melhores Amigos |
| 51 | DG49 — Moeda Sangrenta de Ferro 4 Wanjun | Moeda Sangrenta de Fer |
| 53 | O Paladino Sentimental | Matar Feiticeiro Gobli |
| 55 | Quest sem Tradução | Segredo da Família |
| 56 | Raposas Voadoras que Pertubam | Raposas Voadoras Que Perturba |
| 59 | Cultivo 59 — Cinco Desastres Naturais | Os Cinco Desastres Sinistros. |
| 59 | DG59 — Moeda Sangrenta de Ferro 5 Fera Sagrada | Moeda Sangrenta de Fer |
| 60 | Cargo Militar Nível 4 | Insígnia Nível 4 |

## Cadeias e submissões relevantes

- A antiga `Quest sem Tradução` foi identificada como a sequência `Segredo da Família`, `Encontrar Qin Shang`, `Cristal do Falecido`, `Amuleto Especial` e `Salvação`.
- `Pilar Esquisito` agora mostra títulos como `Procurando um Velho Amigo`, `Ritual de Passagem`, `Provação do Guerreiro`, `Saudades de Casa`, `Problema`, `Segredo dos Abissais`, `Conversão do Cristal` e `Destino`.
- Os cultivos 29, 39, 49 e 59 exibem os títulos realmente usados pelo cliente e suas submissões.
- As quatro missões de moeda das DGs mantêm IDs distintos, embora o campo do cliente mostre o mesmo título truncado `Moeda Sangrenta de Fer`.

## Critério de correlação

A correlação considerou nível mínimo, dependências, árvore pai/filho, NPCs, monstros, itens e descrição dos objetivos. Quando uma etapa do guia resume várias submissões, os nomes reais aparecem juntos e separados por `/`. Nenhum arquivo proprietário do cliente foi incluído no repositório.
