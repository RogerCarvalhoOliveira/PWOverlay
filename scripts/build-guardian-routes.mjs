import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

// Run from the repository root. The checked-in facts make rebuilding independent
// of the game installation and of the read-only binary extraction tools.
const sourcePath = 'data/guardian_quest_facts.json';
const clean = s => String(s || '').replace(/\^[0-9a-f]{6}/gi, '').replace(/\s+/g, ' ').trim();
const starts = [22507,22508,22509,22510,22511,22512,22513,22514,22515,
  22520,22521,22522,22523,22524,22525,22526,22527,22528,23757,22988];
// Quest XP x3 already reaches 20 around the sanctuary. Avoid the long level
// 10–19 chain and tasks whose acceptance cap is exceeded by the earlier rewards.
const story = [22530,22531,22450,22532,22534,22535,22486,22487,23066,23067,
  22494,22493,23214,23215];
const training = [23202,23029,23177,23033,23178,23034,23179,23035,23203,23030,
  23180,23036,23204,23031,23181,23037,23182,23038,23205,23032];

if (process.argv.includes('--import-local')) {
  const all = JSON.parse(fs.readFileSync('work/guardian-tasks.json', 'utf8'));
  const kinds = JSON.parse(fs.readFileSync('work/guide-entities-by-kind.json', 'utf8'));
  const resources = JSON.parse(fs.readFileSync('work/resource-facts.json', 'utf8'));
  const coordinates = new Map();
  const coordBytes = fs.readFileSync('work/client-configs_coord_data.txt');
  for (const line of coordBytes.toString('latin1').split(/\r?\n/)) {
    const match = line.match(/^(\d+)\s+world\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)/);
    if (!match) continue;
    const id = Number(match[1]);
    const point = [Math.round(Number(match[2])/10+400),Math.round(Number(match[4])/10+550)];
    if (!coordinates.has(id)) coordinates.set(id, []);
    if (!coordinates.get(id).some(p => p[0]===point[0] && p[1]===point[1])) coordinates.get(id).push(point);
  }
  const selected = [...new Set([...starts,...story,...training])].map(id => {
    const q = all.find(q => q.id===id); assert(q,`Extração ausente: ${id}`); return q;
  });
  const npcs = new Set(), mobs = new Set(), items = new Set(), rewards = new Set();
  const compact = q => {
    if (q.npc) npcs.add(q.npc); if (q.awardNpc) npcs.add(q.awardNpc);
    for (const m of q.monsters) { mobs.add(m.id); if (m.item) items.add(m.item); }
    for (const i of q.items) items.add(i.id);
    q.rewardItems.forEach(id => rewards.add(id));
    return {...q,description:clean(q.description),children:q.children.map(compact)};
  };
  const tasks = selected.map(compact);
  const entity = (id,kind) => ({name:clean(kinds[kind]?.[id]) || `ID ${id}`,points:coordinates.get(id)||[]});
  const resourceItems = {};
  for (const resource of resources) for (let i=1;i<=16;i++) {
    const item = resource[`materials_${i}_id`];
    if (items.has(item)) (resourceItems[item] ||= []).push({id:resource.ID,...entity(resource.ID,'Resources')});
  }
  const rewardName = id => {
    for (const kind of ['Skill Scrolls','Weapons','Armors','Accessories','Quest Rewards','Quest Items','Remedies','Key Items','ATK Hierograms','Quest Activators','Skills'])
      if(kinds[kind]?.[id]) return clean(kinds[kind][id]);
    return `Item ${id}`;
  };
  const source = {
    source:'Cliente local The Classic: tasks.data v124, elements.data v145 e configs/coord_data.txt. Somente fatos das tarefas selecionadas; nenhum binário do jogo.',
    coordinateSha256:crypto.createHash('sha256').update(coordBytes).digest('hex'),
    coordinateConversion:'X=round(worldX/10+400); Y=round(worldZ/10+550); somente mapa world. Marcador de monstro/recurso indica um ponto de ocorrência.',
    playerXpByLevel:Object.fromEntries(Object.entries(JSON.parse(fs.readFileSync('work/exp-tables.json','utf8')).find(t=>t.ID===202)).filter(([key])=>/^exp_\d+$/.test(key))),
    tasks,npcs:Object.fromEntries([...npcs].sort((a,b)=>a-b).map(id=>[id,entity(id,'NPCs')])),
    monsters:Object.fromEntries([...mobs].sort((a,b)=>a-b).map(id=>[id,entity(id,'Monsters')])),
    items:Object.fromEntries([...items].sort((a,b)=>a-b).map(id=>[id,clean(kinds['Quest Items']?.[id])||rewardName(id)])),
    rewards:Object.fromEntries([...rewards].sort((a,b)=>a-b).map(id=>[id,rewardName(id)])),resourceItems
  };
  fs.writeFileSync(sourcePath,JSON.stringify(source,null,2)+'\n');
}

