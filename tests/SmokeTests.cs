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
                { "WR", 32 }, { "WB", 32 }, { "WF", 35 },
                { "EA", 34 }, { "EP", 34 }, { "MC", 33 }
            };
            foreach (KeyValuePair<string, int> pair in expected)
            {
                int actual = routes.GetForClass(pair.Key).Count;
                if (actual != pair.Value)
                    throw new InvalidDataException(pair.Key + ": esperado " + pair.Value + ", obtido " + actual);
            }

            AppState state = new StateStore().Load();
            if (state.Characters == null || state.Positions == null || state.ClassAssociations == null)
                throw new InvalidDataException("Estado carregado sem coleções obrigatórias.");

            Console.WriteLine("Smoke tests aprovados para WR, WB, WF, EA, EP, MC e estado local.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
