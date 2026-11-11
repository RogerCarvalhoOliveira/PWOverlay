import assert from 'node:assert/strict';
import fs from 'node:fs';

const factsPath = process.argv.slice(2).find(argument => argument !== '--write');
assert(factsPath, 'Informe o JSON gerado por EndgameTaskDump.');
const file = new URL('../data/quest_routes.json', import.meta.url);
const original = fs.readFileSync(file, 'utf8');
const route = JSON.parse(original);
const tasks = new Map();
const launches = new Map();
function indexTask(task, root = task.id) {
  tasks.set(task.id, { ...task, root });
  if (task.newTask && task.newTask !== task.id && !task.children.some(child => child.id === task.newTask)) {
    const previous = launches.get(task.newTask) ?? new Set();
    previous.add(root);
    launches.set(task.newTask, previous);
  }
  for (const child of task.children) indexTask(child, root);
}
for (const task of JSON.parse(fs.readFileSync(factsPath, 'utf8'))) indexTask(task);

const classes = ['WR', 'MG', 'ES', 'WF', 'WB', 'MC', 'EA', 'EP', 'SK', 'MS'];
const scenarios = classes.flatMap((code, occupation) => ['GOD', 'EVIL'].flatMap(path =>
  [1, 2].map(gender => ({ code, occupation, path, gender }))));
const stages = route.missions.flatMap(mission => mission.steps.map((step, index) => ({ mission, step, index })));
const missionStages = new Map();
for (const stage of stages) {
  const list = missionStages.get(stage.mission) ?? [];
  list.push(stage);
  missionStages.set(stage.mission, list);
}
const included = ({ mission }, { code, path }) =>
  !mission.aliasOf && (mission.classScope === 'ALL' || mission.classScope.split('/').includes(code)) &&
  (!mission.cultivationPath || mission.cultivationPath === path);
const usable = (task, { occupation, gender }) => task &&
  (!task.occupations.length || task.occupations.includes(occupation)) && (!task.gender || task.gender === gender);
const nodeTasks = node => node.step.memoryTaskId
  ? [tasks.get(node.step.memoryTaskId)].filter(Boolean)
  : (node.mission.historyTaskGroups?.length === 1
    ? node.mission.historyTaskGroups[0].map(id => tasks.get(id)).filter(Boolean)
    : []);
const offerAction = /^(Aceite|Pegue a missão|Fale com|Falar com)\b/i;
const npcAction = /^(Aceite|Pegue a missão|Fale com|Falar com|Entregue|Retorne|Reporte|Compre)\b/i;
const special = /cultivo|\bDG\s*\d|moeda sangrenta|passado|pilar esquisito|dusk|primeiro v[oô]o|teste de salto|pular em telhados/i;
const travel = /teleport|mude de mapa|entre na|entre no|saia da|saia do|instância|caverna|dentro da|dentro do|\bDG\s*\d/i;
const point = ({ step }) => Number.isInteger(step.markerX) && Number.isInteger(step.markerY)
  ? [step.markerX, step.markerY] : null;
