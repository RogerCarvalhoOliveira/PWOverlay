import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const filename=path.join(root,'data/rota_leveling_pw_144.xlsx');
const routes=JSON.parse(await fs.readFile(path.join(root,'data/quest_routes.json'),'utf8'));
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(filename));
const sheets=[['Humanos - WR','WR'],['Selvagens - WB-WF','WBWF'],['Alados - EA-EP','EAEP'],['Abissais - MC','MC']];
const snapshots=[];
const selectedRoute=process.argv.find(arg=>arg.startsWith('--route='))?.split('=')[1];
const previewRange=process.argv.find(arg=>arg.startsWith('--preview-range='))?.split('=')[1]||'M8:N12';
for(const [name,route] of sheets){
  if(selectedRoute&&route!==selectedRoute)continue;
  const sheet=wb.worksheets.getItem(name);
  const missions=routes.missions.filter(m=>m.routeKey===route||m.routeKey==='COMMON').sort((a,b)=>a.order-b.order);
  const last=7+missions.length;
  snapshots.push({sheet,values:sheet.getRange(`A8:O${last}`).values,formulas:sheet.getRange(`A8:O${last}`).formulas,last});
  const before=await wb.render({sheetName:name,range:previewRange,scale:1,format:'png'});
  await fs.writeFile(path.join(root,`work/route-before-${route}.png`),new Uint8Array(await before.arrayBuffer()));
  for(let i=0;i<missions.length;i++){
    const mission=missions[i];
    if(sheet.getRange(`F${i+8}`).values[0][0]!==mission.name)throw new Error(`Linha divergente ${name}/${i+8}`);
  }
  if(!process.argv.includes('--write'))continue;
  sheet.getRange(`N8:N${last}`).values=missions.map(m=>[m.steps.map((s,i)=>`R${s.routeOrder} · ${i+1}. ${s.title} — ${s.action}`).join('\n')]);
}
if(!process.argv.includes('--write'))process.exit(0);
wb.recalculate();
for(const {sheet,values,formulas,last} of snapshots){
  const after=sheet.getRange(`A8:O${last}`).values;
  const afterFormulas=sheet.getRange(`A8:O${last}`).formulas;
  for(let r=0;r<values.length;r++)for(let c=0;c<values[r].length;c++){
    if(c!==13&&!formulas[r][c]&&JSON.stringify(values[r][c])!==JSON.stringify(after[r][c]))throw new Error(`Valor alterado ${sheet.name}/${r}/${c}`);
    if(formulas[r][c]!==afterFormulas[r][c])throw new Error('Fórmula alterada');
  }
  const preview=await wb.render({sheetName:sheet.name,range:previewRange,scale:1,format:'png'});
  await fs.writeFile(path.join(root,`work/route-after-${sheet.name.replace(/[^A-Za-z]/g,'')}.png`),new Uint8Array(await preview.arrayBuffer()));
}
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!',options:{useRegex:true,maxResults:20},maxChars:2000})).ndjson);
await (await SpreadsheetFile.exportXlsx(wb)).save(filename);
console.log('Sincronizadas quatro abas, preservados outros valores e fórmulas.');
