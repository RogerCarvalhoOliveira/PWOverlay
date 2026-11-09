import fs from 'node:fs';
import assert from 'node:assert/strict';
const data=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8'));
const config=JSON.parse(fs.readFileSync('data/quest_objective_bindings.json','utf8'));
const rows=fs.readFileSync('data/task_catalog_144.tsv','utf8').trim().split(/\r?\n/).slice(1).map(r=>r.split('\t'));
const tasks=new Map(rows.map(r=>[Number(r[0]),{id:Number(r[0]),parent:Number(r[1]),top:Number(r[2]),name:r[4],items:r[17],kills:r[18]}]));
const changes=[];
for(const binding of config.bindings){
 const missions=data.missions.filter(m=>m.routeKey===binding.routeKey&&m.name===binding.mission);
 assert.equal(missions.length,1,`Missão não identificada: ${binding.routeKey}/${binding.mission}`);
 const mission=missions[0];
 for(const [stepId,value] of Object.entries(binding.steps)){
  const step=mission.steps.find(s=>s.id===stepId),task=tasks.get(value.taskId);
  assert(step&&task,`Etapa/ID inválido: ${stepId}/${value.taskId}`);
  const before=JSON.stringify(step);
  step.memoryTaskId=task.id;step.title=task.name;
  if(value.action)step.action=value.action;
  if(value.itemIds){
   const items=new Map((task.items||'').split(';').filter(p=>p.includes(':')).map(p=>p.split(':').map(Number)));
   for(const id of value.itemIds)assert(items.has(id),`Item ${id} não pertence a ${task.id}`);
   step.memoryItemIds=value.itemIds;
  }else delete step.memoryItemIds;
  const requirements=step.memoryItemIds?.map(id=>task.items.split(';').map(p=>p.split(':').map(Number)).find(p=>p[0]===id)?.[1])
    || (task.kills?[Number(task.kills.split(':')[1])]:task.items?.includes(':')&&!task.items.includes(';')?[Number(task.items.split(':')[1])]:[]);
  if(requirements.length===1&&/^(?:Pegar|Matar|Coletar|Colete|Pegue|Mate)\s+\(?\d+/i.test(step.action)){
    step.action=step.action.replace(/^((?:Pegar|Matar|Coletar|Colete|Pegue|Mate)\s+\(?)(\d+)/i,`$1${requirements[0]}`);
  }
  if(before!==JSON.stringify(step))changes.push({route:mission.routeKey,mission:mission.name,step:step.id,taskId:task.id,items:step.memoryItemIds||[],title:task.name});
 }
}
console.log(JSON.stringify({boundStages:config.bindings.reduce((n,b)=>n+Object.keys(b.steps).length,0),changed:changes.length,changes},null,2));
if(process.argv.includes('--write'))fs.writeFileSync('data/quest_routes.json',JSON.stringify(data,null,2)+'\n');