const equalPoint = (a, b) => a && b && a[0] === b[0] && a[1] === b[1];
const distance = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
const npcName = ({ step }) => step.action.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase()
  .split(/\(| — /)[0].replace(/^.*?\b(com|para|ao)\s+(?:(?:o|a)\s+)?|^retorne\s+a\s+/, '')
  .replace(/[^a-z0-9 ]/g, '').trim();
const sameNpc = (offer, visit) => {
  const from = point(offer), to = point(visit);
  if (!from || !to || !npcAction.test(visit.step.action) || travel.test(visit.step.action)) return false;
  const offerNpcs = new Set(nodeTasks(offer).map(task => task.npc).filter(Boolean));
  const visitNpcs = nodeTasks(visit).map(task => visit.index === 0 && offerAction.test(visit.step.action)
    ? task.npc : task.awardNpc).filter(Boolean);
  if (offerNpcs.size === 1 && visitNpcs.length)
    return visitNpcs.every(id => offerNpcs.has(id)) && distance(from, to) <= 2;
  const a = npcName(offer), b = npcName(visit);
  return equalPoint(from, to) && a.length > 2 && b.length > 2 &&
    (a === b || a.startsWith(b + ' ') || b.startsWith(a + ' '));
};

const levelForXp = xp => {
  let level = 1;
  for (const required of route.xpToNextLevel) {
    if (xp < required) break;
    xp -= required;
    level++;
  }
  return level;
};

function scenariosByStage() {
  const result = new Map(stages.map(stage => [stage, []]));
  for (const scenario of scenarios) {
    const sequence = stages.filter(stage => included(stage, scenario)).sort((a, b) => a.step.routeOrder - b.step.routeOrder);
    const position = new Map(sequence.map((stage, index) => [stage, index]));
    const completion = new Map();
    const levelAfter = [];
    const activeAfter = [];
    const rewards = new Map();
    let xp = 0, minLevel = 1, active = 0;
    for (const [index, stage] of sequence.entries()) {
      const { mission } = stage;
      if (stage.index === 0 && mission.steps.length > 1) active++;
      if (stage.index > 0 && stage.index === mission.steps.length - 1) active--;
      if (!mission.xpOptional && stage.index === 0) minLevel = Math.max(minLevel, mission.minLevel);
      if (!mission.xpOptional && stage.index === mission.steps.length - 1) {
        const groups = mission.historyTaskGroups ?? [];
        for (const group of groups) {
          const variants = group.map(id => tasks.get(id)).filter(task => usable(task, scenario));
          if (variants.length === 1 && !completion.has(variants[0].root)) completion.set(variants[0].root, index);
        }
        const key = groups.length ? groups.map(group => [...group].sort().join(',')).sort().join(';') : mission.id;
        const reward = Math.max(0, mission.xp3 ?? ((mission.baseXp ?? 0) * 3));
        const previous = rewards.get(key);
        if (previous === undefined || reward < previous) {
          rewards.set(key, reward);
          xp += reward - (previous ?? 0);
        }
      }
      levelAfter[index] = Math.max(minLevel, levelForXp(xp));
      activeAfter[index] = active;
    }
    const context = { scenario, sequence, position, completion, levelAfter, activeAfter };
    for (const stage of sequence) result.get(stage).push(context);
  }
  return result;
}

const accepted = stages.filter(stage => stage.index === 0 && offerAction.test(stage.step.action) && point(stage) &&
  !stage.mission.repeatable && !stage.mission.cultivationChoice && !special.test(stage.mission.name) &&
  !travel.test(stage.step.action) && !/\bou\b.+\(/i.test(stage.step.action) &&
  nodeTasks(stage).some(task => task.npc > 0 && !task.automatic && !task.timeLimit));

function available(stage, context, visitPosition, needsNpcOffer = true) {
  const level = context.levelAfter[visitPosition];
  if (level < stage.mission.minLevel) return false;
  const candidates = nodeTasks(stage).filter(task => usable(task, context.scenario));
  if (!candidates.length) return false;
  return candidates.every(task => {
    if ((needsNpcOffer && (!task.npc || task.automatic)) || task.timeLimit || task.period || task.minimumReputation ||
      task.reincarnation || task.spouse || level < task.minLevel || (task.maxLevel && level > task.maxLevel)) return false;
    const complete = id => (context.completion.get(tasks.get(id)?.root ?? id) ?? Infinity) <= visitPosition;
    const required = task.prerequisites;
    const count = required.filter(complete).length;
    if (count < (task.prerequisiteLeast || required.length)) return false;
    const previous = [...(launches.get(task.id) ?? [])].filter(id => id !== task.root && usable(tasks.get(id), context.scenario));
    return !previous.length || previous.some(complete);
  });
}

const moves = [];
const moved = new Set();
// Some starting quests are granted by the starter quest's turn-in. Keep the pickup step
// immediately after the hand-in so it can also confirm an automatically granted quest.
let byStage = scenariosByStage();
for (const stage of stages.filter(stage => stage.index === 0 && stage.mission.minLevel === 1 && offerAction.test(stage.step.action))) {
  const contexts = byStage.get(stage);
  if (!contexts.length) continue;
  const first = contexts[0];
  const roots = new Set(nodeTasks(stage).flatMap(task => [...(launches.get(task.id) ?? [])]));
  if (!roots.size || !nodeTasks(stage).every(task => [...(launches.get(task.id) ?? [])].some(id => roots.has(id)))) continue;
  const start = first.position.get(stage);
  const anchor = first.sequence.slice(start + 1, start + 7).find(candidate => candidate.index === candidate.mission.steps.length - 1 &&
    candidate.mission.historyTaskGroups?.flat().some(id => roots.has(id)) && sameNpc(stage, candidate));
  if (!anchor || !contexts.every(context => {
    const from = context.position.get(stage), to = context.position.get(anchor);
    return to > from && to < from + 7 && context.position.get(stages.find(node => node.mission === stage.mission && node.index === 1)) > to &&
      available(stage, context, to, false);
  })) continue;
  const target = anchor.step.routeOrder + 1;
  if (stages.some(other => other.step.routeOrder === target && contexts.some(({ scenario }) => included(other, scenario)))) continue;
  moves.push({ route: stage.mission.routeKey, quest: stage.mission.name, after: anchor.step.action, reason: 'pré-requisito inicial' });
  stage.step.routeOrder = target;
  moved.add(stage);
  byStage = scenariosByStage();
}

const offers = new Set(accepted);
const sameScope = (a, b) => a.mission.classScope === b.mission.classScope &&
  a.mission.cultivationPath === b.mission.cultivationPath;
const nearbyPoint = (sequence, index, direction) => {
  for (let offset = 0; offset < 3; offset++) {
    const stage = sequence[index + offset * direction];
    if (!stage || travel.test(stage.step.action)) return null;
    if (point(stage)) return point(stage);
  }
  return null;
};

for (const stage of accepted) {
  if (moved.has(stage)) continue;
  const contexts = byStage.get(stage);
  if (!contexts.length) continue;
  const first = contexts[0], current = first.position.get(stage);
  if (current < 2) continue;
  const group = [stage];
  while (true) {
    const next = first.sequence[current + group.length];
    if (!offers.has(next) || moved.has(next) || !sameScope(stage, next) ||
      !equalPoint(point(stage), point(next)) || !sameNpc(stage, next)) break;
    group.push(next);
  }
  for (const anchor of first.sequence.slice(0, current - 1).reverse()) {
    if (anchor.mission === stage.mission || anchor.mission.xpOptional || !sameNpc(stage, anchor)) continue;
    const target = anchor.step.routeOrder + 1;
    if (stage.step.routeOrder <= target || stages.some(other => other.step.routeOrder >= target &&
      other.step.routeOrder < target + group.length && contexts.some(({ scenario }) => included(other, scenario)))) continue;
    let savings = 0;
    const safe = contexts.every(context => {
      const from = context.position.get(stage), to = context.position.get(anchor);
      if (!(to >= 0 && from - to >= 2) || !group.every((offer, index) =>
        context.sequence[from + index] === offer && available(offer, context, to))) return false;
      if (context.sequence.slice(to + 1, from).some(other => travel.test(other.step.action))) return false;
      if (context.activeAfter.slice(to, from).some(active => active + group.length > 24)) return false;
      const stop = point(stage), before = nearbyPoint(context.sequence, from - 1, -1),
        after = nearbyPoint(context.sequence, from + group.length, 1);
      if (!before || !after || equalPoint(stop, before) || equalPoint(stop, after)) return false;
      const detour = distance(before, stop) + distance(stop, after) - distance(before, after);
      const extra = 2 * distance(point(anchor), stop);
      if (detour <= extra + 0.5) return false;
      savings += detour - extra;
      return true;
    });
    if (!safe) continue;
    moves.push({ route: stage.mission.routeKey, quests: group.map(offer => offer.mission.name), after: anchor.step.action,
      distanceSaved: Math.round(savings / contexts.length) });
    group.forEach((offer, index) => { offer.step.routeOrder = target + index; moved.add(offer); });
    byStage = scenariosByStage();
    break;
  }
}

const objectiveAction = /^(Mate|Matar|Colete|Pegar|Pegue)\b/i;
const onFoot = stage => point(stage) && !travel.test(stage.step.action) &&
  !/aguarde|minutos|segundos|pop-up|questlog|altitude|voando/i.test(stage.step.action);
const guideRoots = mission => (mission.historyTaskGroups ?? []).flat().map(id => tasks.get(id)).filter(Boolean);
const ordinary = ({ mission }) => !mission.repeatable && !mission.cultivationChoice &&
  !special.test(mission.name) && !mission.cultivationPath;
const safeFollowup = stage => {
  if (stage.index === 0 || !ordinary(stage)) return false;
  if (!npcAction.test(stage.step.action) && !objectiveAction.test(stage.step.action)) return false;
  const roots = guideRoots(stage.mission);
  const stageFacts = nodeTasks(stage);
  return roots.length > 0 && roots.every(root => !root.timeLimit && !root.automatic && !root.newPeriod) &&
    stageFacts.every(task => !task.timeLimit && !task.automatic && !task.newPeriod) &&
    (stage.index < stage.mission.steps.length - 1 || roots.every(root => !root.newTask));
};
const boundaryPoint = (sequence, index, direction, reduced) => {
  for (let cursor = index; cursor >= 0 && cursor < sequence.length; cursor += direction) {
    const stage = sequence[cursor];
    if (reduced && stage.mission.xpOptional) continue;
    return onFoot(stage) ? point(stage) : null;
  }
  return null;
};
const pathDelta = (sequence, from, to, reduced, count = 1) => {
  const start = point(sequence[from]), end = point(sequence[from + count - 1]), at = point(sequence[to]);
  const previous = boundaryPoint(sequence, from - 1, -1, reduced);
  const next = boundaryPoint(sequence, from + count, 1, reduced);
  const insertion = boundaryPoint(sequence, to + 1, 1, reduced);
  if (!previous || !next || !insertion) return null;
  const removed = distance(previous, start) + distance(end, next) - distance(previous, next);
  const added = distance(at, start) + distance(end, insertion) - distance(at, insertion);
  return removed - added;
};
const safeToFinishEarlier = crossed => !crossed.some(other => other.index === 0 &&
  (!nodeTasks(other).length || nodeTasks(other).some(task => task.maxLevel && task.maxLevel < 105)));

const monsterIds = task => [...task.monsters.map(monster => monster.id), ...task.children.flatMap(monsterIds)];
const monstersByMission = new Map(route.missions.map(mission =>
  [mission, new Set(guideRoots(mission).flatMap(monsterIds))]));

// Keep a short quest in the same trip when its entire sequence fits around the existing visit.
for (const start of accepted) {
  const group = missionStages.get(start.mission);
  if (!ordinary(start) || start.mission.xpOptional || group.length < 2 ||
    group.some(stage => moved.has(stage) || !onFoot(stage) || (stage.index > 0 && !safeFollowup(stage)))) continue;
  const contexts = byStage.get(start);
  if (!contexts.length) continue;
  const first = contexts[0], current = first.position.get(start);
  if (current < 3 || !group.every((stage, index) => first.sequence[current + index] === stage)) continue;
  let best = null;
  const monsters = monstersByMission.get(start.mission);
  for (const anchor of first.sequence.slice(0, current - 2).reverse()) {
    if (!onFoot(anchor) || anchor.mission.xpOptional || distance(point(anchor), point(start)) > 30) continue;
    const target = anchor.step.routeOrder + 1;
    if (stages.some(other => other.step.routeOrder >= target && other.step.routeOrder < target + group.length &&
      contexts.some(({ scenario }) => included(other, scenario)))) continue;
    let minimum = Infinity;
    const safe = contexts.every(context => {
      const from = context.position.get(start), to = context.position.get(anchor);
      if (!(to >= 0 && from - to >= 3) || !available(start, context, to) ||
        !group.every((stage, index) => context.sequence[from + index] === stage)) return false;
      const crossed = context.sequence.slice(to + 1, from);
      if (crossed.some(other => travel.test(other.step.action) || !ordinary(other) ||
        [...monstersByMission.get(other.mission)].some(id => monsters.has(id))) || !safeToFinishEarlier(crossed)) return false;
      if (context.activeAfter[to] >= 24) return false;
      const full = pathDelta(context.sequence, from, to, false, group.length);
      const reduced = pathDelta(context.sequence, from, to, true, group.length);
      if (full === null || reduced === null) return false;
      minimum = Math.min(minimum, full, reduced);
      return minimum >= 4;
    });
    if (safe && (!best || minimum > best.minimum)) best = { anchor, target, minimum };
  }
  if (!best) continue;
  moves.push({ route: start.mission.routeKey, quest: start.mission.name, stages: group.length,
    after: best.anchor.step.action, distanceSaved: Math.round(best.minimum) });
  group.forEach((stage, index) => { stage.step.routeOrder = best.target + index; moved.add(stage); });
  byStage = scenariosByStage();
}

// Reuse a visit to the area for an already unlocked objective, delivery or NPC pickup.
for (const stage of stages.slice().sort((a, b) => a.step.routeOrder - b.step.routeOrder)) {
  if (moved.has(stage) || stage.mission.xpOptional || !onFoot(stage) ||
    !(offers.has(stage) ? ordinary(stage) : safeFollowup(stage))) continue;
  const contexts = byStage.get(stage);
  if (!contexts.length) continue;
  const first = contexts[0], current = first.position.get(stage);
  const predecessor = missionStages.get(stage.mission)[stage.index - 1];
  const finishes = stage.index === stage.mission.steps.length - 1;
  let best = null;
  if (current < 3) continue;
  for (const anchor of first.sequence.slice(0, current - 2).reverse()) {
    if (!onFoot(anchor) || anchor.mission === stage.mission || anchor.mission.xpOptional ||
      distance(point(stage), point(anchor)) > 30) continue;
    const target = anchor.step.routeOrder + 1;
    if (stages.some(other => other.step.routeOrder === target &&
      contexts.some(({ scenario }) => included(other, scenario)))) continue;
    let minimum = Infinity;
    const safe = contexts.every(context => {
      const from = context.position.get(stage), to = context.position.get(anchor);
      if (!(to >= 0 && from - to >= 3)) return false;
      if (predecessor ? !(context.position.get(predecessor) <= to) : !available(stage, context, to)) return false;
      const crossed = context.sequence.slice(to + 1, from);
      if (crossed.some(other => travel.test(other.step.action) || !ordinary(other))) return false;
      if (finishes && !safeToFinishEarlier(crossed)) return false;
      if (!predecessor && context.activeAfter.slice(to, from).some(active => active >= 24)) return false;
      const full = pathDelta(context.sequence, from, to, false);
      const reduced = pathDelta(context.sequence, from, to, true);
      if (full === null || reduced === null) return false;
      minimum = Math.min(minimum, full, reduced);
      return minimum >= 2;
    });
    if (safe && (!best || minimum > best.minimum)) best = { anchor, target, minimum };
  }
  if (!best) continue;
  moves.push({ route: stage.mission.routeKey, quest: stage.mission.name, stage: stage.step.title,
    action: stage.step.action, after: best.anchor.step.action, distanceSaved: Math.round(best.minimum) });
  stage.step.routeOrder = best.target;
  moved.add(stage);
  byStage = scenariosByStage();
}

for (const scenario of scenarios) {
  const sequence = stages.filter(stage => included(stage, scenario));
  assert.equal(new Set(sequence.map(node => node.step.routeOrder)).size, sequence.length, scenario.code);
}
for (const mission of route.missions) {
  assert(mission.steps.every((step, index) => !index || step.routeOrder > mission.steps[index - 1].routeOrder), mission.id);
}
console.log(JSON.stringify({ classes: classes.length, stages: stages.length, checkedOffers: accepted.length, moves }, null, 2));
if (process.argv.includes('--write')) {
  let index = 0;
  const updated = original.replace(/("routeOrder":\s*)\d+/g, (_, prefix) => prefix + stages[index++].step.routeOrder);
  assert.equal(index, stages.length);
  fs.writeFileSync(file, updated);
}
