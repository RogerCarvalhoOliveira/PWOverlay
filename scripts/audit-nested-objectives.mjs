import fs from 'node:fs';
import assert from 'node:assert/strict';
const data=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8'));
const bindings=JSON.parse(fs.readFileSync('data/quest_objective_bindings.json','utf8')).bindings;
const rows=fs.readFileSync('data/task_catalog_144.tsv','utf8').trim().split(/\r?\n/).slice(1).map(l=>l.split('\t'));
const tasks=rows.map(r=>({id:Number(r[0]),parent:Number(r[1]),top:Number(r[2]),name:r[4],method:Number(r[5]),monsters:Number(r[9]),items:r[17],kills:r[18]?.includes(':')?r[18]:''}));
const byId=new Map(tasks.map(t=>[t.id,t]));
const issues=[],manual=[];let reviewed=0,explicit=0,selected=0;
for(const m of data.missions)for(const s of m.steps){
 reviewed++;
 if(s.memoryTaskId){
  explicit++;const t=byId.get(s.memoryTaskId);
  assert(t&&t.name===s.title,`Associação inválida ${m.name}/${s.id}`);
  if(s.memoryItemIds?.length){selected++;const ids=new Set(t.items.split(';').map(p=>Number(p.split(':')[0])));for(const id of s.memoryItemIds)assert(ids.has(id),'Seletor de item inválido');}
 }
 if(!/^(?:pegar|pegue|colet|obtenha|matar|mate|complete|conclua)/i.test(s.action))continue;
 const tops=new Set(tasks.filter(t=>t.name===m.name).map(t=>t.top));
 let matches=s.memoryTaskId?[byId.get(s.memoryTaskId)]:tasks.filter(t=>t.name===s.title);
 const chain=matches.filter(t=>tops.has(t.top));if(!s.memoryTaskId&&chain.length)matches=chain;
 if(matches.length!==1){manual.push({route:m.routeKey,mission:m.name,step:s.id,reason:'ID ausente ou ambíguo'});continue;}
 const task=matches[0];
 const children=tasks.filter(t=>t.parent===task.id);
 if(task.method===0&&children.some(t=>t.items?.includes(':')||t.kills))issues.push(`${m.routeKey}/${m.name}/${s.id}: objetivo ainda aponta para pai ${task.id}`);
 const siblings=m.steps.filter(step=>step.memoryTaskId===task.id||(!step.memoryTaskId&&step.title===task.name));
 if(task.items?.includes(';')&&siblings.filter(step=>/^(?:pegar|pegue|colet|obtenha)/i.test(step.action)).length>1&&!s.memoryItemIds?.length)
  issues.push(`${m.routeKey}/${m.name}/${s.id}: materiais compartilhados sem seletor`);
 if(task.monsters>1&&!task.items?.includes(':'))manual.push({route:m.routeKey,mission:m.name,step:s.id,reason:'Vários contadores de abate na mesma entrada; layout ainda não validado'});
}
console.log(JSON.stringify({reviewedStages:reviewed,explicitBindings:explicit,chains:bindings.length,independentItemStages:selected,issues,manualDiagnosticCount:manual.length,...(process.argv.includes('--summary')?{}:{manualDiagnostics:manual})},null,2));
assert.equal(issues.length,0,'Há objetivos aninhados ou materiais ainda sem associação');
