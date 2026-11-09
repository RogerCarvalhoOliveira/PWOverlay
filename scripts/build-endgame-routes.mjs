import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

const factsPath='data/endgame_quest_facts.json';
const flat=q=>[q,...q.children.flatMap(flat)];
const clean=s=>String(s||'').replace(/\^[a-f0-9]{6}/gi,'').replace(/&#\d+;/g,'').replace(/\s+/g,' ').trim();
const periodPath=p=>p>=20&&p<=22?'GOD':p>=30&&p<=32?'EVIL':null;
const periodLevel=p=>p>=20&&p<=32?89:p===8?79:p===7?69:p===6?59:0;
const cultivations=[2113,2117,2118,2122,2642,2136,2138,2140,2148,2153,2169,2804,2818,3204,3227,14015,14016,14161];
const coins=[8746,8762,8778,8779,8780,8781,7969,7971,7973,8437,8439,8441,7977,
  7979,7981,7983,7985,7987,7989,7991,7993,7995,7997,7999,8001,8003,8005];
const explicitPath=new Map([
  ...[2804,8778,7985,7987,7989,3222,3223,3224,3227,8780,7991,7993,7995,7997,14161,14015].map(id=>[id,'GOD']),
  ...[2818,8779,7979,7981,7983,3198,3199,3200,3204,8781,7999,8001,8003,8005,14016].map(id=>[id,'EVIL'])
]);
const dungeonLevel=id=>[7969,7971,7973,8746].includes(id)?70:
  [8437,8439,8441,7977,8762].includes(id)?79:id>=7979&&id<=7989||[8778,8779].includes(id)?89:99;

if(process.argv.includes('--import-local')){
  const all=JSON.parse(fs.readFileSync('work/endgame-tasks.json','utf8'));
  const roots=new Map(all.map(q=>[q.id,q]));
  const owner=new Map(all.flatMap(q=>flat(q).map(t=>[t.id,q.id])));
  const previous=new Map(all.map(q=>[q.id,new Set(q.prerequisites.map(id=>owner.get(id)).filter(id=>id&&id!==q.id))]));
  for(const q of all)for(const task of flat(q))if(roots.has(task.newTask))previous.get(task.newTask).add(q.id);
  const pathMemo=new Map();
  function pathOf(q,seen=new Set()){
    if(explicitPath.has(q.id))return explicitPath.get(q.id);
    if(pathMemo.has(q.id))return pathMemo.get(q.id);
    if(seen.has(q.id))return null;
    seen=new Set([...seen,q.id]);
    const paths=[...new Set([periodPath(q.period),...[...previous.get(q.id)].map(id=>pathOf(roots.get(id),seen))].filter(Boolean))];
    const result=paths.length===1?paths[0]:null;pathMemo.set(q.id,result);return result;
  }
  const selected=new Map();
  const add=(id,category,scheduledLevel)=>{
    const q=roots.get(id);assert(q,`ID não extraído: ${id}`);
    const existing=selected.get(id);
    if(!existing)selected.set(id,{id,category,scheduledLevel:scheduledLevel||q.minLevel,path:pathOf(q)});
    else if(category==='cultivation'||category==='coin')existing.category=category;
  };
  for(const id of cultivations)add(id,'cultivation',id===2122?70:0);
  // Retrieve all explicit prerequisites and automatic continuation tasks of cultivation.
  let changed=true;
  while(changed){changed=false;for(const entry of [...selected.values()]){
    const q=roots.get(entry.id);
    const related=[...q.prerequisites.map(id=>owner.get(id)),...flat(q).map(t=>t.newTask)].filter(Boolean);
    for(const id of related){
      const task=roots.get(id);if(!task||task.minLevel>105||(task.minLevel>0&&task.minLevel<=60))continue;
      if(!selected.has(id)){add(id,'cultivation');changed=true;}
    }
  }}
  for(const id of coins)add(id,'coin',Math.max(roots.get(id).minLevel,dungeonLevel(id)));
  for(const id of [6525,6526,6527,6528,6529,6530,6531,6532])add(id,'rank');
  // Classic, repeatable activities that remain eligible through 105. No Dusk
  // bounty variants or Morai-only quests are used for this client's classic guide.
  for(const id of [9545,11664])add(id,'daily',100);
  const xp=q=>flat(q).reduce((n,t)=>n+t.xp,0);
  const banned=/Oráculo|Livro Cel|Talismã|Loteria|Rank|Insígnia|Troféu|Tarefa do|Arma dos Sábios|Artesão (?:Prim|Inter|Avan)|Alfaiate (?:Prim|Inter|Avan)/i;
  const allowed=q=>q.id<8000&&q.type===0&&q.frequency===0&&!q.gender&&!q.spouse&&!q.reincarnation&&q.minimumReputation===0&&
    (q.occupations.length===0||Array.from({length:10},(_,i)=>i).every(i=>q.occupations.includes(i)))&&
    !banned.test(q.name)&&flat(q).length<=20&&!flat(q).some(t=>/crepúsculo|dusk/i.test(t.name+' '+t.description))&&xp(q)>0;
  const score=q=>xp(q)/(1+flat(q).length/4+flat(q).reduce((n,t)=>n+t.monsters.reduce((m,x)=>m+x.count/18+x.itemCount/24,0),0));
  for(let level=61;level<=105;level++){
    if(level===89)continue;
    const categories=level<=90?[null]:[null,'GOD','EVIL'];
    for(const path of categories){
      const candidates=all.filter(q=>q.minLevel===level&&allowed(q)&&pathOf(q)===path&&!selected.has(q.id))
        .sort((a,b)=>score(b)-score(a)||a.id-b.id);
      const count=level<=90?2:path?2:1;
      candidates.slice(0,count).forEach(q=>add(q.id,'experience'));
    }
  }
  // Add prerequisite roots of selected XP chains, including lower-level roots
  // not already present in the existing 1–60 route. They are scheduled at the
  // current level rather than inserted retroactively into saved progress.
  const existing=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8')).missions;
  const existingTasks=new Set(existing.filter(m=>!m.id.startsWith('ENDGAME|')).flatMap(m=>m.steps.map(s=>s.memoryTaskId)).filter(Boolean));
  changed=true;
  while(changed){changed=false;for(const entry of [...selected.values()]){
    const q=roots.get(entry.id);
    for(const id of q.prerequisites.map(id=>owner.get(id)).filter(Boolean)){
      if(id===q.id||selected.has(id)||existingTasks.has(id))continue;
      const task=roots.get(id);if(!task||task.minLevel>105)continue;
      if(flat(task).some(t=>/crepúsculo|dusk/i.test(t.description+' '+t.name)))continue;
      add(id,entry.category,Math.max(task.minLevel,entry.scheduledLevel||q.minLevel));changed=true;
    }
  }}
  const kinds=JSON.parse(fs.readFileSync('work/guide-entities-by-kind.json','utf8'));
  const resourceRecords=JSON.parse(fs.readFileSync('work/resource-facts.json','utf8'));
  const coordBytes=fs.readFileSync('work/client-configs_coord_data.txt');
  const instanceText=fs.readFileSync('work/instance-utf8.txt','utf8').replace(/\r/g,'');
  const mapNames=Object.fromEntries([...instanceText.matchAll(/"([^"\r\n]+)"\s*\{\s*(\d+)[^\r\n]*\r?\n\s*"([^"\r\n]+)"/g)].map(m=>[m[3],m[1]]));
  assert(mapNames.a21&&mapNames.a22,'Nomes dos planos não extraídos');
  const coords=new Map();
  for(const line of coordBytes.toString('latin1').split(/\r?\n/)){
    const m=line.match(/^(\d+)\s+(\S+)\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)/);if(!m)continue;
    const id=+m[1],p={map:m[2],x:Math.round(+m[3]/10+400),y:Math.round(+m[5]/10+550)};
    if(!coords.has(id))coords.set(id,[]);
    if(!coords.get(id).some(v=>v.map===p.map&&v.x===p.x&&v.y===p.y))coords.get(id).push(p);
  }
  const name=(id,kind)=>clean(kinds[kind]?.[id])||`ID ${id}`;
  const entity=(id,kind)=>({name:name(id,kind),points:coords.get(id)||[]});
  const itemName=id=>{
    for(const kind of ['Quest Items','Quest Rewards','Quest Activators','Weapons','Armors','Accessories','Skill Scrolls','Remedies','Potions','Pet Food','Key Items','Materials','Gems','ATK Hierograms'])
      if(kinds[kind]?.[id])return clean(kinds[kind][id]);
    return `Item ${id}`;
  };
  const npcs=new Set(),monsters=new Set(),items=new Set(),rewards=new Set();
  const facts=[...selected.values()].map(entry=>{
    const q=roots.get(entry.id);
    for(const t of flat(q)){
      if(t.npc)npcs.add(t.npc);if(t.awardNpc)npcs.add(t.awardNpc);
      t.monsters.forEach(m=>{monsters.add(m.id);if(m.item)items.add(m.item);});
      t.items.forEach(i=>items.add(i.id));t.rewardItems.forEach(i=>rewards.add(i));
    }
    return {...entry,task:q,previous:[...previous.get(q.id)].filter(id=>selected.has(id))};
  });
  const resourceItems={};for(const r of resourceRecords)for(let i=1;i<=16;i++){
    const id=r[`materials_${i}_id`];if(items.has(id))(resourceItems[id]||=[]).push({id:r.ID,...entity(r.ID,'Resources')});
  }
  const source={source:'Cliente The Classic local: tasks.data v124; elements.data v145; configs/coord_data.txt. Seleção clássica de XP, cultivos e moedas até 105.',
    coordinateSha256:crypto.createHash('sha256').update(coordBytes).digest('hex'),
    coordinatePolicy:'Marcadores automáticos somente para pontos do mapa world. Mapas internos e planos de cultivo usam nome do mapa/NPC e instrução textual.',
    branchSelections:{11664:18810,1286:1304},mapNames,
    facts,npcs:Object.fromEntries([...npcs].map(id=>[id,entity(id,'NPCs')])),
    monsters:Object.fromEntries([...monsters].map(id=>[id,entity(id,'Monsters')])),
    items:Object.fromEntries([...items].map(id=>[id,itemName(id)])),rewards:Object.fromEntries([...rewards].map(id=>[id,itemName(id)])),resourceItems};
  fs.writeFileSync(factsPath,JSON.stringify(source,null,2)+'\n');
}

