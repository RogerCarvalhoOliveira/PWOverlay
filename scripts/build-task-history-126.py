"""Match 1.2.6 task IDs to 1.4.4 route roots using local EndgameTaskDump exports."""

import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path


def read_catalog(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def signature(task, root=True, relax_container=False):
    fields = ("name", "method", "finishType", "minLevel", "period", "gender", "reincarnation",
              "chooseOne", "randomOne", "ordered")
    # NPCs on leaf objectives and required quantities can differ between editions.
    container = relax_container and not root and task["children"] and not task["monsters"] and not task["items"]
    return (tuple(task[key] for key in fields if not (container and key == "method")),
            (task["npc"], task["awardNpc"]) if root else (),
            tuple(sorted(task["occupations"])),
            tuple((mob["id"], mob["item"]) for mob in task["monsters"]),
            tuple(item["id"] for item in task["items"]),
            tuple(signature(child, False, relax_container) for child in task["children"]))


def build(reference, sources):
    canonical = {task["id"]: signature(task) for task in reference}
    canonical_tasks = {task["id"]: task for task in reference}
    canonical_by_id = {task["id"]: signature(task, relax_container=True) for task in reference}
    by_signature = defaultdict(list)
    for task_id, value in canonical.items():
        by_signature[value].append(task_id)

    matches = defaultdict(set)
    metadata = defaultdict(set)

    def register(task, root, canonical_task, is_root):
        matches[task["id"]].add((root, is_root, canonical_task["id"]) if canonical_task is not None else None)
        for index, child in enumerate(task["children"]):
            canonical_child = canonical_task["children"][index] if canonical_task is not None else None
            register(child, root, canonical_child, False)

    for source in sources:
        for task in source:
            task_id = task["id"]
            candidates = by_signature.get(signature(task), [])
            root = task_id if task_id in candidates else None
            if root is None and task_id in canonical_by_id and canonical_by_id[task_id] == signature(task, relax_container=True):
                root = task_id
            if root is None and len(candidates) == 1 and task_id not in canonical:
                root = candidates[0]
            register(task, root, canonical_tasks.get(root), True)
            if root is not None:
                prerequisites = tuple(task["prerequisites"])
                least = task["prerequisiteLeast"]
                metadata[task_id].add((task["minLevel"], task["maxLevel"], task["minimumReputation"],
                                       least if least > 0 else len(prerequisites), prerequisites,
                                       task["npc"] == 0 and not task["automatic"]))
    result = {}
    for task_id, options in matches.items():
        if len(options) != 1 or None in options:
            continue
        root, is_root, canonical_id = next(iter(options))
        rules = metadata[task_id]
        if is_root and len(rules) == 1:
            minimum, maximum, reputation, required, prerequisites, triggered = next(iter(rules))
            result[task_id] = (root, 1, minimum, maximum, reputation, required,
                               ",".join(map(str, prerequisites)), int(triggered), canonical_id)
        else:
            result[task_id] = (root, int(is_root), "", "", "", "", "", "", canonical_id)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reference", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("sources", type=Path, nargs="+")
    args = parser.parse_args()
    matches = build(read_catalog(args.reference), [read_catalog(path) for path in args.sources])
    with args.output.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.writer(stream, delimiter="\t", lineterminator="\n")
        writer.writerow(("id_126", "root_144", "is_root", "min_level", "max_level",
                         "min_reputation", "required_prerequisites", "prerequisites_126", "needs_activation", "task_144"))
        for task_id, fields in sorted(matches.items()):
            writer.writerow((task_id, *fields))
    print(f"{len(matches)} IDs, {sum(fields[1] for fields in matches.values())} roots")


if __name__ == "__main__":
    main()
