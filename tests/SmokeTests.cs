using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using PWQuestOverlay;
using PWQuestOverlay.Memory;

internal static class SmokeTests
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3) throw new ArgumentException(
                "Informe quest_routes.json, memory_offsets.json e task_catalog_144.tsv.");
            RouteRepository routes = RouteRepository.Load(args[0]);
            var expected = new Dictionary<string, int>
            {
                { "WR", 116 }, { "WB", 116 }, { "WF", 121 },
                { "EA", 117 }, { "EP", 117 }, { "MC", 115 },
                { "MG", 116 }, { "ES", 115 }, { "SK", 106 }, { "MS", 126 }
            };
            foreach (KeyValuePair<string, int> pair in expected)
            {
                int actual = routes.GetForClass(pair.Key).Count;
                if (actual != pair.Value)
                    throw new InvalidDataException(pair.Key + ": esperado " + pair.Value + ", obtido " + actual);
            }

            List<QuestMission> wr = routes.GetForClass("WR");
            List<QuestMission> mc = routes.GetForClass("MC");
            if (!wr.Select(m => m.id).SequenceEqual(routes.GetForClass("MG").Select(m => m.id)) ||
                !mc.Select(m => m.id).SequenceEqual(routes.GetForClass("ES").Select(m => m.id)))
                throw new InvalidDataException("MG/ES não compartilham a rota correta de sua raça.");
            foreach (string guardian in new[] { "SK", "MS" })
            {
                List<QuestMission> guardianRoute = routes.GetForClass(guardian);
                string other = guardian == "SK" ? "MS" : "SK";
                if (guardianRoute.Any(m => m.classScope == other))
                    throw new InvalidDataException("Etapa da outra classe na rota " + guardian);
                if (!guardianRoute.Any(m => m.id == "SKMS|SK/MS|23757") ||
                    !guardianRoute.Any(m => m.id == "SKMS|SK/MS|23215"))
                    throw new InvalidDataException("Treinador ou chegada à Cidade do Dragão ausente em " + guardian);
            }
            foreach (string guardian in new[] { "SK", "MS" })
                if (!routes.GetForClass(guardian).Any(m => m.steps.Any(s => s.memoryTaskId == 23760)) ||
                    !routes.GetForClass(guardian).Any(m => m.steps.Any(s => s.memoryTaskId == 23761)))
                    throw new InvalidDataException("Maestria de Habilidades deve incluir os dois filhos obrigatórios.");
            foreach (uint summon in new uint[] { 23029, 23030, 23031, 23032, 23033, 23034, 23035, 23036, 23037, 23038 })
                if (!routes.GetForClass("MS").Any(m => m.id == "SKMS|MS|" + summon))
                    throw new InvalidDataException("Invocação/planta do Místico ausente: " + summon);
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

            MemoryLayoutFile memoryLayouts = MemoryLayoutFile.Load(args[1]);
            MemoryLayoutProfile memoryProfile = memoryLayouts.Find("the_classic_144");
            if (memoryProfile == null)
                throw new InvalidDataException("Perfil de memória 1.4.4 ausente.");
            memoryProfile.Validate();
            if (memoryProfile.pointerSize != 4 ||
                memoryProfile.player.className != "CECHostPlayer" ||
                memoryProfile.quests.className != "CECTaskInterface" ||
                memoryProfile.quests.ActiveEntryStride != 0x20 ||
                memoryProfile.quests.ActiveParentIndexOffset != 0x2 ||
                memoryProfile.quests.ActiveFlagsOffset != 0x7 ||
                memoryProfile.inventory.PlayerPointerOffset != 0xFDC ||
                memoryProfile.inventory.TaskInventoryOffset != 0x78 ||
                memoryProfile.inventory.ItemIdOffset != 0xC ||
                memoryProfile.inventory.ItemCountOffset != 0x14)
                throw new InvalidDataException("Layout de memória 1.4.4 inesperado.");

            TaskCatalog taskCatalog = TaskCatalog.Load(args[2]);
            TaskCatalogEntry bamboo = taskCatalog.Find(2646);
            TaskCatalogEntry bambooFlight = taskCatalog.Find(2655);
            TaskCatalogEntry beetleBlood = taskCatalog.Find(936);
            int beetleBloodCount = 0;
            if (beetleBlood != null)
                beetleBlood.RequiredTaskItems.TryGetValue(3502, out beetleBloodCount);
            if (taskCatalog.Count != 24277 || bamboo == null ||
                bamboo.Name != "Vila do Bambu" || bambooFlight == null ||
                bambooFlight.Name != "Voe para a Vila do Bam" ||
                bambooFlight.ParentId != bamboo.Id ||
                beetleBloodCount != 10 ||
                taskCatalog.FindByName("Voe para a Vila do Bam").Count != 1 ||
                taskCatalog.FindByName("Moeda Sangrenta de Fer").Count < 2)
                throw new InvalidDataException("Catálogo dataminado de missões está divergente.");

            var tracker = new QuestStateTracker();
            var reconnectedWb = new QuestMemorySnapshot();
            reconnectedWb.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1177 });
            reconnectedWb.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1179 });
            if (!taskCatalog.HasActiveOrderedSuccessor(1178, 1179, reconnectedWb))
                throw new InvalidDataException("Sucessor de Primeira Viagem não recuperou abates concluídos antes da conexão.");
            reconnectedWb.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1178 });
            if (taskCatalog.HasActiveOrderedSuccessor(1178, 1179, reconnectedWb))
                throw new InvalidDataException("Objetivo ainda ativo foi dado como concluído pelo sucessor.");
            var parallelChildren = new QuestMemorySnapshot();
            parallelChildren.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 805 });
            if (taskCatalog.HasActiveOrderedSuccessor(804, 805, parallelChildren) ||
                taskCatalog.HasActiveOrderedSuccessor(1178, 805, parallelChildren))
                throw new InvalidDataException("Filhos paralelos ou de outra cadeia provaram conclusão.");
            var deliveredBamboo = new QuestMemorySnapshot();
            if (taskCatalog.IsChainRecordedComplete(938, deliveredBamboo))
                throw new InvalidDataException("Ausência sem histórico foi tratada como entrega.");
            deliveredBamboo.FinishedTaskIds.Add(938);
            if (!taskCatalog.IsChainRecordedComplete(938, deliveredBamboo))
                throw new InvalidDataException("Histórico de Bambu dos Selvagens não foi reconhecido.");
            deliveredBamboo.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 938 });
            if (taskCatalog.IsChainRecordedComplete(938, deliveredBamboo))
                throw new InvalidDataException("Missão repetida ativa foi confundida com entrega anterior.");
            var conversations = new QuestMemoryClientState();
            conversations.Observe(deliveredBamboo);
            conversations.Record(new[] {new QuestMemoryEvent {
                TaskId = 938, Kind = QuestMemoryEventKind.DisappearedAfterObservation }});
            if (!conversations.HasObservedDisappearance(938) ||
                !conversations.HasObservedDisappearance(938) ||
                conversations.HasObservedDisappearance(939))
                throw new InvalidDataException("Conversa/entrega repetida perdeu a evidência ou usou outra missão.");
            conversations.Record(new[] {new QuestMemoryEvent {
                TaskId = 938, Kind = QuestMemoryEventKind.Activated }});
            if (conversations.HasObservedDisappearance(938))
                throw new InvalidDataException("Reaceite não limpou a evidência antiga.");
            var materials = new QuestMemorySnapshot();
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 80 });
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 82 });
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 86 });
            materials.TaskItemCounts[2377] = 10;
            materials.TaskItemCounts[2375] = 2;
            if (taskCatalog.Find(85).ParentId != 82 || taskCatalog.Find(85).TopId != 80 ||
                !taskCatalog.AreTaskItemsComplete(85, materials) ||
                taskCatalog.AreTaskItemsComplete(86, materials) ||
                taskCatalog.AreTaskItemsComplete(82, materials))
                throw new InvalidDataException("Coletas filhas não distinguem lenha 10/10 de ferro 2/10.");
            materials.TaskItemCounts[2377] = 9;
            if (taskCatalog.AreTaskItemsComplete(85, materials))
                throw new InvalidDataException("Lenha incompleta avançou.");
            materials.TaskItemCounts[2377] = 10;
            materials.TaskItemCounts[2375] = 10;
            materials.ActiveTasks.RemoveAll(entry => entry.TaskId != 80);
            if (!taskCatalog.AreTaskItemsComplete(85, materials) ||
                !taskCatalog.AreTaskItemsComplete(86, materials))
                throw new InvalidDataException("Coletas completas não sobreviveram ao desaparecimento dos filhos.");
            materials.ActiveTasks.Clear();
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 934 });
            if (taskCatalog.AreTaskItemsComplete(85, materials) ||
                taskCatalog.AreTaskItemsComplete(86, materials))
                throw new InvalidDataException("Itens sem cadeia ativa foram tratados como missão completa.");
            QuestMission foundry = routes.GetForClass("EA").Find(m => m.name == "Livro de Fundição");
            if (foundry.steps[2].title != taskCatalog.Find(85).Name ||
                foundry.steps[3].title != taskCatalog.Find(86).Name)
                throw new InvalidDataException("Rota de matérias-primas aponta para o pai em vez das submissões.");
            TaskCatalogEntry hedgehogs = taskCatalog.Find(2714);
            int boundObjectives = 0;
            foreach (QuestMission mission in routes.AllMissions)
            foreach (QuestStep step in mission.steps)
            {
                if (step.memoryTaskId == 0) continue;
                boundObjectives++;
                TaskCatalogEntry boundTask = taskCatalog.Find(step.memoryTaskId);
                if (boundTask == null || boundTask.Name != step.title)
                    throw new InvalidDataException("Associação explícita divergente: " + mission.name + "/" + step.id);
                if (step.memoryItemIds == null || step.memoryItemIds.Length == 0) continue;
                var partial = new QuestMemorySnapshot();
                partial.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = boundTask.TopId });
                foreach (uint itemId in step.memoryItemIds)
                {
                    int requirement;
                    if (!boundTask.RequiredTaskItems.TryGetValue(itemId, out requirement))
                        throw new InvalidDataException("Item vinculado a outra missão.");
                    partial.TaskItemCounts[itemId] = requirement;
                }
                if (!taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, step.memoryItemIds) ||
                    taskCatalog.AreTaskItemsComplete(boundTask.Id, partial) ||
                    taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, new uint[] { uint.MaxValue }))
                    throw new InvalidDataException("Materiais independentes não foram separados: " + mission.name + "/" + step.id);
                foreach (uint itemId in step.memoryItemIds) partial.TaskItemCounts[itemId]--;
                if (taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, step.memoryItemIds))
                    throw new InvalidDataException("Material abaixo da quantidade avançou.");
                partial.ActiveTasks.Clear();
                foreach (uint itemId in step.memoryItemIds)
                    partial.TaskItemCounts[itemId] = boundTask.RequiredTaskItems[itemId];
                if (taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, step.memoryItemIds))
                    throw new InvalidDataException("Coleta avançou sem cadeia ativa.");
            }
            if (boundObjectives < 62)
                throw new InvalidDataException("Associações das cadeias aninhadas incompletas.");
            var automaticChild = new QuestMemorySnapshot();
            automaticChild.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 736 });
            if (!taskCatalog.CanConfirmAutomaticKillCompletion(804, "Matar (5) Gato-Rei Sentinela", automaticChild) ||
                taskCatalog.CanConfirmAutomaticKillCompletion(804, "Entregue ao guarda", automaticChild) ||
                taskCatalog.CanConfirmAutomaticKillCompletion(6440, "Matar (6) Espinhos do Orgulho", automaticChild))
                throw new InvalidDataException("Transição de objetivo automático confundiu entrega ou outra cadeia.");
            automaticChild.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 804 });
            if (taskCatalog.CanConfirmAutomaticKillCompletion(804, "Matar (5) Gato-Rei Sentinela", automaticChild))
                throw new InvalidDataException("Filho ainda ativo foi tratado como desaparecido.");
            automaticChild.ActiveTasks.Clear();
            if (taskCatalog.CanConfirmAutomaticKillCompletion(804, "Matar (5) Gato-Rei Sentinela", automaticChild))
                throw new InvalidDataException("Abandono da cadeia foi tratado como objetivo concluído.");
            string killEvidence;
            var killEntry = new ActiveQuestMemoryEntry { TaskId = 2714 };
            if (hedgehogs == null || hedgehogs.RequiredMonsterId != 1037 ||
                hedgehogs.RequiredMonsterKills != 10)
                throw new InvalidDataException("Requisitos dos filhotes de ouriço divergentes.");
            foreach (int count in new[] { 0, 1, 9, 10, 11, 256 })
            {
                killEntry.FirstMonsterKillCount = count;
                bool expectedComplete = count >= 10 && count <= 255;
                if (hedgehogs.TryGetKillProgress(killEntry, "Matar (10) Filhote de Ouriço", out killEvidence) != expectedComplete)
                    throw new InvalidDataException("Limiar de abates incorreto: " + count);
            }
            killEntry.FirstMonsterKillCount = 10;
            foreach (string action in new[] {
                "Aceite a missão com Guarda", "Entregue a missão para Guarda",
                "Mate 10 filhotes e entregue ao Guarda", "Pegar 10 itens matando filhotes" })
                if (hedgehogs.TryGetKillProgress(killEntry, action, out killEvidence))
                    throw new InvalidDataException("Contador de abates avançou uma etapa incompatível.");
            if (hedgehogs.TryGetKillProgress(null, "Matar (10) Filhote de Ouriço", out killEvidence) ||
                beetleBlood.TryGetKillProgress(new ActiveQuestMemoryEntry {
                    TaskId = 936, FirstMonsterKillCount = 10 }, "Matar besouros", out killEvidence))
                throw new InvalidDataException("Contador avançou uma missão inativa ou uma coleta.");
            killEntry.TaskId = 694;
            if (hedgehogs.TryGetKillProgress(killEntry, "Matar (10) Filhote de Ouriço", out killEvidence))
                throw new InvalidDataException("Contador de outra missão foi reutilizado.");
            var baseline = new QuestMemorySnapshot();
            baseline.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 10, Flags = 0xA1 });
            baseline.FinishedTaskIds.Add(20);
            tracker.SetBaseline(baseline);
            var changed = new QuestMemorySnapshot();
            changed.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 10, Flags = 0xA3 });
            changed.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 11, Flags = 0xA1 });
            changed.FinishedTaskIds.Add(20);
            changed.FinishedTaskIds.Add(10);
            List<QuestMemoryEvent> changes = tracker.Update(changed);
            if (changes.Count != 3 ||
                changes.Count(item => item.Kind == QuestMemoryEventKind.StateChanged) != 1 ||
                changes.Count(item => item.Kind == QuestMemoryEventKind.Activated) != 1 ||
                changes.Count(item => item.Kind == QuestMemoryEventKind.AddedToHistory) != 1)
                throw new InvalidDataException("Rastreador não distinguiu as transições de missão.");
            var disappeared = new QuestMemorySnapshot();
            disappeared.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 11, Flags = 0xA1 });
            disappeared.FinishedTaskIds.Add(20);
            disappeared.FinishedTaskIds.Add(10);
            List<QuestMemoryEvent> removals = tracker.Update(disappeared);
            if (removals.Count != 1 ||
                removals[0].Kind != QuestMemoryEventKind.DisappearedAfterObservation ||
                removals[0].TaskId != 10)
                throw new InvalidDataException("Rastreador confundiu ausência com missão nunca aceita.");

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
            if (!(moeda29.steps[1].routeOrder < cultivo29.steps[2].routeOrder &&
                  cultivo29.steps[2].routeOrder < moeda29.steps[2].routeOrder &&
                  moeda29.steps[2].routeOrder < cultivo29.steps[3].routeOrder &&
                  moeda29.steps[3].routeOrder < cultivo29.steps[4].routeOrder))
                throw new InvalidDataException("DG29 separou objetivos de chefes ou retornos ao Imortal.");
            QuestMission dg39 = common.Find(m => m.id.Contains("DG39"));
            QuestMission dg59 = common.Find(m => m.id.Contains("DG59"));
            QuestMission cultivo59 = common.Find(m => m.id.Contains("Cultivo 59"));
            QuestMission flightCleanup = common.Find(m => m.id.Contains("Remover missão de voo"));
            if (dg39.steps.Last().routeOrder >= common.Where(m => m.minLevel >= 40)
                    .SelectMany(m => m.steps).Min(s => s.routeOrder) ||
                dg59.steps[1].routeOrder >= cultivo59.steps[2].routeOrder ||
                flightCleanup.steps.Last().routeOrder >= common.Where(m => m.minLevel == 31)
                    .SelectMany(m => m.steps).Min(s => s.routeOrder))
                throw new InvalidDataException("DG39, DG59 ou remoção do voo foram adiadas para fora da visita planejada.");

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
                bool guardian = classCode == "SK" || classCode == "MS";
                if (forgePopup == null || forgePopup.minLevel != (guardian ? 6 : 5) ||
                    (!guardian && forgePopup.expectedLevel != 5) || forgePopup.steps.Count != 2)
                    throw new InvalidDataException("Orientação de forja ausente ou inválida em " + classCode);
                QuestMission firstLevelFive = routes.GetForClass(classCode).Find(m => m.expectedLevel >= 5);
                if (!guardian && firstLevelFive != forgePopup)
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

            string[] dualMonitors = { "primary", "secondary" };
            if (OverlayPlacement.ResolveMonitor("secondary", "primary", dualMonitors) != "secondary" ||
                OverlayPlacement.ResolveMonitor(null, "primary", dualMonitors) != "primary" ||
                OverlayPlacement.ResolveMonitor("secondary", "primary", new[] { "primary" }) != "primary" ||
                OverlayPlacement.ResolveMonitor("secondary", "secondary", dualMonitors) != "secondary")
                throw new InvalidDataException("Seleção do monitor fixo, automático ou desconectado incorreta.");
            Size overlaySize = new Size(570, 180);
            Rectangle leftScreen = new Rectangle(-1920, 0, 1920, 1032);
            Rectangle rightScreen = new Rectangle(1920, 0, 1920, 1032);
            Rectangle upperScreen = new Rectangle(0, -1080, 1920, 1080);
            if (OverlayPlacement.Clamp(new Point(-1700, 70), overlaySize, leftScreen) != new Point(-1700, 70) ||
                OverlayPlacement.Clamp(new Point(2100, 70), overlaySize, rightScreen) != new Point(2100, 70) ||
                OverlayPlacement.Clamp(new Point(100, -1000), overlaySize, upperScreen) != new Point(100, -1000) ||
                OverlayPlacement.Clamp(new Point(-100, 1000), overlaySize, leftScreen) != new Point(-570, 852))
                throw new InvalidDataException("Overlay foi trazido para o monitor errado ou saiu da área útil.");
            var positionState = new AppState { OverlayMonitorDeviceName = "secondary", PositionLocked = true };
            positionState.Positions["secondary"] = new SavedPosition { X = -1700, Y = 70 };
            var stateSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            AppState restoredPosition = stateSerializer.Deserialize<AppState>(stateSerializer.Serialize(positionState));
            if (restoredPosition.OverlayMonitorDeviceName != "secondary" || !restoredPosition.PositionLocked ||
                restoredPosition.Positions["secondary"].X != -1700 ||
                !string.IsNullOrEmpty(new AppState().OverlayMonitorDeviceName))
                throw new InvalidDataException("Monitor escolhido não sobreviveu à gravação ou alterou o padrão antigo.");

            AppState state = new StateStore().Load();
            if (state.Characters == null || state.Positions == null || state.ClassAssociations == null)
                throw new InvalidDataException("Estado carregado sem coleções obrigatórias.");
            if (state.SchemaVersion < 10)
                throw new InvalidDataException("Estado não foi migrado para a versão atual.");
            if (state.Marker == null || state.Marker.MarkerList == null)
                throw new InvalidDataException("Calibração do marcador incompleta.");
            if (state.Ocr == null)
                throw new InvalidDataException("Configuração do OCR não foi inicializada.");
            if (state.Memory == null || string.IsNullOrWhiteSpace(state.Memory.ProfileId))
                throw new InvalidDataException("Configuração de memória não foi inicializada.");
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