const source=JSON.parse(fs.readFileSync(factsPath,'utf8'));
const entries=new Map(source.facts.map(e=>[e.id,e]));
const catalog=new Map(fs.readFileSync('data/task_catalog_144.tsv','utf8').trim().split(/\r?\n/).slice(1).map(line=>{
  const p=line.split('\t');return[+p[0],{title:p[4],items:(p[17]||'').split(';').filter(i=>i.includes(':')).map(i=>+i.split(':')[0])}];
}));
const minMemo=new Map();
function minimum(e,seen=new Set()){
  if(e.task.minLevel)return e.task.minLevel;
  if(minMemo.has(e.id))return minMemo.get(e.id);
  if(seen.has(e.id))return periodLevel(e.task.period)||61;
  seen=new Set([...seen,e.id]);
  const n=Math.max(periodLevel(e.task.period),...e.previous.map(id=>minimum(entries.get(id),seen)),0)||Math.max(61,e.scheduledLevel||0);
  minMemo.set(e.id,n);return n;
}
function scheduled(e,seen=new Set()){
  if(seen.has(e.id))return Math.max(61,minimum(e));seen=new Set([...seen,e.id]);
  const forced=[3198,3199,3200,3222,3223,3224].includes(e.id)?99:0;
  return Math.max(61,minimum(e),e.scheduledLevel||0,forced,...e.previous.map(id=>scheduled(entries.get(id),seen)));
}
const mapName=m=>m==='world'?'mundo principal':source.mapNames[m]||`mapa ${m}`;
function closest(entity,near,currentMap='world'){
  const ps=(entity?.points||[]).filter(p=>p.map===currentMap);
  ps.sort((a,b)=>near?Math.hypot(a.x-near.x,a.y-near.y)-Math.hypot(b.x-near.x,b.y-near.y):0);
  return ps[0];
}
function describe(entity,near,currentMap){
  const p=closest(entity,near,currentMap)||entity?.points?.[0];
  return {text:`${entity?.name||'NPC indicado no questlog'}${p?` — ${mapName(p.map)}${p.map==='world'?` (${p.x}, ${p.y})`:''}`:''}`,point:p};
}
const missions=[];
for(const e of source.facts){
  const q=e.task,level=scheduled(e),steps=[];
  let currentMap='world',near=null;
  const add=(t,action,entity,itemId)=>{
    const p=closest(entity,near,currentMap)||entity?.points?.[0];
    const marker=p?.map==='world'?{markerX:p.x,markerY:p.y}:{};
    if(p){currentMap=p.map;near=p;}
    const bound=catalog.get(t.id);assert(bound&&bound.title===t.name,`Nome divergente: ${t.id}/${t.name}`);
    steps.push({id:`t${t.id}-${steps.filter(s=>s.memoryTaskId===t.id).length+1}`,title:t.name,action:clean(action),...marker,
      routeOrder:0,memoryTaskId:t.id,...(itemId&&bound.items.includes(itemId)?{memoryItemIds:[itemId]}:{})});
  };
  const giver=source.npcs[q.npc];
  add(q,giver?`Aceite a missão com ${describe(giver,near,currentMap).text}.`:
    'Receba a missão de continuação da cadeia atual; confira o nome no questlog.',giver);
  function visit(t){
    if(t.children.length){
      const branch=source.branchSelections[t.id];
      if(t.chooseOne){assert(branch,`Escolha de filhos não revisada: ${t.id}/${t.name}`);visit(t.children.find(c=>c.id===branch));}
      else t.children.forEach(visit);
      return;
    }
    if(t.method===4&&!t.description&&!t.rewardItems.length)return;
    const target=source.npcs[t.awardNpc];
    if(t.monsters.length){
      const beforeObjective=steps.length;
      for(const m of t.monsters.filter(m=>m.item&&m.itemCount)){
        const mob=source.monsters[m.id];add(t,`Colete ${m.itemCount} ${source.items[m.item]} de ${describe(mob,near,currentMap).text}.`,mob,
          t.monsters.filter(m=>m.item&&m.itemCount).length>1?m.item:undefined);
      }
      const kills=t.monsters.filter(m=>m.count>0);
      if(kills.length){
        const mob=source.monsters[kills[0].id];
        add(t,`Mate ${kills.map(m=>`${m.count} ${source.monsters[m.id]?.name||`monstro ${m.id}`}`).join(' e ')}.${kills.length>1?' Confira todos os contadores no jogo.':''}`,mob);
      }
      if(target)add(t,`Entregue a missão para ${describe(target,near,currentMap).text}.`,target);
      if(steps.length===beforeObjective)add(t,clean(t.description)||`Confira os objetivos de “${t.name}” no questlog e confirme após concluir no jogo.`);
    }else if(t.items.length){
      let gathered=false;
      for(const item of t.items){
        const resource=(source.resourceItems[item.id]||[]).find(r=>closest(r,near,currentMap));
        if(!resource)continue;
        add(t,`Colete ${item.count} ${source.items[item.id]} em ${describe(resource,near,currentMap).text}.`,resource,t.items.length>1?item.id:undefined);gathered=true;
      }
      if(target)add(t,`Entregue ${t.items.map(i=>`${i.count} ${source.items[i.id]}`).join(', ')} para ${describe(target,near,currentMap).text}.`,target);
      else if(!gathered)add(t,clean(t.description)||`Obtenha ${t.items.map(i=>`${i.count} ${source.items[i.id]}`).join(', ')} conforme o questlog.`);
    }else if(target){
      add(t,`${t.method===5?'Aguarde o prazo/diálogo e fale':'Fale'} com ${describe(target,near,currentMap).text}.`,target);
    }else{
      add(t,clean(t.description)||`Confirme a etapa “${t.name}” no questlog. Use Etapa feita após concluir a ação no jogo.`);
    }
  }
  visit(q);
  if(e.category==='coin'&&q.rewardItems.length&&steps.length>1)
    steps[steps.length-1].action='Confira no inventário as moedas recebidas. Guarde-as para ativar os pilares da DG com o grupo pronto.';
  const activeNodes=t=>[t,...(t.chooseOne?t.children.filter(c=>c.id===source.branchSelections[t.id]):t.children).flatMap(activeNodes)];
  const nodes=activeNodes(q),xp=nodes.reduce((n,t)=>n+t.xp,0);
  const rewardIds=[...new Set(nodes.flatMap(t=>t.rewardItems))];
  const instruction=e.category==='cultivation'?'Cultivo prioritário. Siga todas as submissões e combine os objetivos de DG com as moedas indicadas na própria rota.':
    e.category==='coin'?'Retire a moeda e ative o pilar com o grupo pronto. Combine os chefes com o cultivo da mesma DG; não troque o líder durante a missão.':
    e.category==='daily'?'Atividade diária/repetível para complementar a XP até 105. Faça quando disponível e reabra pelo menu Etapas nas próximas repetições.':
    e.category==='rank'?`Opcional: exige ${q.minimumReputation||'a reputação do rank anterior'} de fama e os ranks anteriores. Pule se não cumprir o requisito.`:
    'Pegue objetivos próximos antes de sair, complete-os com o grupo e concentre as entregas no retorno ao mesmo polo.';
  missions.push({id:`ENDGAME|${e.path||'ALL'}|${q.id}`,routeKey:'COMMON',order:0,classScope:'ALL',
    minLevel:minimum(e),expectedLevel:level,maxLevel:q.maxLevel?String(q.maxLevel):'Sem limite',name:q.name,
    npc:giver?describe(giver,null,'world').text:'Continuação / missão automática',
    location:giver?describe(giver,null,'world').text:clean(q.description).slice(0,180)||'Consulte a submissão atual',
    baseXp:xp,xp3:xp*3,priority:e.category==='cultivation'||e.category==='coin'?'Essencial':e.category==='rank'?'Opcional':'Alta',
    benefit:e.category==='cultivation'?'Progressão de cultivo e liberação de habilidades':rewardIds.length?rewardIds.map(id=>source.rewards[id]).join(', '):'XP de missão ×3',
    instruction:instruction+(q.description?' '+clean(q.description):'')+
      (q.minLevel>0&&q.minLevel<=60?' Revisão de pré-requisito anterior: se já concluiu no jogo, confirme pelo histórico/sincronização ou pule esta revisão.':''),
    ...(e.path?{cultivationPath:e.path}:{}),
    ...(q.frequency?{repeatable:true}:{}),
    ...(q.minLevel===0?{availabilityNote:'Mínimo derivado dos pré-requisitos/cultivo; a tarefa não tem mínimo próprio explícito no cliente.'}:{}),steps});
}

