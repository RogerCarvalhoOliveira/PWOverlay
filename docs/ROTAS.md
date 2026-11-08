# Edição das rotas

As rotas ficam em `data/quest_routes.json`. O overlay nunca altera esse arquivo; ele somente lê e ordena as missões.

## Campos de uma missão

| Campo | Uso |
| --- | --- |
| `id` | Identificador estável e único usado no progresso salvo. |
| `routeKey` | Grupo da rota: `WR`, `WBWF`, `EAEP` ou `MC`. |
| `order` | Ordem de exibição dentro da rota. |
| `classScope` | Classe à qual a missão se aplica. |
| `minLevel` | Nível mínimo para pegar a missão. |
| `maxLevel` | Nível máximo informado pela fonte, ou texto vazio quando não houver limite conhecido. |
| `name` | Nome exibido no overlay. |
| `npc` | NPC que inicia a missão. |
| `location` | Região e coordenadas. |
| `baseXp` | XP original, quando conhecida. |
| `xp3` | XP calculada com multiplicador 3. |
| `priority` | Motivo para priorizar a missão. |
| `steps` | Lista ordenada das etapas exibidas no overlay. |

Cada item de `steps` possui:

| Campo | Uso |
| --- | --- |
| `id` | Identificador estável dentro da missão, como `e1`. |
| `action` | Texto curto da ação atual. |
| `markerX` / `markerY` | Coordenada usada pelo botão **Marcar**, quando conhecida. |

## Alterar uma missão

1. Faça uma cópia do arquivo.
2. Localize a missão pelo `id`.
3. Corrija os campos necessários.
4. Não troque o `id` se a missão for a mesma; isso preserva as marcações existentes.
5. Mantenha os IDs de etapa existentes quando apenas corrigir texto ou coordenadas.
6. Atualize também `data\rota_leveling_pw_144.xlsx`, coluna **Etapas**, para manter a documentação sincronizada.
7. Execute `scripts\validate-data.ps1`.
8. Compile e percorra as etapas anterior, alterada e seguinte no overlay.

## Adicionar uma missão

Crie um `id` que não será reutilizado. Use o mesmo `routeKey` e `classScope` das missões vizinhas e escolha um `order` que mantenha a sequência. Se duas classes compartilharem a missão, use o escopo compartilhado já adotado no grupo.

As rotas terminam na chegada à Cidade do Dragão. Missões de liberação de forja ou craft foram deliberadamente excluídas desta versão.

## Fontes das etapas

As 134 missões foram cruzadas com guias clássicos separados por raça:

- Humanos: <https://pwbrasilquest.blogspot.com/2012/02/quest-humanos-level-1-19.html>
- Selvagens: <https://pwbr2010.wordpress.com/quests-2/quests-selvagens-1-ao-19/>
- Alados: <https://pweujogo.blogspot.com/p/missoes-alados-level-1-ao-19.html>
- Abissais: <https://guiapw.forumbrasil.net/t77-guia-missoes-quests-para-lvl-1-19-dos-abissais>

O comportamento do painel de marcadores foi conferido na interface 1.4.4 e comparado com a documentação oficial da tela básica: <https://perfectworld.com.br/o-jogo/primeiros-passos/a-tela-e-os-comandos-basicos>.

## Validar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
```

O script verifica campos obrigatórios, IDs duplicados, escopos de classe, presença das etapas, IDs de etapa repetidos e pares de coordenadas incompletos. Em seguida, mostra a quantidade resultante por classe e o total de etapas.
