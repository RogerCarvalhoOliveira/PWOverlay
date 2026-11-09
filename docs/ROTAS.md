# Edição das rotas

As rotas ficam em `data/quest_routes.json`. Normalmente o overlay somente lê e ordena as missões; a ação explícita **Editar rotas...** pode salvar alterações e sincronizar a planilha.

## Campos de uma missão

| Campo | Uso |
| --- | --- |
| `id` | Identificador estável e único usado no progresso salvo. |
| `routeKey` | Grupo da rota: `WR`, `WBWF`, `EAEP`, `MC` ou `COMMON` para o trecho compartilhado pós-Cidade do Dragão. |
| `order` | Ordem de exibição dentro da rota. |
| `classScope` | Classe à qual a missão se aplica. |
| `minLevel` | Nível mínimo para pegar a missão. |
| `expectedLevel` | Nível em que a rota prevê fazer a missão. |
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
| `routeOrder` | Ordem global da ação; permite intercalar etapas de missões diferentes sem alterar seus IDs. |

## Alterar uma missão

1. Faça uma cópia do arquivo.
2. Localize a missão pelo `id`.
3. Corrija os campos necessários.
4. Não troque o `id` se a missão for a mesma; isso preserva as marcações existentes.
5. Mantenha os IDs de etapa existentes quando apenas corrigir texto ou coordenadas.
6. Preserve ou ajuste `routeOrder`; números menores aparecem antes, mesmo quando pertencem a outra missão.
7. Atualize também `data\rota_leveling_pw_144.xlsx`, colunas **Etapas** e **Nível previsto**, para manter a documentação sincronizada. O editor visual faz isso automaticamente quando o Excel está instalado.
8. Execute `scripts\validate-data.ps1`.
9. Compile e percorra as etapas anterior, alterada e seguinte no overlay.

## Adicionar uma missão

Crie um `id` que não será reutilizado. Use o mesmo `routeKey` e `classScope` das missões vizinhas e escolha um `order` que mantenha a sequência. Se duas classes compartilharem a missão, use o escopo compartilhado já adotado no grupo.

As rotas vão até o nível 60. Depois da chegada à Cidade do Dragão, o repositório combina a rota racial com `COMMON`; apenas missões realmente exclusivas permanecem nas rotas de raça. A ordem global de `COMMON` foi otimizada como um único circuito 20–60, podendo intercalar níveis e deixar cadeias longas abertas até o próximo retorno conveniente. Os desvios raciais usam os intervalos entre ordens comuns e depois reencontram a sequência principal. A orientação automática do nível 5 para falar com o Ancião foi incluída, mas as missões específicas de Ferreiro, Alfaiate, Artesão e Boticário continuam excluídas. Missões de voo também foram removidas e substituídas por uma etapa explícita para abandonar o pop-up.

## Fontes das etapas

As missões iniciais foram cruzadas com guias clássicos separados por raça:

- Humanos: <https://pwbrasilquest.blogspot.com/2012/02/quest-humanos-level-1-19.html>
- Selvagens: <https://pwbr2010.wordpress.com/quests-2/quests-selvagens-1-ao-19/>
- Alados: <https://pweujogo.blogspot.com/p/missoes-alados-level-1-ao-19.html>
- Abissais: <https://guiapw.forumbrasil.net/t77-guia-missoes-quests-para-lvl-1-19-dos-abissais>

O trecho 20–59 usa o guia clássico de missões <https://pwmissoes1a59.blogspot.com/>. A cadeia do Pilar Esquisito foi conferida em <https://wikipw.com.br/index.php?title=Pilar_Esquisito> e no banco de dados <https://www.pwdatabase.com/br/npc/25808>. Os níveis e requisitos de Cargo Militar foram conferidos em <https://wikipw.com.br/index.php/Sistema_de_Rank_de_Fama>.

O comportamento do painel de marcadores foi conferido na interface 1.4.4 e comparado com a documentação oficial da tela básica: <https://perfectworld.com.br/o-jogo/primeiros-passos/a-tela-e-os-comandos-basicos>.

## Validar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
```

O script verifica campos obrigatórios, IDs duplicados, escopos de classe, presença das etapas, IDs e ordens globais repetidos e pares de coordenadas incompletos. Em seguida, mostra a quantidade resultante por classe e o total de etapas.
