import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const routePath = path.join(projectRoot, "data", "quest_routes.json");
const payload = JSON.parse(fs.readFileSync(routePath, "utf8"));

function short(value, limit = 72) {
  const normalized = value.replace(/\s+/g, " ").trim();
  if (normalized.length <= limit) return normalized;
  const cut = normalized.lastIndexOf(" ", limit);
  return `${normalized.slice(0, cut > 40 ? cut : limit).trim()}…`;
}

function titleFromAction(action) {
  let value = String(action || "")
    .replace(/\s*\(\s*\d+\s*,\s*\d+(?:\s*,?\s*[A-Z]\d+|\s+\d+)?\s*\)/gi, "")
    .trim();
  const rules = [
    [/^Relatório de\s+/i, "Reportar ao "],
    [/^NPC Final:\s*/i, "Entregar a "],
    [/^Fale novamente com\s+/i, "Falar novamente com "],
    [/^Fale com\s+/i, "Falar com "],
    [/^Aguarde\s+/i, "Aguardar "],
    [/^Espere\s+/i, "Aguardar "],
    [/^Pegar\s+/i, "Coletar "],
    [/^Mate\s+/i, "Matar "],
    [/^Receba\s+/i, "Receber "],
    [/^Confirme\s+/i, "Confirmar "],
    [/^Complete\s+/i, "Completar "],
    [/^Abra\s+/i, "Abrir "],
    [/^Clique\s+/i, "Clicar "],
    [/^Use\s+/i, "Usar "],
    [/^Vá\s+/i, "Ir "],
    [/^Selecione\s+/i, "Selecionar "],
  ];
  for (const [pattern, replacement] of rules) {
    if (pattern.test(value)) {
      value = value.replace(pattern, replacement);
      break;
    }
  }
  return short(value || "Próxima ação");
}

for (const mission of payload.missions) {
  for (const step of mission.steps) {
    if (!String(step.title || "").trim()) step.title = titleFromAction(step.action);
  }
}

const pearl = payload.missions.find((mission) => mission.name === "Pérola de Lágrimas" && mission.routeKey === "MC");
if (!pearl) throw new Error("Missão Pérola de Lágrimas não encontrada.");
const pearlOverrides = {
  e1: { title: "Iniciar com o Guarda Lufu" },
  e2: { title: "Confirmar o pedido do Guarda Lufu" },
  e3: { title: "Pagar o Ferreiro Xiaodao" },
  e4: { title: "Aguardar a Moeda de Ouro Falsa", action: "Dê 500 moedas ao Ferreiro Xiaodao e aguarde 20 segundos" },
  e5: { title: "Receber a Moeda de Ouro Falsa", action: "Fale novamente com Ferreiro Xiaodao e receba a Moeda de Ouro Falsa (630, 241)" },
  e6: { title: "Levar a moeda falsa ao Guarda Lufu", action: "Leve a Moeda de Ouro Falsa ao Guarda Lufu (630, 243)" },
  e7: { title: "Investigar o Bambu de Pedra Brilhante" },
  e8: { title: "Derrotar o Sapo e obter a Pérola", action: "Aguarde o Sapo aparecer, mate-o e pegue a Pérola do Homem Tubarão" },
  e9: { title: "Reportar ao Guarda Lufu", action: "Reporte ao Guarda Lufu (630, 243)" },
};
for (const step of pearl.steps) Object.assign(step, pearlOverrides[step.id] || {});

payload.schemaVersion = Math.max(7, Number(payload.schemaVersion) || 0);
fs.writeFileSync(routePath, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
console.log(`Títulos adicionados a ${payload.missions.reduce((total, mission) => total + mission.steps.length, 0)} etapas.`);
