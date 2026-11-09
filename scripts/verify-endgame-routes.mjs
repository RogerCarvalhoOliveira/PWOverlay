import fs from 'node:fs';
import assert from 'node:assert/strict';
const data=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8'));
const source=JSON.parse(fs.readFileSync('data/endgame_quest_facts.json','utf8'));
const catalog=new Map(fs.readFileSync('data/task_catalog_144.tsv','utf8').trim().split(/\r?\n/).slice(1).map(l=>{
  const p=l.split('\t');return[+p[0],{title:p[4],items:(p[17]||'').split(';').filter(i=>i.includes(':')).map(i=>+i.split(':')[0])}];
}));
const generated=data.missions.filter(m=>m.id.startsWith('ENDGAME|'));
const rootMission=new Map(generated.filter(m=>/^\d+$/.test(m.id.split('|')[2])).map(m=>[+m.id.split('|')[2],m]));
const sourceEntry=new Map(source.facts.map(e=>[e.id,e]));
for(const e of source.facts){
  const m=rootMission.get(e.id);assert(m,`Missão ausente: ${e.id}`);
  assert.equal(m.name,e.task.name);assert.equal(m.cultivationPath||null,e.path||null);
  assert.equal(m.maxLevel,e.task.maxLevel?String(e.task.maxLevel):'Sem limite');
  if(e.task.minLevel)assert.equal(m.minLevel,e.task.minLevel);
  assert(m.expectedLevel>=m.minLevel&&m.expectedLevel<=105,`Faixa inválida: ${m.id}`);
  if(e.task.maxLevel)assert(m.expectedLevel<=e.task.maxLevel,`Quest agendada depois do máximo: ${m.id}`);
  for(const id of e.previous){
    const previous=rootMission.get(id);assert(previous);
    assert(previous.steps.at(-1).routeOrder<m.steps[0].routeOrder,`Pré-requisito fora de ordem: ${id} → ${e.id}`);
    const path=sourceEntry.get(id).path;
    if(path)assert.equal(e.path,path,`Pré-requisito de outro cultivo: ${id} → ${e.id}`);
  }
  for(const stage of m.steps){
    const task=catalog.get(stage.memoryTaskId);assert(task&&task.title===stage.title,`ID/título inválido: ${m.id}/${stage.id}`);
    for(const id of stage.memoryItemIds||[])assert(task.items.includes(id),`Material inválido: ${m.id}/${stage.id}/${id}`);
  }
}
const choice=generated.filter(m=>m.cultivationChoice);assert.equal(choice.length,1);assert.equal(choice[0].expectedLevel,89);
for(const level of [61,65,69,70,71,75,79,81,85,89,90,99,100,101,102,103,104,105])assert(generated.some(m=>m.expectedLevel===level),`Faixa ausente: ${level}`);
for(const [cult,coin] of [[2122,8746],[2153,8762],[2804,8778],[2818,8779],[3227,8780],[3204,8781]]){
  const c=rootMission.get(cult),a=rootMission.get(coin);
  assert(c.steps[0].routeOrder<a.steps[0].routeOrder&&a.steps.at(-1).routeOrder<c.steps[1].routeOrder,`Moeda não foi preparada com o cultivo: ${cult}/${coin}`);
  assert.equal(c.cultivationPath||null,a.cultivationPath||null);
}
const previousFile=process.argv.find(arg=>arg.startsWith('--previous='))?.slice('--previous='.length);
if(previousFile){
  const old=JSON.parse(fs.readFileSync(previousFile,'utf8').replace(/^\uFEFF/,''));
  for(const m of old.missions)assert.deepEqual(data.missions.find(n=>n.id===m.id),m,`Missão legada alterada: ${m.id}`);
}
console.log(`Verificadas ${generated.length} entradas novas, pré-requisitos, níveis máximos, cultivos, moedas e ${generated.reduce((n,m)=>n+m.steps.length,0)} etapas.`);
