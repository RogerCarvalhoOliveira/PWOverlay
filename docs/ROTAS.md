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

## Alterar uma missão

1. Faça uma cópia do arquivo.
2. Localize a missão pelo `id`.
3. Corrija os campos necessários.
4. Não troque o `id` se a missão for a mesma; isso preserva as marcações existentes.
5. Execute `scripts\validate-data.ps1`.
6. Compile e percorra as missões anterior, alterada e seguinte no overlay.

## Adicionar uma missão

Crie um `id` que não será reutilizado. Use o mesmo `routeKey` e `classScope` das missões vizinhas e escolha um `order` que mantenha a sequência. Se duas classes compartilharem a missão, use o escopo compartilhado já adotado no grupo.

As rotas terminam na chegada à Cidade do Dragão. Missões de liberação de forja ou craft foram deliberadamente excluídas desta versão.

## Validar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
```

O script verifica campos obrigatórios, IDs duplicados e escopos de classe inválidos. Em seguida, mostra a quantidade resultante por classe.