const source=JSON.parse(fs.readFileSync(sourcePath,'utf8'));
const roots=new Map(source.tasks.map(q=>[q.id,q]));
const data=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8'));
const common=data.missions.filter(m=>m.routeKey==='COMMON');
const byId=new Map();
const index=q=>{byId.set(q.id,q);q.children.forEach(index);};source.tasks.forEach(index);
const level=q=>q.minLevel || Math.max(1,...q.prerequisites.map(id=>roots.has(id)?level(roots.get(id)):1));
const point=(entity,near)=>entity?.points?.slice().sort((a,b)=>near?
  Math.hypot(a[0]-near[0],a[1]-near[1])-Math.hypot(b[0]-near[0],b[1]-near[1]):0)[0];
const at=(entity,near)=>{const p=point(entity,near);return `${entity?.name||'NPC indicado no questlog'}${p?` (${p.join(', ')})`:''}`;};
const flatten=q=>[q,...q.children.flatMap(flatten)];
const generated=[];
let counter=0;
function build(root,scope,override={}) {
  const minLevel=override.minLevel || level(root);
  const giver=source.npcs[root.npc];
  const steps=[];
  let current=point(giver);
  const add=(q,action,entity,itemId)=>{
    const p=point(entity,current);
    const step={id:`t${q.id}-${steps.filter(s=>s.memoryTaskId===q.id).length+1}`,title:q.name,action,
      ...(p?{markerX:p[0],markerY:p[1]}:{}),routeOrder:0,memoryTaskId:q.id,
      ...(itemId?{memoryItemIds:[itemId]}:{})};
    steps.push(step);if(p)current=p;
  };
  add(root,giver?`Aceite a missão com ${at(giver)}`:
    `Aceite a missão recebida automaticamente ao atingir o nível ${minLevel}.`,giver);
  function visit(q) {
    // Pure UI notices are represented by the accept step; they require no travel.
    if (q.method===4 && !q.description && q.rewardItems.length===0 && !q.children.length)return;
    if (q.children.length) { q.children.forEach(visit); return; }
    const target=source.npcs[q.awardNpc];
    if (q.monsters.length) {
      const drops=q.monsters.filter(m=>m.item && m.itemCount);
      const kills=q.monsters.filter(m=>m.count>0);
      for(const m of drops){
        const mob=source.monsters[m.id];
        add(q,`Colete ${m.itemCount} ${source.items[m.item]} de ${at(mob,current)}.`,mob,
          drops.length>1?m.item:undefined);
      }
      if(kills.length){
        const mob=source.monsters[kills[0].id];
        add(q,`Mate ${kills.map(m=>`${m.count} ${source.monsters[m.id].name}`).join(' e ')}${point(mob,current)?` na região de (${point(mob,current).join(', ')})`:''}.${kills.length>1?' Vários contadores: confira todos no jogo; use Etapa feita se necessário.':''}`,mob);
      }
      if(target)add(q,`Entregue a missão para ${at(target,current)}.`,target);
    } else if(q.items.length) {
      let collected=false;
      for(const item of q.items){
        const resource=(source.resourceItems[item.id]||[]).find(r=>r.points.length);
        if(!resource)continue;
        collected=true;
        add(q,`Colete ${item.count} ${source.items[item.id]} em ${at(resource,current)}.`,resource,
          q.items.length>1?item.id:undefined);
      }
      if(target)add(q,`Entregue ${q.items.map(i=>`${i.count} ${source.items[i.id]}`).join(' e ')} para ${at(target,current)}.`,target);
      else if(!collected)add(q,q.description || `Obtenha ${q.items.map(i=>`${i.count} ${source.items[i.id]}`).join(' e ')} conforme o questlog.`);
    } else if(target) {
      add(q,`${q.method===5?'Aguarde a conversa e fale':'Fale'} com ${at(target,current)}.${q.method===5?' '+(q.description.match(/\d+ minuto/)?.[0]||'Confirme o diálogo antes de prosseguir.') :''}`,target);
    } else {
      add(q,q.description || `Confirme a etapa “${q.name}” no jogo e use Etapa feita após a conclusão.`);
    }
  }
  visit(root);
  // Specific childless map triggers and altars need their instruction after accepting.
  if(root.id===23215)steps[steps.length-1].action='Use o acesso aberto pelo Ancião Star para ir à Cidade do Dragão. Reúna o grupo e continue a rota comum do nível 20.';
  const xp=flatten(root).reduce((sum,q)=>sum+q.xp,0);
  const rewardIds=[...new Set(flatten(root).flatMap(q=>q.rewardItems))];
  const skillQuest=training.includes(root.id);
  const mission={id:`SKMS|${scope}|${root.id}`,routeKey:'SKMS',order:++counter,classScope:scope,
    minLevel,expectedLevel:minLevel,maxLevel:root.maxLevel?String(root.maxLevel):'Sem limite',name:root.name,
    npc:giver?at(giver):'Missão automática / pop-up',
    location:giver?at(giver):(source.npcs[flatten(root).find(q=>q.awardNpc)?.awardNpc]?at(source.npcs[flatten(root).find(q=>q.awardNpc)?.awardNpc]):'Consulte a etapa atual'),
    baseXp:xp,xp3:xp*3,priority:skillQuest||[22450,22487,22493,23215,23757].includes(root.id)?'Essencial':'Alta',
    benefit:skillQuest?'Libera invocação ou planta do Místico; '+rewardIds.map(id=>source.rewards[id]).join(', '):
      rewardIds.length?rewardIds.map(id=>source.rewards[id]).join(', '):'XP ×3 e progresso da história',
    instruction:skillQuest?'Desvio exclusivo do Místico. Complete a cadeia e retorne ao grupo no ponto da rota comum.':
      'Siga a ordem global das ações. Aceite e entregue missões próximas na mesma passagem; os nomes são os do cliente instalado.',steps,...override};
  generated.push(mission);return mission;
}
for(const id of starts){
  const q=roots.get(id);
  build(q,q.occupations.length===1?(q.occupations[0]===8?'SK':'MS'):'SK/MS');
}
for(const id of story)build(roots.get(id),'SK/MS');
for(const id of training)build(roots.get(id),'MS');

