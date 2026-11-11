import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {FileBlob,SpreadsheetFile} from '@oai/artifact-tool';
const routes=JSON.parse(await fs.readFile('data/quest_routes.json','utf8'));
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load('data/rota_leveling_pw_144.xlsx'));
wb.recalculate();
for(const [name,key] of [['Humanos - WR','WR'],['Selvagens - WB-WF','WBWF'],['Alados - EA-EP','EAEP'],['Abissais - MC','MC'],['Guardiões - SK-MS','SKMS']]){
  const sheet=wb.worksheets.getItem(name),missions=routes.missions.filter(m=>[key,'COMMON'].includes(m.routeKey)).sort((a,b)=>a.order-b.order);
  const values=sheet.getRange(`A8:P${7+missions.length}`).values;
  const formulas=sheet.getRange(`J8:J${7+missions.length}`).formulas;
  for(let i=0;i<missions.length;i++){
    const m=missions[i],row=values[i];
    const expected=[m.order,m.classScope,m.minLevel,m.maxLevel,m.name,m.npc,m.location,m.baseXp];
    assert.deepEqual(row.slice(1,9),expected,`${name}/${i+8}: dados`);
    assert.equal(row[14],m.expectedLevel,`${name}/${i+8}: nível previsto`);
    assert.equal(row[12],m.instruction,`${name}/${i+8}: instrução`);
    assert.equal(row[15],m.cultivationPath==='GOD'?'God':m.cultivationPath==='EVIL'?'Evil':'Ambos',`${name}/${i+8}: cultivo`);
    assert.equal(row[13],m.aliasOf?'Siga a entrada Cultivo 59 no trecho da DG59.':
      m.steps.map((s,j)=>`R${s.routeOrder} · ${j+1}. ${s.title} — ${s.action}`).join('\n'));
    if(m.baseXp>0)assert.equal(row[9],m.baseXp*3,`${name}/${i+8}: XP`);
    else assert(row[9]===null||row[9]===''||row[9]===0);
    assert(formulas[i][0].includes(`I${i+8}`));
    assert(['Sim','Não'].includes(row[0]));
  }
  console.log(`${name}: ${missions.length} linhas, etapas, fórmulas e níveis conferidos após reabrir.`);
}