const data=JSON.parse(fs.readFileSync('data/quest_routes.json','utf8'));
const legacy=data.missions.filter(m=>!m.id.startsWith('ENDGAME|'));
const choice={id:'ENDGAME|ALL|CULTIVATION-CHOICE',routeKey:'COMMON',classScope:'ALL',order:0,minLevel:89,expectedLevel:89,maxLevel:'Sem limite',
  name:'Escolher cultivo God ou Evil',npc:'Escolha no overlay',location:'Cidade Universal — decisão de cultivo',baseXp:null,xp3:null,priority:'Essencial',
  benefit:'Seleciona a cadeia específica deste personagem',instruction:'Escolha God ou Evil. Faça a mesma escolha com o NPC no jogo. A preferência é independente por personagem.',cultivationChoice:true,
  steps:[{id:'choice',title:'God ou Evil?',action:'Escolha God (Celestial) ou Evil (Demoníaco) para continuar o guia deste personagem.',routeOrder:0}]};
missions.push(choice);
missions.push({id:'ENDGAME|ALL|PAST-ACCESS',routeKey:'COMMON',classScope:'ALL',order:0,minLevel:100,expectedLevel:100,maxLevel:'Sem limite',
  name:'Acessos do Passado para o cultivo 100',npc:'Conferência dos acessos ao Passado',location:'Floresta Kylin / Dor Celestial / Vale da Cicatriz',baseXp:null,xp3:null,priority:'Pré-requisito',
  benefit:'Confere acesso às áreas usadas pelos cultivos God/Evil 100',
  instruction:'O cultivo 100 exige áreas do Passado. A liberação inclui conteúdo de Dusk/Crepúsculo, mantido fora deste guia por solicitação. Se os acessos ainda estiverem pendentes, adie o cultivo 100 pelo menu Etapas e continue o circuito de XP elegível.',
  steps:[{id:'past-access',title:'Conferir acessos do Passado',action:'Antes do cultivo 100, confira acesso à Floresta Kylin, Dor Celestial e Vale da Cicatriz. Se estiver pendente, adie o cultivo pelo menu Etapas.',routeOrder:0}]});
