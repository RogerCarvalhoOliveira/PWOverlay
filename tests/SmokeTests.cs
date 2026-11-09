using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PWQuestOverlay;

internal static class SmokeTests
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Informe quest_routes.json.");
            RouteRepository routes = RouteRepository.Load(args[0]);
            var expected = new Dictionary<string, int>
            {
                { "WR", 116 }, { "WB", 116 }, { "WF", 121 },
                { "EA", 117 }, { "EP", 117 }, { "MC", 115 }
            };
            foreach (KeyValuePair<string, int> pair in expected)
            {
                int actual = routes.GetForClass(pair.Key).Count;
                if (actual != pair.Value)
                    throw new InvalidDataException(pair.Key + ": esperado " + pair.Value + ", obtido " + actual);
            }

            List<QuestMission> wr = routes.GetForClass("WR");
            List<QuestMission> mc = routes.GetForClass("MC");
            if (wr[2].name != "Revitalizando a Pátria" || wr[2].steps.Count != 7)
                throw new InvalidDataException("Etapas de Revitalizando a Pátria não foram carregadas.");
            if (mc[1].name != "Cidade das Tormentas" || mc[1].steps.Count != 10)
                throw new InvalidDataException("Etapas de Cidade das Tormentas não foram carregadas.");
            foreach (QuestMission mission in routes.AllMissions)
            {
                if (mission.steps == null || mission.steps.Count == 0)
                    throw new InvalidDataException("Missão sem etapas: " + mission.name);
                if (mission.expectedLevel < mission.minLevel)
                    throw new InvalidDataException("Nível previsto abaixo do mínimo: " + mission.name);
                int previousRouteOrder = 0;
                foreach (QuestStep step in mission.steps)
                {
                    if (string.IsNullOrWhiteSpace(step.title))
                        throw new InvalidDataException("Etapa sem título de submissão: " + mission.name + " / " + step.id);
                    if (step.routeOrder <= 0)
                        throw new InvalidDataException("Etapa sem ordem global: " + mission.name + " / " + step.id);
                    if (step.routeOrder <= previousRouteOrder)
                        throw new InvalidDataException("Etapas fora de ordem dentro da missão: " + mission.name);
                    previousRouteOrder = step.routeOrder;
                }
            }

            List<QuestMission> common = routes.AllMissions.Where(m => m.routeKey == "COMMON").ToList();
            int lastLevelTwenty = common.Where(m => m.minLevel == 20)
                .SelectMany(m => m.steps).Max(step => step.routeOrder);
            int firstLaterLevel = common.Where(m => m.minLevel > 20)
                .SelectMany(m => m.steps).Min(step => step.routeOrder);
            if (firstLaterLevel >= lastLevelTwenty)
                throw new InvalidDataException("A rota comum 20-60 não intercala missões entre regiões e retornos.");

            QuestMission cultivo29 = common.Find(m => m.id == "COMMON|ALL|29|Cultivo 29 — O Infante Primordial");
            QuestMission moeda29 = common.Find(m => m.id == "COMMON|ALL|29|DG29 — Moeda Sangrenta de Ferro 2 Peão");
            if (cultivo29 == null || cultivo29.name != "Princípio Lúcido." ||
                moeda29 == null || moeda29.name != "Moeda Sangrenta de Fer")
                throw new InvalidDataException("Nomes dataminados de Cultivo 29 ou DG29 estão ausentes.");
            if (cultivo29.steps[0].routeOrder >= moeda29.steps[moeda29.steps.Count - 1].routeOrder)
                throw new InvalidDataException("Cultivo 29 e Moeda da DG29 não foram agrupados corretamente.");

            QuestMission forgeWr = wr.Find(m => m.name == "Habilidades Produção");
            QuestMission hedgehogWr = wr.Find(m => m.name == "Espinho do Ouriço");
            if (forgeWr.steps[1].routeOrder >= hedgehogWr.steps[0].routeOrder)
                throw new InvalidDataException("Rota WR ainda manda sair da cidade antes de falar com o Ancião no nível 5.");
            QuestMission weaponsWr = wr.Find(m => m.name == "Fazer Armas");
            QuestMission jumpWr = wr.Find(m => m.name == "Teste de Salto");
            if (!(weaponsWr.steps[0].routeOrder < jumpWr.steps[0].routeOrder &&
                  jumpWr.steps[jumpWr.steps.Count - 1].routeOrder < weaponsWr.steps[1].routeOrder))
                throw new InvalidDataException("Rota WR não intercala Teste de Salto e Fazer Armas.");

            QuestMission pearl = mc.Find(m => m.name == "Pérola de Lágrimas");
            QuestStep pearlReport = pearl.steps.Find(step => step.id == "e9");
            if (pearlReport == null || pearlReport.title != "Reportar ao Lu Fu")
                throw new InvalidDataException("Submissão final de Pérola de Lágrimas não está identificada.");
            if (pearl.steps.Find(step => step.id == "e8").action.IndexOf("Sapo", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidDataException("Objetivo do Sapo está ausente em Pérola de Lágrimas.");

            foreach (string classCode in expected.Keys)
            {
                QuestMission forgePopup = routes.GetForClass(classCode).Find(m =>
                    m.name == "Habilidades Produção");
                if (forgePopup == null || forgePopup.expectedLevel != 5 || forgePopup.steps.Count != 2)
                    throw new InvalidDataException("Orientação de forja ausente ou inválida em " + classCode);
                QuestMission firstLevelFive = routes.GetForClass(classCode).Find(m => m.expectedLevel >= 5);
                if (firstLevelFive != forgePopup)
                    throw new InvalidDataException("Orientação de forja não é a primeira missão prevista do nível 5 em " + classCode);

                List<QuestMission> route = routes.GetForClass(classCode);
                if (route[route.Count - 1].expectedLevel != 60 || route[route.Count - 1].name != "Insígnia Nível 4")
                    throw new InvalidDataException("Rota não termina corretamente no nível 60 em " + classCode);
                var required = new Dictionary<string, string>
                {
                    { "COMMON|ALL|29|DG29 — Moeda Sangrenta de Ferro 2 Peão", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|39|DG39 — Moeda Sangrenta de Ferro 3 Fantasma", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|51|DG49 — Moeda Sangrenta de Ferro 4 Wanjun", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|59|DG59 — Moeda Sangrenta de Ferro 5 Fera Sagrada", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|50|Pilar Esquisito — cadeia completa", "Pilar Esquisito" },
                    { "COMMON|ALL|30|Remover missão de voo do questlog", "Remover missão de voo do questlog" }
                };
                foreach (KeyValuePair<string, string> item in required)
                {
                    QuestMission requiredMission = route.Find(m => m.id == item.Key);
                    if (requiredMission == null || requiredMission.name != item.Value)
                        throw new InvalidDataException(item.Value + " ausente ou com nome divergente em " + classCode);
                }

                var globalOrders = new HashSet<int>();
                foreach (QuestMission mission in route)
                    foreach (QuestStep step in mission.steps)
                        if (!globalOrders.Add(step.routeOrder))
                            throw new InvalidDataException("Ordem global repetida em " + classCode + ": " + step.routeOrder);

                QuestMission forge = route.Find(m => m.name == "Habilidades Produção");
                int lastLevelSix = route.Where(m => m.minLevel == 6).SelectMany(m => m.steps).Max(step => step.routeOrder);
                if (forge.steps[forge.steps.Count - 1].routeOrder >= lastLevelSix)
                    throw new InvalidDataException("Orientação de forja ficou atrasada na rota " + classCode);
            }

            QuestMission conspiracy = mc.Find(m => m.name == "Conspiração");
            QuestMission enemyInformation = mc.Find(m => m.name == "Informação do Inimigo");
            QuestMission celestialBalance = mc.Find(m => m.name == "O Equilíbrio Celestial");
            if (conspiracy == null || conspiracy.steps.Find(step => step.title == "Reportar para Tormenta") == null)
                throw new InvalidDataException("Nome dataminado de Conspiração ou de suas submissões está ausente.");
            if (enemyInformation == null || enemyInformation.steps.Find(step => step.title == "Informe o Sacerdote") == null)
                throw new InvalidDataException("Nome dataminado de Informação do Inimigo está ausente.");
            if (celestialBalance == null || celestialBalance.steps.Find(step => step.title == "Celestial em Perigo") == null)
                throw new InvalidDataException("Submissões dataminadas de O Equilíbrio Celestial estão ausentes.");

            List<QuestMission> ea = routes.GetForClass("EA");
            if (ea.Find(m => m.name == "Mão e Coração de Ouro") == null ||
                ea.Find(m => m.name == "Matar Cobras Nagas") == null)
                throw new InvalidDataException("Correções dataminadas da rota EA/EP estão ausentes.");
            QuestMission unknownChain = common.Find(m => m.id == "COMMON|ALL|55|Quest sem Tradução");
            if (unknownChain == null || unknownChain.name != "Segredo da Família" ||
                unknownChain.steps.Find(step => step.id == "e10").title != "Salvação")
                throw new InvalidDataException("Cadeia Segredo da Família não recebeu os nomes dataminados.");
            QuestMission pillar = common.Find(m => m.id == "COMMON|ALL|50|Pilar Esquisito — cadeia completa");
            if (pillar == null || pillar.steps.Find(step => step.id == "e9").title != "Problema" ||
                pillar.steps.Find(step => step.id == "e11").title.IndexOf("Conversão do Cristal", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Submissões dataminadas de Pilar Esquisito estão ausentes.");
            foreach (QuestMission mission in routes.AllMissions)
            {
                if (mission.name.IndexOf('^') >= 0)
                    throw new InvalidDataException("Código de cor do cliente permaneceu no nome: " + mission.name);
            }

            AppState state = new StateStore().Load();
            if (state.Characters == null || state.Positions == null || state.ClassAssociations == null)
                throw new InvalidDataException("Estado carregado sem coleções obrigatórias.");
            if (state.SchemaVersion < 9)
                throw new InvalidDataException("Estado não foi migrado para a versão atual.");
            if (state.Marker == null || state.Marker.MarkerList == null)
                throw new InvalidDataException("Calibração do marcador incompleta.");
            if (state.Ocr == null)
                throw new InvalidDataException("Configuração do OCR não foi inicializada.");
            if (!state.Marker.Calibrated && state.Marker.MarkerList.Y != 70)
                throw new InvalidDataException("Ponto padrão da lista de marcadores não foi migrado para uma linha selecionável.");
            foreach (CharacterProgress progress in state.Characters.Values)
                if (progress.HasOverlayMarker && progress.OverlayMarkerCount <= 0)
                    throw new InvalidDataException("Contagem de marcadores não foi migrada para " + progress.CharacterName);

            Console.WriteLine("Smoke tests aprovados para rotas, etapas e estado local.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
