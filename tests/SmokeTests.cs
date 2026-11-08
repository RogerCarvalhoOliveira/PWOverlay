using System;
using System.Collections.Generic;
using System.IO;
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
                { "WR", 33 }, { "WB", 33 }, { "WF", 36 },
                { "EA", 35 }, { "EP", 35 }, { "MC", 34 }
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
            }

            foreach (string classCode in expected.Keys)
            {
                QuestMission forgePopup = routes.GetForClass(classCode).Find(m =>
                    m.name == "Aprenda sobre Habilidades de Forja");
                if (forgePopup == null || forgePopup.expectedLevel != 5 || forgePopup.steps.Count != 2)
                    throw new InvalidDataException("Orientação de forja ausente ou inválida em " + classCode);
                QuestMission firstLevelFive = routes.GetForClass(classCode).Find(m => m.expectedLevel >= 5);
                if (firstLevelFive != forgePopup)
                    throw new InvalidDataException("Orientação de forja não é a primeira missão prevista do nível 5 em " + classCode);
            }

            AppState state = new StateStore().Load();
            if (state.Characters == null || state.Positions == null || state.ClassAssociations == null)
                throw new InvalidDataException("Estado carregado sem coleções obrigatórias.");
            if (state.Marker == null || state.Marker.MarkerList == null)
                throw new InvalidDataException("Calibração do marcador incompleta.");

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
