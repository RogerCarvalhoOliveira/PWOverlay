import fs from 'node:fs';
import assert from 'node:assert/strict';

// Preserve level-unlock boundaries, quest dependencies and unknown/instance travel.
const file = 'data/quest_routes.json';
const data = JSON.parse(fs.readFileSync(file, 'utf8'));
const key = n => `${n.m.id}/${n.s.id}`;
const coordinate = n => {
  if (/caverna|dentro da|dentro do|DG\d|instância|teleport|minutos|pop-up|questlog/i.test(n.s.action)) return null;
  const match = n.s.action.match(/\((\d{3})\s*[, ]\s*(\d{3})/);
  return match ? [Number(match[1]), Number(match[2])] : null;
};
const distance = (a,b) => Math.hypot(a[0]-b[0], a[1]-b[1]);
const length = nodes => nodes.slice(1).reduce((sum,n,i) => sum + distance(coordinate(nodes[i]),coordinate(n)),0);
const original = new Map(data.missions.flatMap(m => m.steps.map(s => [`${m.id}/${s.id}`,s.routeOrder])));
const report = [];
for (const routeKey of ['WR','WBWF','EAEP','MC','COMMON']) {
  const nodes = data.missions.filter(m=>m.routeKey===routeKey)
    .flatMap(m=>m.steps.map((s,i)=>({m,s,i}))).sort((a,b)=>a.s.routeOrder-b.s.routeOrder);
  let improvements=0, before=0, after=0;
  // Local windows never cross a new level gate, unknown location, or special chain.
  let highLevel=0, block=[];
  const flush = () => {
    if(block.length<4){block=[];return;}
    const slots=block.map(n=>n.s.routeOrder);
    const old=length(block); before+=old;
    let best=block.slice(), cost=old, changed=true;
    const valid = a => {
      const positions=new Map(a.map((n,i)=>[key(n),i]));
      return a.every(n=>n.i===0 || !positions.has(`${n.m.id}/${n.m.steps[n.i-1].id}`) ||
        positions.get(`${n.m.id}/${n.m.steps[n.i-1].id}`)<positions.get(key(n)));
    };
    while(changed){
      changed=false;
      // Keep both boundary actions fixed, so neighboring legs stay unchanged.
      for(let from=1;from<best.length-1;from++) for(let to=1;to<best.length-1;to++){
        if(from===to)continue;
        const candidate=best.slice();candidate.splice(to,0,candidate.splice(from,1)[0]);
        if(!valid(candidate))continue;
        const next=length(candidate);
        if(next<cost-0.01){best=candidate;cost=next;changed=true;improvements++;}
      }
    }
    best.forEach((n,i)=>{n.s.routeOrder=slots[i];});after+=cost;block=[];
  };
  for(const n of nodes){
    const level=n.m.expectedLevel||n.m.minLevel;
    const special=/cultivo|DG\d|moeda|pilar|habilidades|salto|primári|primeiro v[oô]o|voo do questlog/i.test(n.m.id+' '+n.m.name);
    if(level>highLevel){flush();highLevel=level;}
    if(special||!coordinate(n)){flush();continue;}
    block.push(n);
  }
  flush();report.push({routeKey,improvements,localDistanceBefore:Math.round(before),localDistanceAfter:Math.round(after)});
}

const common=data.missions.filter(m=>m.routeKey==='COMMON');
let sequence=common.flatMap(m=>m.steps.map(s=>({m,s}))).sort((a,b)=>a.s.routeOrder-b.s.routeOrder);
const find=(fragment,step)=>{
  const m=common.find(m=>m.id.includes(fragment));assert(m,fragment);
  const s=m.steps.find(s=>s.id===step);assert(s,step);return sequence.find(n=>n.s===s);
};
const moveAfter=(node,anchor)=>{sequence.splice(sequence.indexOf(node),1);sequence.splice(sequence.indexOf(anchor)+1,0,node);};
// Verify coins before entering, complete cultivation and coin objectives on one run.
moveAfter(find('DG29','e2'),find('DG29','e1'));
moveAfter(find('DG29','e3'),find('Cultivo 29','e3'));
moveAfter(find('DG29','e4'),find('Cultivo 29','e4'));
moveAfter(find('DG39','e2'),find('DG39','e1'));
moveAfter(find('DG39','e3'),find('Cultivo 39','e4'));
moveAfter(find('DG39','e4'),find('DG39','e3'));
moveAfter(find('DG59','e2'),find('Cultivo 59','e2'));
moveAfter(find('DG59','e3'),find('Cultivo 59','e4'));
// Remove the flight popup at the first level-30 city visit, not near level 39.
let anchor=sequence.find(n=>n.m.minLevel===30 && n.m.name==='Cargo Militar de Honra' && n.s.id==='e1');
assert(anchor,'Visita à cidade no nível 30');
for(const step of ['e1','e2','e3']){
  const node=find('Remover missão de voo',step);moveAfter(node,anchor);anchor=node;
}
const commonSlots=sequence.map(n=>original.get(key(n))).sort((a,b)=>a-b);
const oldCommon=sequence.slice().sort((a,b)=>original.get(key(a))-original.get(key(b)));
const racialAnchors=data.missions.filter(m=>m.routeKey!=='COMMON'&&m.minLevel>=20)
  .map(m=>({m,anchor:oldCommon.filter(n=>original.get(key(n))<m.steps[0].routeOrder).pop()}));
sequence.forEach((n,i)=>{n.s.routeOrder=commonSlots[i];});
const offsets=new Map();
for(const {m,anchor} of racialAnchors){
  assert(anchor,m.id);let offset=offsets.get(anchor.s.routeOrder)||1;
  for(const s of m.steps)s.routeOrder=anchor.s.routeOrder+offset++;
  assert(offset<100,'Espaço para desvios raciais');offsets.set(anchor.s.routeOrder,offset);
}
for(const m of data.missions){
  for(let i=1;i<m.steps.length;i++)assert(m.steps[i].routeOrder>m.steps[i-1].routeOrder,m.id);
}
const changed=data.missions.flatMap(m=>m.steps.map(s=>({m,s})))
  .filter(n=>original.get(key(n))!==n.s.routeOrder).length;
console.log(JSON.stringify({changedStages:changed,localWindows:report},null,2));
if(process.argv.includes('--write'))fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');
