import fs from 'node:fs';
import assert from 'node:assert/strict';
const data=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8'));
const source=JSON.parse(fs.readFileSync('data/guardian_quest_facts.json','utf8'));
const roots=new Map(source.tasks.map(t=>[t.id,t]));
for(const code of ['SK','MS']){
  const missions=data.missions.filter(m=>m.routeKey==='SKMS'&&(m.classScope==='SK/MS'||m.classScope===code));
  const sequence=missions.flatMap(m=>m.steps.map(s=>({m,s}))).sort((a,b)=>a.s.routeOrder-b.s.routeOrder);
  const complete=new Set();
  for(const {m,s} of sequence){
    const root=roots.get(Number(m.id.split('|')[2]));assert(root);
    if(s===m.steps[0]){
      assert.equal(m.minLevel,root.minLevel||Math.max(1,...root.prerequisites.map(id=>roots.get(id)?.minLevel||1)));
      assert.equal(m.maxLevel,root.maxLevel?String(root.maxLevel):'Sem limite');
      assert(m.expectedLevel>=m.minLevel);
      assert(!root.maxLevel||m.expectedLevel<=root.maxLevel,`Limite de aceite: ${code}/${m.name}`);
      for(const id of root.prerequisites)if(missions.some(q=>q.id.endsWith(`|${id}`)))assert(complete.has(id),`Pré-requisito incompleto: ${m.name}/${id}`);
      if(root.npc&&source.npcs[root.npc]?.points.length)assert(s.markerX!=null,`Aceite sem marcador: ${m.name}`);
    }
    if(s===m.steps[m.steps.length-1])complete.add(root.id);
  }
  const named=id=>missions.find(m=>m.id.endsWith(`|${id}`));
  assert(named(22530).steps[0].routeOrder<named(22988).steps[1].routeOrder,'A Reunião deve ser pega antes da recompensa do ancião');
  assert(named(22535).steps[0].routeOrder<named(22534).steps.at(-1).routeOrder,'Pegue Memórias antes da entrega no mesmo NPC');
  assert(named(22450).steps.at(-1).routeOrder<named(22531).steps.at(-1).routeOrder,'Produção deve aguardar a passagem pelo ancião, sem viagem extra');
  assert.equal(named(22450).minLevel,6);
  if(code==='MS')assert(named(23029).steps.at(-1).routeOrder<named(22528).steps[0].routeOrder,'Aprenda a primeira invocação antes de deixar a região inicial');
  assert(!missions.some(m=>m.minLevel<=20&&m.expectedLevel>Number(m.maxLevel)&&Number(m.maxLevel)>0));
}
const old=fs.existsSync('work/pre-classes-routes.json')?JSON.parse(fs.readFileSync('work/pre-classes-routes.json','utf8').replace(/^\uFEFF/,'')):null;
if(old)for(const before of old.missions){
  const after=data.missions.find(m=>m.id===before.id);assert(after,`ID legado removido: ${before.id}`);
  assert.deepEqual(after.steps,before.steps,`Etapas legadas alteradas: ${before.id}`);
}
console.log('Guardiões: limites de aceite, pré-requisitos, ordem, marcadores e IDs legados verificados.');
