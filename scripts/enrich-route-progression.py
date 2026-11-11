import argparse
import collections
import json
import re
from pathlib import Path

parser = argparse.ArgumentParser(description='Vincula a rota ao histórico de missões e à curva de XP do cliente.')
parser.add_argument('facts', type=Path, help='Extração integral de tasks.data (EndgameTaskDump)')
parser.add_argument('experience', type=Path, help='Curvas extraídas de elements.data (registro 202)')
parser.add_argument('--write', action='store_true')
args = parser.parse_args()
route_path = Path(__file__).resolve().parent.parent / 'data' / 'quest_routes.json'
route = json.loads(route_path.read_text(encoding='utf-8-sig'))
roots = json.loads(args.facts.read_text(encoding='utf-8-sig'))
curve = next(table for table in json.loads(args.experience.read_text(encoding='utf-8-sig')) if table['ID'] == 202)


def descendants(task):
    yield task
    for child in task['children']:
        yield from descendants(child)


def names(text):
    return {part.strip().casefold() for part in text.split(' / ') if part.strip()}


root_by_id = {task['id']: task for task in roots}
root_for = {child['id']: root['id'] for root in roots for child in descendants(root)}
roots_by_name = collections.defaultdict(list)
for root in roots:
    roots_by_name[root['name'].strip().casefold()].append(root)
all_tasks = [task for root in roots for task in descendants(root)]
prerequisites = {root_for.get(required, required) for task in all_tasks for required in task['prerequisites']}
professions = {'WR': 0, 'MG': 1, 'ES': 2, 'WF': 3, 'WB': 4, 'MC': 5, 'EA': 6, 'EP': 7, 'SK': 8, 'MS': 9}
protected_benefits = re.compile(r'equip|arma|armadura|colar|anel|amuleto|manta|bolsa|invent|teleport|cultiv|habilidade|reputa|desbloque|acesso|moeda|mascote|daimon|montaria|ba[uú]|dungeon|instância', re.I)
completion_overrides = {
    'WR|WR|1|Primeiro Teste / Exposição de Talento': [[1173, 1198]],
    'WR|WR|19|Moeda Sangrenta de Ferro': [[8738]],
    'EAEP|EA/EP|19|Moeda Sangrenta de Ferro': [[8739]],
    'COMMON|ALL|39|Cultivo 39 — Aparição do Vazio de Hades': [[990]],
    'COMMON|ALL|44|Lâmina Sagrada de Meta': [[1881]],
}


def exact_roots(mission, name):
    classes = {professions[code] for code in mission['classScope'].split('/') if code in professions}
    candidates = [q for q in roots_by_name[name] if not classes or not q['occupations'] or classes.intersection(q['occupations'])]
    same_level = [q for q in candidates if q['minLevel'] == mission['minLevel']]
    return same_level or candidates


def completion(mission):
    # These guide entries include a dungeon run after receiving its coin.
    # A recorded coin handout proves only the initial step.
    if 'DG' in mission['id'] and 'Moeda Sangrenta' in mission['id'] and len(mission['steps']) > 1:
        return []
    if mission['id'] in completion_overrides:
        return completion_overrides[mission['id']]
    explicit = {root_for[s['memoryTaskId']] for s in mission['steps'] if s.get('memoryTaskId') in root_for}
    if mission['id'].startswith(('ENDGAME|', 'SKMS|')):
        numeric_id = mission['id'].rsplit('|', 1)[-1]
        if numeric_id.isdigit() and int(numeric_id) in root_by_id:
            explicit.add(int(numeric_id))
    if explicit:
        return [[task_id] for task_id in sorted(explicit)]
    candidates = {q['id']: q for name in names(mission['name']) for q in exact_roots(mission, name)}
    if len(candidates) == 1:
        return [[next(iter(candidates))]]
    if len(candidates) == 2 and {q['gender'] for q in candidates.values()} == {1, 2}:
        return [sorted(candidates)]
    class_variants = [set(q['occupations']) for q in candidates.values()]
    if len(class_variants) > 1 and all(class_variants):
        classes = {professions[code] for code in mission['classScope'].split('/') if code in professions}
        if set.union(*class_variants) == classes and sum(map(len, class_variants)) == len(classes):
            return [sorted(candidates)]
    return []


def optional(mission, groups):
    if not groups or not mission.get('xp3') or mission['priority'] not in ('Alta', 'Opcional') or mission.get('repeatable'):
        return False
    if mission.get('cultivationPath') or re.search(r'cultivo|\bDG\d|pilar|passado|desbloque|habilidade', mission['id'], re.I):
        return False
    if protected_benefits.search(mission.get('benefit') or ''):
        return False
    for group in groups:
        for task_id in group:
            if task_id in prerequisites:
                return False
            for task in descendants(root_by_id[task_id]):
                if task['newPeriod'] or task['skill'] or task['newTask']:
                    return False
                if any(root_for.get(required, required) != task_id for required in task['prerequisites']):
                    return False
    return True


route['xpToNextLevel'] = [curve['exp_' + str(level)] for level in range(1, 105)]
counts = collections.Counter()
missing = []
for mission in route['missions']:
    groups = completion(mission) if not mission.get('cultivationChoice') else []
    mission.pop('historyTaskGroups', None)
    mission.pop('xpOptional', None)
    if groups:
        mission['historyTaskGroups'] = groups
        counts['history'] += 1
    else:
        missing.append(mission['id'])
    if optional(mission, groups):
        mission['xpOptional'] = True
        counts['optional'] += 1
        counts['optional_' + mission['routeKey']] += 1
print(json.dumps({'counts': counts, 'unmapped_count': len(missing), 'early_unmapped': [x for x in missing if not x.startswith('ENDGAME|')][:50]}, ensure_ascii=True, indent=2))
if args.write:
    route['schemaVersion'] = max(route['schemaVersion'], 9)
    # Keep the curve before the large mission array for review.
    missions = route.pop('missions')
    route['missions'] = missions
    text = json.dumps(route, ensure_ascii=False, indent=2)
    text = re.sub(r'("historyTaskGroups": )(\[\s*(?:\[[\d,\s]+\],?\s*)+\])',
                  lambda match: match[1] + json.dumps(json.loads(match[2])), text)
    route_path.write_text(text + '\n', encoding='utf-8')
