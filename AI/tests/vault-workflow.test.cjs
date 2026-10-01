
const fs=require("node:fs"),path=require("node:path"),assert=require("node:assert/strict"),vm=require("node:vm");
const root=process.cwd(),vault=path.join(root,"ObsidianVault");
const helper=fs.readFileSync(path.join(vault,"90 Служебное/Согласование.js"),"utf8");
const AsyncFunction=Object.getPrototypeOf(async function(){}).constructor;
const definitions=helper.replace(/await runReview\(app, obsidian, context, component, container\);\s*$/,"return {canonical,digest,runReview};");
(async()=>{
const {canonical,digest,runReview}=await new AsyncFunction("crypto",definitions)(globalThis.crypto);
const base=JSON.stringify({id:"test",review_status:"не проверено",reviewed_at:null,decision_status:"идея",other:"keep"});
let text="---\n"+base+"\n---\n\nПроверяемая мысль. ^b-001\n",writes=0,notices=[],listeners=[];
const file={path:"02 Корректура/test.md",extension:"md"};
let race=false;
const app={vault:{read:async f=>{assert.equal(f,file);return text},process:async(f,fn)=>{assert.equal(f,file);if(race)text+="Чужая правка\n";text=fn(text);writes++},on:(event,fn)=>{listeners.push(fn);return fn}}};
const obsidian={parseYaml:JSON.parse,stringifyYaml:x=>JSON.stringify(x),Notice:class{constructor(s){notices.push(s)}}};
const meta=()=>JSON.parse(text.match(/^---\n([\s\S]*?)\n---/)[1]);
await runReview(app,obsidian,{file,args:{status:"согласовано"}});
assert.equal(writes,1);assert.equal(meta().review_status,"согласовано");assert.equal(meta().decision_status,"идея");assert.equal(meta().other,"keep");assert.ok(/Z$/.test(meta().reviewed_at));assert.equal(meta().reviewed_revision,await digest(text));
await runReview(app,obsidian,{file,args:{status:"согласовано"}});assert.equal(writes,1);
let label={};await runReview(app,obsidian,{file,args:{mode:"display"}},{registerEvent(){}},{createEl:()=>label});
assert.match(label.textContent,/согласовано/);assert.equal(writes,1);
const approvedAt=meta().reviewed_at;text=text.replace("Проверяемая мысль.","Изменённая мысль.");
await runReview(app,obsidian,{file,args:{mode:"display"}},{registerEvent(){}},{createEl:()=>label});assert.match(label.textContent,/изменён/);
await runReview(app,obsidian,{file,args:{status:"нужны правки"}});assert.equal(meta().reviewed_at,approvedAt);assert.equal(meta().review_history.length,2);
race=true;await assert.rejects(()=>runReview(app,obsidian,{file,args:{status:"согласовано"}}),/изменился/);assert.ok(text.endsWith("Чужая правка\n"));assert.equal(writes,2);race=false;
await assert.rejects(()=>runReview(app,obsidian,{file:{path:"01 Сырьё/test.md",extension:"md"},args:{status:"согласовано"}}),/производной/);
assert.equal(canonical("Текст. ^b-1\n\nЕщё."),canonical("Текст. ^b-2\nЕщё."));
assert.notEqual(canonical("\x60\x60\x60\n x\n\x60\x60\x60"),canonical("\x60\x60\x60\n  x\n\x60\x60\x60"));
assert.equal(canonical("Текст\n<!-- review-panel:start -->anything<!-- review-panel:end -->"),canonical("Текст"));
console.log("PASS: approval, timestamp, idempotence, history, revision detection, concurrent edits, target isolation, technical anchors.");
const noteDirs=["01 Сырьё","02 Корректура","03 Структура","04 База знаний"];
const notes=noteDirs.flatMap(d=>fs.readdirSync(path.join(vault,d)).filter(x=>x.endsWith(".md")).map(x=>d+"/"+x));
let links=0;
function validate(target,from){
target=decodeURIComponent(target);const [name,anchor]=target.split("#^");
let candidate=name.startsWith("../")?path.resolve(vault,path.dirname(from),name):path.join(vault,name.endsWith(".md")?name:name+".md");
if(!fs.existsSync(candidate)&&!name.includes("/")){const matches=notes.filter(n=>path.basename(n,".md")===name);assert.equal(matches.length,1,"Ambiguous/missing "+name);candidate=path.join(vault,matches[0])}
assert.ok(fs.existsSync(candidate),"Missing target "+target+" in "+from);
if(anchor){const t=fs.readFileSync(candidate,"utf8");assert.ok(new RegExp("\\^"+anchor+"(?=\\s|$)").test(t),"Missing anchor "+target)}
links++;
}
for(const note of notes){
const t=fs.readFileSync(path.join(vault,note),"utf8");
const anchors=[...t.matchAll(/(?:^|\s)\^([a-zA-Z0-9-]+)(?=\s*$)/gm)].map(m=>m[1]);assert.equal(anchors.length,new Set(anchors).size,"Duplicate anchors "+note);
assert.ok(!/^# /m.test(t),"Duplicate H1 "+note);
for(const m of t.matchAll(/\[\[([^\]|]+)(?:\|([^\]]+))?\]\]/g))validate(m[1],note);
for(const m of t.matchAll(/\[[^\]]*\]\(([^)]+)\)/g))if(!m[1].startsWith("http"))validate(m[1],note);
if(note.startsWith("04 "))assert.match(t,/^tags:/m);
}
console.log("PASS: "+links+" links resolve; anchors unique; no duplicate H1; knowledge notes tagged.");
// Update source fingerprints in topological order, using the same canonicalization as approval.
if(process.argv.includes("--update-source-hashes")){
for(const note of notes.filter(n=>!n.startsWith("01 "))){
const p=path.join(vault,note);let t=fs.readFileSync(p,"utf8");const header=t.match(/^---\n([\s\S]*?)\n---/);assert.ok(header);
const parent=header[1].match(/parents:\n\s+- "\[\[([^\]]+)\]\]"/);assert.ok(parent);
const source=fs.readFileSync(path.join(vault,parent[1]+".md"),"utf8"),hash=await digest(source);
let yaml=header[1].replace(/^source_revision:.*\n?/m,"").replace(/^source_revision_format:.*\n?/m,"").trimEnd();
yaml+="\nsource_revision: "+hash+"\nsource_revision_format: semantic-v1";
t=t.replace(header[0],"---\n"+yaml+"\n---");fs.writeFileSync(p,t);
}console.log("Updated source fingerprints.");
}
})();
