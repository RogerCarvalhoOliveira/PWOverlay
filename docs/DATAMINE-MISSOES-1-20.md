# Datamine de nomes das missões — níveis 1 a 20

Este levantamento usa como fonte de verdade o `data/tasks.data` instalado com o cliente The Classic PW 1.4.4. Os códigos de cor (`^ffffff`, `^8080ff` etc.) foram removidos, mas grafia, capitalização, acentos e truncamentos do arquivo foram preservados.

## Validação da extração

- Formato do `tasks.data`: versão 124.
- Raízes declaradas no cabeçalho: 10.746.
- Entradas extraídas, incluindo submissões: 24.277.
- O leitor terminou no byte 77.443.012, exatamente o tamanho do arquivo. Isso confirma o alinhamento integral dos registros.
- A estrutura foi conferida contra o código aberto do [sTASKedit](https://github.com/PerfectWorldReference/sTASKedit), commit `5780a4244242a1ed1b3a49e8c68226323869d95b`.
- Os IDs internos das missões foram usados somente para correlação local. Nenhum arquivo proprietário do cliente foi adicionado ao repositório.

## Resultado aplicado

- 58 títulos de missão foram alterados para a grafia desta versão.
- 511 títulos de etapas foram substituídos pelos nomes reais das missões ou submissões.
- Os IDs do overlay foram mantidos, para não apagar o progresso já salvo dos personagens.

## Títulos corrigidos

| Rota | Nível | Antes | Nome no cliente |
|---|---:|---|---|
| WR | 1 | Primeiro Teste / Exposição de Talento | Recruta / Exposição de Talento |
| WR | 3 | Duende Desertor / A Chama do Túmulo | Duende Desertor / a Chamas do Túmulo |
| WR | 5 | Aprenda sobre Habilidades de Forja | Habilidades Produção |
| WR | 5 | Espinho de Ouriço | Espinho do Ouriço |
| WR | 7 | Lágrimas do Dragão | Lágrima de Dragão |
| WR | 9 | Cultivo – Orientação de Cultivo | Guia de Cultivo Espiritual |
| WR | 9 | O Surgimento de Lingxu | Só um Pouco de Progresso |
| WR | 12 | Intercomunicação | Trocar Informações |
| WR | 18 | A Lista de Xin | A lista |
| WR | 19 | Orientação de Cultivo – Aumentar Cultivo | Guia de Cultivo Espiritual |
| WR | 19 | A Harmonia do Céu e a Terra | O Equilíbrio Celestial |
| WR | 19 | Moeda Sangrenta de Ferro | Moeda Sangrenta de Fer |
| WR | 19 | Missão Desafio – Portão de Devildom | Portão de Devildom |
| WBWF | 3 | Habilidade de Controle | Habilidade de controle |
| WBWF | 5 | Aprenda sobre Habilidades de Forja | Habilidades Produção |
| WBWF | 9 | Jornada Tranquila | Jornada Tranqüila |
| WBWF | 9 | Cultivo – Orientação de Cultivo | Cultivo |
| WBWF | 9 | O Surgimento de Lingxu | Só um Pouco de Progresso |
| WBWF | 11 | Garganta dos Ventos Ancestrais | Garganta dos Ventos An |
| WBWF | 14 | Vantagens Sem Esforço | Vantagens sem esforço |
| WBWF | 16 | Fortaleza da Cidade do Rio | Fortaleza da Cidade do |
| WBWF | 19 | Orientação de Cultivo – Aumentar Cultivo | Guia de Cultivo Espiritual |
| WBWF | 19 | A Harmoniosa Céu-Terra | O Equilíbrio Celestial |
| WBWF | 19 | Moeda Sangrenta de Ferro | Moeda Sangrenta de Fer |
| WBWF | 19 | Missão Desafio – Despachos aos Alados | Despachos aos Alados |
| EAEP | 1 | Nova Chegada / Primeira Visita | Nova Chegada |
| EAEP | 1 | Presente Para o Guarda | Presente para o Guarda |
| EAEP | 4 | Primeiro Voo | Primeiro Vôo |
| EAEP | 4 | Livro de Fundição – O Livro Perdido | Livro de Fundição |
| EAEP | 5 | Aprenda sobre Habilidades de Forja | Habilidades Produção |
| EAEP | 7 | Mãe e Coração de Ouro | Mão e Coração de Ouro |
| EAEP | 8 | O Desastre dos Espíritos das Águas | Matar Cobras Nagas |
| EAEP | 8 | Bênção de Aniversário | Benção de Aniversário |
| EAEP | 9 | Cultivo – Orientação de Cultivo | Cultivo |
| EAEP | 9 | O Surgimento de Lingxu | Só um Pouco de Progresso |
| EAEP | 15 | Veneno de Água Corrente | Veneno da Água Corrente |
| EAEP | 16 | Amostra da Cobra Venenosa | Amostra de Veneno |
| EAEP | 19 | Orientação de Cultivo – Aumentar Cultivo | Guia de Cultivo Espiritual |
| EAEP | 19 | A União Harmoniosa | O Equilíbrio Celestial |
| EAEP | 19 | Moeda Sangrenta de Ferro | Moeda Sangrenta de Fer |
| EAEP | 19 | Missão Desafio – Exibição de Talento | Exibição de Talento |
| MC | 4 | Campo da Fonte Negra | A Montanha Nebulosa |
| MC | 4 | Pular em Telhado | Pular em Telhados |
| MC | 5 | Aprenda sobre Habilidades de Forja | Habilidades Produção |
| MC | 9 | Cultivo | Guia de Cultivo Espiritual |
| MC | 9 | O Surgimento de Lingxu | Só um Pouco de Progresso |
| MC | 12 | O Tesouro I | Tesouro 1 |
| MC | 16 | Nunca Mais Voltar | Sem Retorno |
| MC | 16 | Traição | Conspiração |
| MC | 16 | Planos do Inimigo | Informação do Inimigo |
| MC | 17 | Chave – Monstro do Mar | Chave - Monstro do Mar |
| MC | 18 | Família Heirloom | Herança de Família |
| MC | 18 | Resgate Soldado | Atrição do Velho Sold. |
| MC | 19 | Cultivo Espiritual | Guia de Cultivo Espiritual |
| MC | 19 | A União Harmoniosa | O Equilíbrio Celestial |
| MC | 19 | Matar o Monstro do Mar – DG19 | Matar o Monstro do Mar |
| MC | 19 | Bandeira da Expedição | Bandeira do Embaixador |
| COMMON | 20 | Encontre o Caçador de Dragões | Encontre o Caçador de |

## Exemplos de submissões confirmadas

- `Pérola de Lágrimas`: `Informar com Guarda`, `Moeda Falsa`, `Pegar Barra de Ouro Falso`, `Perguntar ao Guarda`, `Pescar Peixe`, `O Culpado` e `Reportar ao Lu Fu`.
- `Cidade das Tormentas`: `Visitar Ancião`, `Visitas`, `Voltar Para Ancião`, `Volte para a Guia` e `Visitar Guarda`.
- `Uma Pedra Estranha`: `Informar com Lu Dadao`, `Informar com Guang Lei`, `Informar Chang Ying` e `Reportar a Lu Miao`.
- `Só um Pouco de Progresso`: as submissões específicas de Humanos, Selvagens, Alados e Abissais foram preservadas separadamente.
- `O Equilíbrio Celestial`: `Celestial em Perigo`, `Celestial` e `Escoltando o Celestial`.

## Observações sobre traduções

O objetivo aqui é reproduzir o texto que esta instalação realmente apresenta, inclusive inconsistências do próprio cliente. Por isso aparecem formas como `Habilidades Produção`, `Mão e Coração de Ouro`, `Jornada Tranqüila`, `Moeda Sangrenta de Fer`, `Atrição do Velho Sold.` e `Encontre o Caçador de`. Os três últimos nomes estão truncados no próprio campo de 30 caracteres do `tasks.data`.

A correlação não foi feita apenas pelo título. Foram comparados ID da classe/raça, nível mínimo, árvore pai/filho, NPCs, monstros, itens e descrição do objetivo.