missions.push({id:'ENDGAME|ALL|LEVEL105',routeKey:'COMMON',classScope:'ALL',order:0,minLevel:105,expectedLevel:105,maxLevel:'Sem limite',
  name:'Chegada ao nível 105',npc:'Conferência manual',location:'Personagem no nível 105',baseXp:null,xp3:null,priority:'Marco',
  benefit:'Encerra o guia 1–105',instruction:'Confirme somente depois de atingir 105 no jogo. Se faltar XP, repita as atividades elegíveis da faixa 100–105 e retome o guia. Não há leitura/exibição do nível real no painel.',
  steps:[{id:'level105',title:'Nível 105 alcançado',action:'Confira o nível no jogo e marque esta etapa quando o personagem atingir 105. Se faltar XP, repita o circuito de atividades elegíveis da faixa 100–105.',routeOrder:0}]});
const entryFor=m=>entries.get(+m.id.split('|')[2]);
const pending=missions.slice().sort((a,b)=>a.expectedLevel-b.expectedLevel||
  (a.id==='ENDGAME|ALL|LEVEL105'?1:b.id==='ENDGAME|ALL|LEVEL105'?-1:0)||(a.cultivationChoice?-1:b.cultivationChoice?1:0)||
  (a.cultivationPath||'').localeCompare(b.cultivationPath||'')||a.minLevel-b.minLevel||+a.id.split('|')[2]-+b.id.split('|')[2]);
