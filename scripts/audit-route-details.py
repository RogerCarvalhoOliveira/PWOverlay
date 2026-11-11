"""Compare route steps with the 1.4.4 task and world-coordinate extracts."""

import argparse
from collections import Counter, defaultdict
import json
from pathlib import Path
import re
import unicodedata


def normalize(value):
    without_accents = ''.join(char for char in unicodedata.normalize('NFD', value.casefold())
                              if unicodedata.category(char) != 'Mn')
    return re.sub(r'[^a-z0-9]+', ' ', without_accents).strip()


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('facts', type=Path, help='Extração integral do EndgameTaskDump')
    parser.add_argument('names', type=Path, help='Nomes do elements.data por categoria')
    parser.add_argument('coordinates', type=Path, help='configs/coord_data.txt')
    parser.add_argument('--routes', type=Path, help='JSON alternativo para comparação de versões')
    parser.add_argument('--json', type=Path)
    args = parser.parse_args()
    payload = read_json(args.routes or Path(__file__).resolve().parents[1] / 'data' / 'quest_routes.json')
    names = read_json(args.names)
    tasks, roots = {}, {}
    by_title = defaultdict(list)

    def index(task, root):
        tasks[task['id']] = task
        roots[task['id']] = root
        by_title[normalize(task['name'])].append(task)
        for child in task['children']:
            index(child, root)

    for task in read_json(args.facts):
        index(task, task['id'])
    continuations, granted = defaultdict(set), set()
    for task_id, task in tasks.items():
        next_id = task['newTask']
        if next_id in roots and roots[task_id] != roots[next_id]:
            continuations[roots[task_id]].add(roots[next_id])
            granted.add(next_id)

    npc_points = defaultdict(set)
    for line in args.coordinates.read_text(encoding='latin1').splitlines():
        match = re.match(r'^(\d+)\s+world\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)', line)
        if match:
            npc_points[int(match[1])].add((round(float(match[2]) / 10 + 400), round(float(match[4]) / 10 + 550)))

    def mission_roots(mission):
        direct = {roots[task_id] for group in mission.get('historyTaskGroups', [])
                  for task_id in group if task_id in roots}
        expanded = set(direct)
        pending = list(direct)
        while pending:
            for next_root in continuations[pending.pop()] - expanded:
                expanded.add(next_root)
                pending.append(next_root)
        return direct, expanded

    def candidates(step, allowed):
        explicit = tasks.get(step.get('memoryTaskId'))
        if explicit:
            return [explicit]
        return [task for title in step['title'].split(' / ')
                for task in by_title[normalize(title)] if roots[task['id']] in allowed]

    def npc_for_action(action, task, explicit):
        if re.match(r'^(Aceite|Pegue a missão)', action, re.I):
            return task['npc']
        if re.match(r'^(Entregue|Reporte|Retorne|Volte|NPC Final)', action, re.I):
            return task['awardNpc']
        if explicit and re.match(r'^(Fale com|Converse com)', action, re.I):
            if not task['npc']:
                return task['awardNpc']
            if not task['awardNpc']:
                return task['npc']
        return 0

    rows, coverage = defaultdict(list), Counter()
    active = [mission for mission in payload['missions'] if not mission.get('aliasOf')]
    linked = {}
    for mission in active:
        coverage['missions'] += 1
        direct, expanded = mission_roots(mission)
        mission_links = []
        for step in mission['steps']:
            coverage['steps'] += 1
            key = (mission['id'], step['id'])
            action = step['action']
            facts = candidates(step, expanded)
            linked[key] = facts
            mission_links.extend(facts)
            if facts:
                coverage['boundSteps'] += 1
                if any(roots[task['id']] in expanded - direct for task in facts):
                    coverage['inferredContinuations'] += 1
            npcs = {npc_for_action(action, task, bool(step.get('memoryTaskId'))) for task in facts}
            if len(npcs) == 1:
                npc = next(iter(npcs))
                points = npc_points[npc]
                if npc and points and step.get('markerX') is not None and not re.search(
                        r'passado|instância|caverna|mapa a\d|palácio| — sábio| — demoníaco', action, re.I):
                    coverage['npcCoordinates'] += 1
                    x, y = step['markerX'], step['markerY']
                    nearest = min(points, key=lambda point: (point[0] - x) ** 2 + (point[1] - y) ** 2)
                    delta = ((nearest[0] - x) ** 2 + (nearest[1] - y) ** 2) ** .5
                    if delta > 4:
                        rows['npcOffset'].append([*key, npc, names['NPCs'].get(str(npc)), [x, y],
                                                  nearest, round(delta, 1), action])
            if not facts or not re.match(r'^(Matar|Mate)\b', action, re.I):
                continue
            for task in {task['id']: task for task in facts}.values():
                for monster in task['monsters']:
                    name = names['Monsters'].get(str(monster['id']))
                    if not name or not monster['count']:
                        continue
                    match = re.search(r'\b(\d+) ' + re.escape(normalize(name)) + r'\b', normalize(action))
                    if not match:
                        continue
                    coverage['namedKillCounters'] += 1
                    if len(task['monsters']) > 1:
                        coverage['multiKillCounters'] += 1
                    stated = int(match[1])
                    if stated != monster['count']:
                        rows['killCount'].append([*key, task['id'], name, stated, monster['count'], action])

        first = mission['steps'][0]
        first_facts = linked[mission['id'], first['id']]
        represented_ids = {task['id'] for task in mission_links}
        for root_id in direct:
            root = tasks[root_id]
            points = npc_points[root['npc']]
            if first.get('markerX') is None or not points or any(
                    (first['markerX'] - x) ** 2 + (first['markerY'] - y) ** 2 <= 4 ** 2 for x, y in points):
                continue
            if (root_id not in represented_ids and root['npc'] and not root['automatic'] and
                    root_id not in granted and first_facts and all(roots[task['id']] == root_id for task in first_facts)):
                rows['missingPickup'].append([mission['id'], root_id, root['npc'], first['action']])

    classes = ('WR', 'MG', 'ES', 'WF', 'WB', 'MC', 'EA', 'EP', 'SK', 'MS')
    seen = set()
    for occupation, code in enumerate(classes):
        for path in ('GOD', 'EVIL'):
            for gender in (1, 2):
                coverage['scenarios'] += 1
                completes, starts = defaultdict(list), {}

                def usable(task):
                    return (not task['occupations'] or occupation in task['occupations']) and (
                        not task['gender'] or task['gender'] == gender)

                for mission in active:
                    if mission['classScope'] != 'ALL' and code not in mission['classScope'].split('/'):
                        continue
                    if mission.get('cultivationPath') and mission['cultivationPath'] != path:
                        continue
                    by_task = defaultdict(list)
                    for step in mission['steps']:
                        facts = {task['id']: task for task in linked[mission['id'], step['id']] if usable(task)}
                        if len(facts) == 1:
                            by_task[next(iter(facts))].append(step['routeOrder'])
                    for task_id, orders in by_task.items():
                        start = min(orders)
                        previous = starts.get(task_id)
                        if previous is None or start < previous[1]:
                            starts[task_id] = (mission['id'], start)
                        completes[task_id].append(max(orders))
                    for group in mission.get('historyTaskGroups', []):
                        usable_ids = [task_id for task_id in group if task_id in tasks and usable(tasks[task_id])]
                        if len(usable_ids) == 1 and len(mission.get('historyTaskGroups', [])) == 1:
                            completes[usable_ids[0]].append(mission['steps'][-1]['routeOrder'])
                for task_id, (mission_id, start) in starts.items():
                    task = tasks[task_id]
                    prerequisites = task['prerequisites']
                    needed = task['prerequisiteLeast'] or len(prerequisites)
                    if not needed:
                        continue
                    coverage['prerequisiteChecks'] += 1
                    late = [(previous, min(completes[previous])) for previous in prerequisites
                            if completes[previous] and min(completes[previous]) >= start]
                    if len(prerequisites) - len(late) < needed:
                        signature = (mission_id, task_id, start, tuple(late))
                        if signature not in seen:
                            rows['latePrerequisite'].append([mission_id, task_id, start, late])
                            seen.add(signature)

    result = {'coverage': dict(coverage), 'findings': dict(rows)}
    if args.json:
        args.json.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'coverage': result['coverage'],
                      'findings': {key: len(entries) for key, entries in rows.items()}}, ensure_ascii=False))


if __name__ == '__main__':
    main()
