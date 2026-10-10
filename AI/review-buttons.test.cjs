const fs = require("node:fs"), path = require("node:path"), assert = require("node:assert/strict");
const {webcrypto} = require("node:crypto");
const AsyncFunction = Object.getPrototypeOf(async function(){}).constructor;
const script = fs.readFileSync(path.join(__dirname, "review-buttons.js"), "utf8");
const definitions = script.replace(/await renderPanel\(app, obsidian, context, component, container\);\s*$/, "return {canonical,digest,runReview,renderPanel,ensureFeedback,revealFeedback};");
if (definitions === script) throw new Error("Missing entry point");
(async () => {
  const api = await new AsyncFunction("crypto", definitions)(webcrypto);
  const {canonical, digest, runReview, renderPanel, ensureFeedback} = api;
  const sha = "a".repeat(40), gitPath = "ObsidianVault/04 База знаний/test.md";
  const initial = {id:"test", review_status:null, review_history:null, decision_status:"идея", tags:["тест"],
    agent_revision:sha, agent_revision_path:gitPath};
  let text, writes, race;
  const notices = [], listeners = [];
  const file = {path:"04 База знаний/test.md", extension:"md"};
  // JSON is a YAML subset. Test serializer substitutes Obsidian's parse/stringify API.
  const obsidian = {parseYaml:JSON.parse, stringifyYaml:JSON.stringify, Notice:class {constructor(s){notices.push(s)}}};
  const app = {vault:{read:async f=>{assert.equal(f,file);return text},
    process:async(f,fn)=>{assert.equal(f,file);if(race)text+="Чужая правка\n";const next=fn(text);text=next;writes++},
    on:(event,fn)=>{listeners.push(fn);return fn}}};
  const reset=(m=initial,body="\nПроверяемая мысль. ^001\n")=>{text="---\n"+JSON.stringify(m)+"\n---\n"+body;writes=0;race=false};
  const metadata=()=>JSON.parse(text.match(/^---\n([\s\S]*?)\n---/)[1]);
  const body=()=>text.slice(text.indexOf("\n---\n")+5);
  const action=status=>runReview(app,obsidian,{file,args:{status}});
  reset(); const originalBody=body();
  await action("согласовано");
  assert.equal(metadata().review_status,"согласовано");
  assert.equal(metadata().reviewed_revision,await digest(text));
  assert.match(metadata().reviewed_at,/Z$/); assert.equal(metadata().reviewed_revision_format,"review-v3");
  assert.equal(metadata().decision_status,"идея");assert.deepEqual(metadata().tags,["тест"]);assert.equal(body(),originalBody);
  await action("согласовано");assert.equal(writes,1);
  await action("undo");assert.equal(metadata().review_status,null);assert.equal(metadata().review_history.length,2);
  assert.equal(body(),originalBody);assert.equal(metadata().review_undo,undefined);
  await action("undo");assert.equal(writes,2);

  reset();text=text.replace("Проверяемая","Исправленная");
  await action("исправлено мной");
  assert.equal(metadata().review_base_commit,sha);assert.equal(metadata().review_base_path,gitPath);
  assert.equal(metadata().review_status,"исправлено мной");
  await action("исправлено мной");assert.equal(writes,1);
  text=text.replace(sha,"b".repeat(40)); // agent_revision changes, frozen feedback base does not.
  assert.equal(metadata().review_base_commit,sha);
  const approvedAt=metadata().reviewed_at;
  await action("нужны правки");assert.equal(metadata().reviewed_at,approvedAt);
  assert.equal((body().match(/review-feedback:start/g)||[]).length,1);
  const markedBody=body();await action("нужны правки");assert.equal(writes,2);assert.equal(body(),markedBody);
  text=text.replace("## Для ИИ\n\n", "## Для ИИ\n\nМне понравилась идея.\n");
  const commentedBody=body();
  await action("undo");assert.equal(metadata().review_status,"исправлено мной");assert.equal(body(),commentedBody);
  assert.equal(metadata().review_base_commit,sha);assert.equal(metadata().review_history.length,3);

  reset({...initial,agent_revision:null});const missing=text;
  await assert.rejects(()=>action("исправлено мной"),/Git-базы/);assert.equal(text,missing);assert.equal(writes,0);
  reset({...initial,review_history:"not a list"});await assert.rejects(()=>action("согласовано"),/История/);assert.equal(writes,0);
  reset();await action("согласовано");text=text.replace('"review_status":"согласовано"','"review_status":"ручная правка"');
  await assert.rejects(()=>action("undo"),/вне кнопки/);assert.equal(metadata().review_status,"ручная правка");
  reset();race=true;await assert.rejects(()=>action("согласовано"),/изменился/);assert.equal(writes,0);assert.match(text,/Чужая правка/);
  reset();await assert.rejects(()=>runReview(app,obsidian,{file:{path:"01 Сырьё/test.md",extension:"md"},args:{status:"согласовано"}}),/производной/);
  await assert.rejects(()=>action("unknown"),/Неизвестное/);

  const plain="Текст. ^001\n\nЕщё.";
  assert.equal(canonical(plain),canonical("Текст. ^002\nЕщё."));
  assert.equal(canonical(plain),canonical(ensureFeedback(plain)));
  assert.equal(canonical(ensureFeedback(plain)),canonical(ensureFeedback(plain).replace("## Для ИИ","## Для ИИ\nПожелания")));
  assert.equal(canonical("[[03 Структура/X#^001|К]][[04 База знаний/Y|олония]]"),"Колония");
  assert.notEqual(canonical("Текст."),canonical("Текст!"));
  assert.notEqual(canonical("```\n x\n```"),canonical("```\n  x\n```"));
  assert.match(canonical("```\n<!-- review-feedback:start -->\nважно\n<!-- review-feedback:end -->\n```"),/важно/);
  assert.notEqual(canonical("## Для ИИ\nА"),canonical("## Для ИИ\nБ"));
  assert.throws(()=>canonical("<!-- review-panel:start -->\ntext"),/не закрыт/);
  assert.throws(()=>canonical("<!-- review-panel:start -->\n<!-- review-feedback:end -->"),/Непарные/);
  assert.throws(()=>ensureFeedback("## Для ИИ\nЛичный комментарий"),/обрамить/);
  const twice=ensureFeedback(plain)+"\n"+ensureFeedback("");assert.throws(()=>ensureFeedback(twice),/несколько/);

  const elements=[],clicks=[],disposers=[];
  const element=()=>({style:{},textContent:"",setAttribute(){},createEl(tag,options={}){const e=element();e.tag=tag;e.textContent=options.text||"";elements.push(e);return e}});
  const component={register(fn){disposers.push(fn)},registerEvent(){},registerDomEvent(el,event,fn){clicks.push({el,event,fn})}};
  reset();await renderPanel(app,obsidian,{file},component,element());
  assert.equal(writes,0);assert.deepEqual(clicks.map(c=>c.el.textContent),["Согласовать","Исправлено мной","Нужны правки","Отменить действие"]);
  await clicks[1].fn();assert.equal(metadata().review_status,"исправлено мной");
  text=text.replace("Проверяемая","Изменённая");
  await listeners.at(-1)(file);await new Promise(r=>setTimeout(r,10));
  assert.ok(elements.some(e=>e.textContent.includes("текст изменён после проверки")));
  const count=elements.length;await renderPanel(app,obsidian,{file,args:{mode:"properties"}},component,element());assert.equal(elements.length,count);
  let opened=null,cursor=null;
  app.workspace={getLeavesOfType:()=>[],getLeaf:()=>({openFile:async(f)=>{opened=f},view:{editor:{lineCount:()=>100,setCursor:p=>{cursor=p},scrollIntoView(){},focus(){}}}})};
  await clicks[2].fn();assert.equal(opened,file);assert.ok(cursor.line>0);assert.equal(metadata().review_status,"нужны правки");
  await clicks[3].fn();assert.equal(metadata().review_status,"исправлено мной");assert.match(body(),/Для ИИ/);
  const beforeDisposed=writes;disposers.forEach(fn=>fn());await clicks[0].fn();assert.equal(writes,beforeDisposed);
  console.log("PASS: four actions; null metadata; idempotence; Git base; persistent undo; body/comment preservation; invalid input; write race; target isolation; canonical exclusions; UI handlers; no footer; comment navigation.");
})().catch(error=>{console.error(error);process.exitCode=1;});