const get=(id,scope)=>{const m=generated.find(m=>m.id.endsWith(`|${id}`)&&(!scope||m.classScope===scope));assert(m,`Missão ausente ${id}/${scope}`);return m;};
const sorted=[
  ...[22507,22508,22509,22510,22511,22512,22513].map(id=>get(id)),
  ...[22520,22521,22522,22523,22524,22525,23202,23029,22526].map(id=>get(id)),
  // Both instructor visits are required by the client (parallel children, no
  // choose-one/parent-success flag), even though skills learned depend on class.
  get(23757),get(22514),get(22515),get(22527),get(22528),
  // Accept A Reunião (maximum level 11) before the optional welcome reward.
  ...[22530,22988,22450,22486,22487,22531,23177,23033,
    22532,22534,22535,23066,23067,23178,23034,22494,22493,23214,23215].map(id=>get(id)),
  ...generated.filter(m=>m.minLevel>20).sort((a,b)=>a.minLevel-b.minLevel||a.order-b.order)
];
assert.equal(new Set(sorted).size,generated.length,'Missão ausente ou duplicada no circuito');
let earlyOrder=20000;
const laterOffset=new Map();
for(const m of sorted){
  if(m.expectedLevel<=20){
    m.order=sorted.indexOf(m)+1;
    m.steps.forEach(s=>s.routeOrder=earlyOrder+=10);
  }else{
    const levelCommon=common.filter(q=>q.minLevel>=m.expectedLevel).sort((a,b)=>a.minLevel-b.minLevel||a.order-b.order);
    const anchor=Math.min(...levelCommon.filter(q=>q.minLevel===levelCommon[0].minLevel).flatMap(q=>q.steps.map(s=>s.routeOrder)));
    let offset=laterOffset.get(anchor)||1;
    m.steps.forEach(s=>{assert(offset<100,'Intervalo insuficiente entre etapas comuns');s.routeOrder=anchor+offset++;});
    laterOffset.set(anchor,offset);m.order=10000+m.expectedLevel*100+(training.indexOf(Number(m.id.split('|')[2]))%10);
  }
}
const forge=generated.find(m=>m.id.endsWith('|22450'));
forge.instruction='No nível 6, aceite o aviso de produção e fale com o Ancião Star antes de sair de Tellus. Não aceite as missões específicas de ofício.';
// Two same-NPC pickups must happen before XP rewards close their level caps.
const early=sorted.filter(m=>m.minLevel<=20).flatMap(m=>m.steps).sort((a,b)=>a.routeOrder-b.routeOrder);
const slots=early.map(s=>s.routeOrder);
const moveBefore=(step,anchor)=>{early.splice(early.indexOf(step),1);early.splice(early.indexOf(anchor),0,step);};
const meeting=get(22530),welcome=get(22988),memories=get(22535),temple=get(22534);
moveBefore(welcome.steps[0],meeting.steps[1]);
moveBefore(welcome.steps[1],meeting.steps[1]);
moveBefore(memories.steps[0],temple.steps[temple.steps.length-1]);
moveBefore(get(23067).steps[0],get(23066).steps[1]);
meeting.instruction='Pegue A Reunião antes de entregar Visitando o Ancião: o aceite termina no nível 11. O multiplicador ×3 torna essa ordem importante.';
memories.instruction='Pegue Memórias com o Oficial Qine antes de entregar O Guardião do Templo, no mesmo NPC. Limite de aceite: nível 16.';
early.forEach((s,i)=>s.routeOrder=slots[i]);