const ordered=[];
while(pending.length){
  const index=pending.findIndex(m=>!entryFor(m)||entryFor(m).previous.every(id=>!pending.some(p=>entryFor(p)?.id===id)));
  assert(index>=0,'Ciclo na cadeia de pré-requisitos');ordered.push(pending.splice(index,1)[0]);
}
// Reserve an independent range per level; no saved 1–60 stage is renumbered.
const counts=new Map();for(const m of ordered){
  const level=m.expectedLevel;let index=counts.get(level)||0;
  m.order=20000+(level-61)*1000+index;
  for(const s of m.steps){s.routeOrder=1300000+(level-61)*10000+index*10;index++;}
  assert(index<1000,`Faixa de ordens esgotada no nível ${level}`);counts.set(level,index+1);
}
const taskIndex=new Map(source.facts.flatMap(e=>flat(e.task).map(t=>[t.id,t])));
const sequence=ordered.flatMap(m=>m.steps).sort((a,b)=>a.routeOrder-b.routeOrder);
const orderSlots=sequence.map(s=>s.routeOrder);
const mission=id=>ordered.find(m=>entryFor(m)?.id===id);
const before=(stage,anchor)=>{sequence.splice(sequence.indexOf(stage),1);sequence.splice(sequence.indexOf(anchor),0,stage);};
const after=(stage,anchor)=>{sequence.splice(sequence.indexOf(stage),1);sequence.splice(sequence.indexOf(anchor)+1,0,stage);};
const combinedDungeons=[];
for(const plan of [
  {cult:2122,coin:8746,leaders:[7973,7969,7971]},
  {cult:2153,coin:8762,leaders:[8437,8439,8441,7977]},
  {cult:2804,coin:8778,leaders:[7985,7987,7989]},
  {cult:2818,coin:8779,leaders:[7979,7981,7983]},
  {cult:3227,coin:8780,leaders:[7991,7993,7995,7997]},
  {cult:3204,coin:8781,leaders:[7999,8001,8003,8005]}
]){
  const cultivation=mission(plan.cult),acquire=mission(plan.coin);assert(cultivation&&acquire);
  let anchor=cultivation.steps[0];for(const s of acquire.steps){after(s,anchor);anchor=s;}
  let matches=0,additional=0;
  for(const id of plan.leaders){
    const leader=mission(id);assert(leader);
    const targets=new Set(entries.get(id).task.monsters.map(m=>m.id));
    const objective=cultivation.steps.find(s=>/^(?:Mate|Colete)\b/.test(s.action)&&taskIndex.get(s.memoryTaskId)?.monsters.some(m=>targets.has(m.id)));
    if(objective){
      before(leader.steps[0],objective);let previous=objective;
      for(const s of leader.steps.slice(1)){after(s,previous);previous=s;}matches++;
    }else{
      const maps=new Set([...targets].flatMap(id=>(source.monsters[id]?.points||[]).map(p=>p.map)));
      const entrance=cultivation.steps.find(s=>taskIndex.get(s.memoryTaskId)?.monsters.some(m=>(source.monsters[m.id]?.points||[]).some(p=>maps.has(p.map))));
      assert(entrance,`Entrada de DG não identificada para a moeda ${id}`);
      for(const s of leader.steps)before(s,entrance);additional++;
    }
  }
  combinedDungeons.push({cultivation:plan.cult,coin:plan.coin,sharedBossObjectives:matches,additionalCoinBosses:additional});
}
const regionalBatches=[];
const distance=(a,b)=>Math.hypot(a.markerX-b.markerX,a.markerY-b.markerY);
const routeDistance=stages=>stages.slice(1).reduce((n,s,i)=>n+distance(stages[i],s),0);
const groups=new Map();
for(const m of ordered){
  const e=entryFor(m);if(!e||e.category!=='experience')continue;
  if(flat(e.task).some(t=>t.timeLimit||/minutos|segundos|teleport|cronômetro|tempo limitado/i.test(t.description)))continue;
  if(!m.steps.every(s=>s.markerX!=null&&s.markerY!=null))continue;
  const key=`${m.expectedLevel}/${m.cultivationPath||'ALL'}`;
  if(!groups.has(key))groups.set(key,[]);groups.get(key).push(m);
}
for(const [key,group] of groups){
  if(group.length<2)continue;
  const ids=new Set(group.map(m=>entryFor(m).id));
  if(group.some(m=>entryFor(m).previous.some(id=>ids.has(id))))continue;
  if(group.some(m=>distance(group[0].steps[0],m.steps[0])>20))continue;
  const stages=new Set(group.flatMap(m=>m.steps));
  const indices=sequence.map((s,i)=>stages.has(s)?i:-1).filter(i=>i>=0);
  if(indices.at(-1)-indices[0]+1!==indices.length)continue;
  const original=sequence.slice(indices[0],indices.at(-1)+1);
  const candidate=group.map(m=>m.steps[0]),next=new Map(group.map(m=>[m,1]));
  while(candidate.length<original.length){
    const available=group.filter(m=>next.get(m)<m.steps.length).map(m=>({m,s:m.steps[next.get(m)]}));
    available.sort((a,b)=>distance(candidate.at(-1),a.s)-distance(candidate.at(-1),b.s));
    const chosen=available[0];candidate.push(chosen.s);next.set(chosen.m,next.get(chosen.m)+1);
  }
  if(routeDistance(candidate)>=routeDistance(original))continue;
  sequence.splice(indices[0],indices.length,...candidate);
  regionalBatches.push({group:key,missions:group.length,before:Math.round(routeDistance(original)),after:Math.round(routeDistance(candidate))});
}
sequence.forEach((s,i)=>s.routeOrder=orderSlots[i]);
// Group same-NPC pickups and respect quest/branch dependencies, including coins
// required in the middle of a cultivation chain. More route tuning is explicit
// rather than a nearest-neighbor pass across teleports or different planes.
const report={addedMissions:missions.length,addedStages:missions.reduce((n,m)=>n+m.steps.length,0),
  god:missions.filter(m=>m.cultivationPath==='GOD').length,evil:missions.filter(m=>m.cultivationPath==='EVIL').length,
  shared:missions.filter(m=>!m.cultivationPath).length,levels:[...counts.keys()].sort((a,b)=>a-b)};
report.combinedDungeons=combinedDungeons;
report.regionalBatches=regionalBatches;
console.log(JSON.stringify(report,null,2));
data.schemaVersion=Math.max(8,data.schemaVersion);data.generatedFrom='Guia 1–105, dez classes, XP ×3, cultivos God/Evil por personagem; fontes dataminadas do cliente.';
data.missions=legacy.concat(ordered);
if(process.argv.includes('--write'))fs.writeFileSync('data/quest_routes.json',JSON.stringify(data,null,2)+'\n');
