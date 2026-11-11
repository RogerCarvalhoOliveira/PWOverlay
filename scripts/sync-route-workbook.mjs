import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const filename=path.join(root,'data/rota_leveling_pw_144.xlsx');
const routes=JSON.parse(await fs.readFile(path.join(root,'data/quest_routes.json'),'utf8'));
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(filename));
const sheets=[['Humanos - WR','WR'],['Selvagens - WB-WF','WBWF'],['Alados - EA-EP','EAEP'],['Abissais - MC','MC'],['Guardiões - SK-MS','SKMS']];
const expand=process.argv.includes('--expand-classes');
const extend=process.argv.includes('--extend-105');
const audit=process.argv.includes('--sync-audit-fields');
const levelCap=Math.max(...routes.missions.map(m=>m.expectedLevel));
const titles={WR:'Humanos — WR / Guerreiro e MG / Mago',WBWF:'Selvagens — WB / Bárbaro e WF / Feiticeira',EAEP:'Alados — EA / Arqueiro e EP / Sacerdote',MC:'Abissais — MC / Mercenário e ES / Espiritualista',SKMS:'Guardiões — SK / Arcano e MS / Místico'};
const pathLabel=m=>m.cultivationPath==='GOD'?'God':m.cultivationPath==='EVIL'?'Evil':'Ambos';
const snapshots=[];
const selectedRoute=process.argv.find(arg=>arg.startsWith('--route='))?.split('=')[1];
const previewRange=process.argv.find(arg=>arg.startsWith('--preview-range='))?.split('=')[1]||'M8:N12';
for(const [name,route] of sheets){
  if(selectedRoute&&route!==selectedRoute)continue;
  let sheet;
  let created=false;
  try{sheet=wb.worksheets.getItem(name);}catch(error){
    if(route!=='SKMS'||!expand)continue;
    if(!process.argv.includes('--write'))continue;
    sheet=wb.worksheets.add(name);created=true;
    const template=wb.worksheets.getItem('Humanos - WR');
    sheet.getRange('A1:P7').copyFrom(template.getRange('A1:P7'),'all');
    for(const range of ['A2:M2','A4:D4','E4:G4','H4:M4','A5:M5'])sheet.getRange(range).merge();
    for(const [i,width] of [9,8,10,8,12,33,30,30,11,12,13,34,48,58,14,58].entries())
      sheet.getRangeByIndexes(0,i,240,1).format.columnWidth=width;
    sheet.getRange('A7:O7').format.fill='#176B55';
    sheet.getRange('A2:M2').format.fill='#176B55';
    sheet.getRange('A2:M2').format.font={color:'#FFFFFF',bold:true,size:16};
    sheet.getRange('A2:M2').format.rowHeight=32;
    sheet.getRange('A5:M5').format.rowHeight=30;
    sheet.tabColor='#176B55';sheet.showGridLines=false;
    sheet.freezePanes.freezeRows(7);
  }
  const missions=routes.missions.filter(m=>m.routeKey===route||m.routeKey==='COMMON').sort((a,b)=>a.order-b.order);
  const last=7+missions.length;
  const rebuild=created||(expand&&route==='SKMS')||extend;
  const stateKey=row=>[row[2],row[3],row[5],row[6],row[15]||'Ambos'].join('|');
  const previousLast=created?7:sheet.getUsedRange(true).rowCount;
  const previousRows=rebuild&&!created?sheet.getRange(`A8:P${previousLast}`).values:[];
  const previousFormulas=rebuild&&!created?sheet.getRange(`A8:O${previousLast}`).formulas:[];
  const statuses=new Map(previousRows.map(row=>[stateKey(row),row[0]]));
  if(!created){
    if(!rebuild)snapshots.push({sheet,values:sheet.getRange(`A8:O${last}`).values,formulas:sheet.getRange(`A8:O${last}`).formulas,last});
    const before=await wb.render({sheetName:name,range:previewRange,scale:1,format:'png'});
    await fs.writeFile(path.join(root,`work/route-before-${route}.png`),new Uint8Array(await before.arrayBuffer()));
  }
  for(let i=0;!rebuild&&i<missions.length;i++){
    const mission=missions[i];
    const existing=sheet.getRange(`B${i+8}:F${i+8}`).values[0];
    if(audit ? existing[0]!==mission.order || existing[1]!==mission.classScope : existing[4]!==mission.name)
      throw new Error(`Linha divergente ${name}/${i+8}`);
  }
  if(!process.argv.includes('--write'))continue;
  if(rebuild){
    const template=wb.worksheets.getItem('Humanos - WR');
    if(!created)sheet.getRange(`A8:P${previousLast}`).clear({applyTo:'contents'});
    for(let row=8;row<=last;row++)if(!extend||row>previousLast)sheet.getRange(`A${row}:O${row}`).copyFrom(template.getRange('A8:O8'),'all');
    sheet.getRange(`A8:O${last}`).values=missions.map(m=>[
      statuses.get([m.classScope,m.minLevel,m.name,m.npc,pathLabel(m)].join('|'))||'Não',m.order,m.classScope,m.minLevel,m.maxLevel,m.name,m.npc,m.location,m.baseXp,null,m.priority,m.benefit,m.instruction,'',m.expectedLevel]);
    sheet.getRange(`J8:J${last}`).formulas=missions.map((m,i)=>[`=IF(I${i+8}="","",I${i+8}*3)`]);
    sheet.getRange(`A8:A${last}`).dataValidation={rule:{type:'list',values:['Não','Sim']}};
    sheet.getRange(`M8:N${last}`).format.wrapText=true;
    sheet.tables.deleteAll();
    sheet.tables.add(`A7:${extend?'P':'O'}${last}`,true,`Route${route}`).style='TableStyleMedium2';
    sheet.getRange('A7:O7').format.rowHeight=32;
    sheet.getRange('A4').values=[[`Após a capital, siga a rota comum até ${levelCap}.`]];
    sheet.getRange('H4').values=[[`Destino: cultivo racial + Cidade do Dragão + rota comum até ${levelCap}`]];
    sheet.getRange('A5').values=[[extend?'Filtre Cultivo por Ambos + God ou Evil. Na coluna Classe, mantenha as missões compartilhadas e as da sua classe. Feita?: Não/Sim.':'Filtre Classe por SK/MS + sua sigla. Previsão usa XP de quests ×3; XP de monstros pode elevar o nível. Feita?: Não/Sim.']];
    if(created){
      sheet.getRange('P2').values=[['Fonte Guardiões: tasks.data v124, elements.data v145, configs/coord_data.txt do cliente local.']];
      sheet.getRange('P3').values=[['Coordenadas de monstros e recursos indicam um ponto de ocorrência; use os nomes de submissão para conferir no jogo.']];
      sheet.getRange('P4').values=[['Aceite A Reunião antes da recompensa de Visitando o Ancião e Memórias antes de entregar O Guardião do Templo.']];
      sheet.getRange('P2:P4').format.wrapText=true;
    }
  }
  if((expand||extend)&&titles[route])sheet.getRange('A2').values=[[`Rota de leveling 1–${levelCap} · ${titles[route]}`]];
  if(extend){
    const notes=sheet.getRange('P2:P6').values;
    if(notes.some(row=>row[0])){
      if(sheet.getRange('Q2:Q6').values.every(row=>!row[0]))sheet.getRange('Q2:Q6').values=notes;
      sheet.getRange('P2:P6').clear({applyTo:'contents'});
    }
    sheet.getRange('Q1:Q6').format.columnWidth=58;sheet.getRange('Q2:Q6').format.wrapText=true;
    sheet.getRange('P7').copyFrom(sheet.getRange('D7'),'all');sheet.getRange('P7').values=[['Cultivo']];
    sheet.getRange(`P8:P${last}`).values=missions.map(m=>[pathLabel(m)]);
    sheet.getRange(`P8:P${last}`).format.verticalAlignment='top';
    sheet.getRange(`P1:P${last}`).format.columnWidth=12;
    sheet.getRange(`N1:N${last}`).format.columnWidth=90;
    sheet.getRange('G7').values=[['NPC inicial']];
  }
  if(expand&&!created){
    sheet.getRange(`C8:C${last}`).values=missions.map(m=>[m.classScope]);
    sheet.getRange(`L8:M${last}`).values=missions.map(m=>[m.benefit,m.instruction]);
  }
  if(audit){
    sheet.getRange(`D8:H${last}`).values=missions.map(m=>[m.minLevel,m.maxLevel,m.name,m.npc,m.location]);
    sheet.getRange(`M8:M${last}`).values=missions.map(m=>[m.instruction]);
    sheet.getRange(`O8:O${last}`).values=missions.map(m=>[m.expectedLevel]);
  }
  const stageTexts=missions.map(m=>[m.aliasOf
    ?'Siga a entrada Cultivo 59 no trecho da DG59.'
    :m.steps.map((s,i)=>`R${s.routeOrder} · ${i+1}. ${s.title} — ${s.action}`).join('\n')]);
  if(audit&&!rebuild){
    const previous=sheet.getRange(`N8:N${last}`).values;
    for(let i=0;i<stageTexts.length;i++){
      const lines=stageTexts[i][0].split('\n');
      if(lines.length<=String(previous[i][0]||'').split('\n').length)continue;
      const row=sheet.getRange(`A${i+8}:P${i+8}`);
      const needed=lines.reduce((total,line)=>total+Math.ceil(line.length/88),0)*14+14;
      row.format.rowHeight=Math.min(409,Math.max(Number(row.format.rowHeight)||0,needed));
    }
  }
  sheet.getRange(`N8:N${last}`).values=stageTexts;
  if(rebuild){
    // Bound each new row to Excel's maximum while allowing its full stage text.
    for(let i=0;i<missions.length;i++){
      if(extend&&!missions[i].id.startsWith('ENDGAME|'))continue;
      const lines=Math.max(missions[i].steps.reduce((n,s)=>n+Math.ceil((s.title.length+s.action.length+30)/(extend?125:62)),0),Math.ceil(missions[i].instruction.length/68),Math.ceil(missions[i].benefit.length/44),Math.ceil(missions[i].location.length/42),Math.ceil(missions[i].npc.length/42));
      if(extend){
        sheet.getRange(`L${i+8}:N${i+8}`).format.font={size:9};
        sheet.getRange(`G${i+8}:H${i+8}`).format.font={size:9};
        sheet.getRange(`G${i+8}:H${i+8}`).format.wrapText=true;
      }
      sheet.getRange(`A${i+8}:P${i+8}`).format.rowHeight=Math.min(409,Math.max(66,lines*12+16));
    }
  }
  if(extend&&!created){
    const after=sheet.getRange(`A8:O${last}`).values,formulas=sheet.getRange(`A8:O${last}`).formulas;
    for(let r=0;r<previousRows.length;r++){
      if(!previousRows[r][5]||missions[r]?.id.startsWith('ENDGAME|'))continue;
      for(let c=0;c<15;c++){
        if(previousFormulas[r][c]){if(previousFormulas[r][c]!==formulas[r][c])throw new Error(`Fórmula legada alterada ${name}/${r+8}`);}
        else if(JSON.stringify(previousRows[r][c])!==JSON.stringify(after[r][c]))throw new Error(`Valor legado alterado ${name}/${r+8}/${c}`);
      }
    }
  }
  if((extend||rebuild)&&process.argv.includes('--write')){
    const preview=await wb.render({sheetName:name,range:previewRange,scale:1,format:'png'});
    await fs.writeFile(path.join(root,`work/route-105-${route}.png`),new Uint8Array(await preview.arrayBuffer()));
  }
}
if(!process.argv.includes('--write'))process.exit(0);
wb.recalculate();
for(const {sheet,values,formulas,last} of snapshots){
  const after=sheet.getRange(`A8:O${last}`).values;
  const afterFormulas=sheet.getRange(`A8:O${last}`).formulas;
  for(let r=0;r<values.length;r++)for(let c=0;c<values[r].length;c++){
    if(![13,...(expand?[2,11,12]:[]),...(audit?[3,4,5,6,7,12,14]:[])].includes(c)&&!formulas[r][c]&&JSON.stringify(values[r][c])!==JSON.stringify(after[r][c]))throw new Error(`Valor alterado ${sheet.name}/${r}/${c}`);
    if(formulas[r][c]!==afterFormulas[r][c])throw new Error('Fórmula alterada');
  }
  const preview=await wb.render({sheetName:sheet.name,range:previewRange,scale:1,format:'png'});
  await fs.writeFile(path.join(root,`work/route-after-${sheet.name.replace(/[^A-Za-z]/g,'')}.png`),new Uint8Array(await preview.arrayBuffer()));
}
if(expand){
  const preview=await wb.render({sheetName:'Guardiões - SK-MS',range:previewRange,scale:1,format:'png'});
  await fs.writeFile(path.join(root,'work/route-after-SKMS.png'),new Uint8Array(await preview.arrayBuffer()));
}
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!',options:{useRegex:true,maxResults:20},maxChars:2000})).ndjson);
await (await SpreadsheetFile.exportXlsx(wb)).save(filename);
console.log('Abas sincronizadas; valores e fórmulas fora do escopo preservados.');