// Forecast from the client's player XP table, quest rewards only. Monster XP
// and server bonuses can increase the actual level; never treat it as live data.
for(const code of ['SK','MS']){
  let playerLevel=1,remainingXp=0;
  const applicable=sorted.filter(m=>m.minLevel<=20&&(m.classScope==='SK/MS'||m.classScope===code));
  const stages=applicable.flatMap(m=>m.steps.map(s=>({m,s}))).sort((a,b)=>a.s.routeOrder-b.s.routeOrder);
  for(const {m,s} of stages){
    if(s===m.steps[0]){
      playerLevel=Math.max(playerLevel,m.minLevel);
      m.expectedLevel=Math.max(m.expectedLevel,playerLevel);
      if(Number(m.maxLevel)>0)assert(playerLevel<=Number(m.maxLevel),`${code}/${m.name}: previsão ${playerLevel} acima do máximo ${m.maxLevel}`);
    }
    if(s===m.steps[m.steps.length-1]){
      remainingXp+=m.xp3;
      while(playerLevel<60&&remainingXp>=source.playerXpByLevel[`exp_${playerLevel}`]){
        remainingXp-=source.playerXpByLevel[`exp_${playerLevel}`];playerLevel++;
      }
    }
  }
}

// Share human/Tideborn quests without changing legacy mission or stage IDs.
for(const m of data.missions){
  if(m.routeKey==='WR')m.classScope='WR/MG';
  if(m.routeKey==='MC')m.classScope='MC/ES';
  if(m.routeKey==='WR'&&m.name==='Teste de Salto')m.instruction='Pegue o baú sobre o Portão Sul. Escolha o amuleto de HP ou MP conforme a necessidade da classe (WR/MG).';
  if(m.routeKey==='WR'&&m.benefit==='Espada inicial e poções')m.benefit='Arma inicial adequada à classe e poções';
  if(m.routeKey==='MC'&&/Adaga da Provação/.test(m.benefit))m.benefit='Arma da Provação adequada à classe e 10 de fama';
}
data.missions=data.missions.filter(m=>m.routeKey!=='SKMS').concat(generated.sort((a,b)=>a.order-b.order));
data.generatedFrom=`Rotas 1–${Math.max(...data.missions.map(m=>m.expectedLevel))}, XP ×3; cinco raças e dez classes; Guardiões conferidos nos dados do cliente`;
console.log(JSON.stringify({guardianMissions:generated.length,guardianStages:generated.reduce((n,m)=>n+m.steps.length,0),
  SK:generated.filter(m=>m.classScope!=='MS').length,MS:generated.filter(m=>m.classScope!=='SK').length,
  totalMissions:data.missions.length,totalStages:data.missions.reduce((n,m)=>n+m.steps.length,0)},null,2));
if(process.argv.includes('--write'))fs.writeFileSync('data/quest_routes.json',JSON.stringify(data,null,2)+'\n');
