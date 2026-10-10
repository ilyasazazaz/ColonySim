// Shared implementation. Master maintains AI source; deploy byte-identical vault copy.
const FORMAT = "review-v3";
const FIELDS = ["review_status", "reviewed_at", "reviewed_revision", "reviewed_revision_format",
  "review_changed_at", "review_base_commit", "review_base_path"];
const LABELS = [["Согласовать", "согласовано"], ["Исправлено мной", "исправлено мной"],
  ["Нужны правки", "нужны правки"], ["Отменить действие", "undo"]];

function readMetadata(text, obsidian) {
  const match = text.match(/^\uFEFF?---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)/);
  if (!match) throw new Error("В заметке нет YAML-свойств.");
  const metadata = obsidian.parseYaml(match[1]);
  if (!metadata || typeof metadata !== "object" || Array.isArray(metadata)) throw new Error("Некорректные YAML-свойства.");
  return {metadata, body: text.slice(match[0].length)};
}
// Only explicit, balanced service regions OUTSIDE author code fences are excluded.
function scan(text) {
  const lines = text.replace(/\r\n?/g, "\n").split("\n");
  const records = [];
  let fence = null, service = null;
  for (let index = 0; index < lines.length; index++) {
    const line = lines[index];
    const marker = !fence && line.match(/^\s*<!-- review-(panel|properties|feedback):(start|end) -->\s*$/);
    if (marker) {
      if (marker[2] === "start") {
        if (service) throw new Error("Вложенные служебные блоки.");
        service = marker[1];
      } else {
        if (service !== marker[1]) throw new Error("Непарные служебные границы.");
        service = null;
      }
      records.push({line, index, marker: marker[1], edge: marker[2], service: true});
      continue;
    }
    const mark = line.match(/^ {0,3}(`{3,}|~{3,})(.*)$/);
    const isCode = !!fence || !!mark;
    if (fence) {
      if (mark && mark[1][0] === fence[0] && mark[1].length >= fence.length && !mark[2].trim()) fence = null;
    } else if (mark) fence = mark[1];
    records.push({line, index, code: isCode, service: !!service});
  }
  if (service) throw new Error("Служебный блок не закрыт.");
  return records;
}
function canonical(text) {
  const body = text.replace(/^\uFEFF?---\r?\n[\s\S]*?\r?\n---(?:\r?\n|$)/, "");
  const chunks = [], prose = [];
  const flush = () => { if (prose.length) { const value = prose.join("\n").replace(/\s+/g, " ").trim(); if (value) chunks.push(value); prose.length = 0; } };
  for (const record of scan(body)) {
    if (record.service) continue;
    if (record.code) { flush(); chunks.push(record.line); continue; }
    prose.push(record.line.replace(/(?:^|\s)\^[A-Za-z0-9-]+\s*$/, "")
      .replace(/\[\[([^\]|]+)(?:\|([^\]]+))?\]\]/g, (_, target, label) => label || target)
      .replace(/\[([^\]]+)\]\([^)]+\)/g, "$1"));
  }
  flush();
  return chunks.join("\n");
}
async function digest(text) {
  const bytes = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(canonical(text)));
  return Array.from(new Uint8Array(bytes), n => n.toString(16).padStart(2, "0")).join("");
}
function snapshot(metadata) {
  return Object.fromEntries(FIELDS.filter(key => Object.hasOwn(metadata, key)).map(key => [key, metadata[key]]));
}
function sameState(a, b) { return JSON.stringify(a) === JSON.stringify(b); }
function targetFile(context) {
  const file = context.file;
  if (!file || file.extension !== "md" || !/^(02 Корректура|03 Структура|04 База знаний)\//.test(file.path)) {
    throw new Error("Кнопка доступна только в производной заметке 02–04.");
  }
  return file;
}
function ensureFeedback(body) {
  const records = scan(body);
  const starts = records.filter(r => r.marker === "feedback" && r.edge === "start");
  if (starts.length > 1) throw new Error("В заметке несколько блоков «Для ИИ».");
  if (starts.length) return body;
  if (records.some(r => !r.code && !r.service && /^#{1,6}\s+Для ИИ\s*$/.test(r.line))) {
    throw new Error("Существующий блок «Для ИИ» нужно обрамить review-feedback:start/end, сохранив комментарий.");
  }
  return body + (body.endsWith("\n") ? "\n" : "\n\n") +
    "<!-- review-feedback:start -->\n---\n## Для ИИ\n\n\n---\n<!-- review-feedback:end -->\n";
}
async function revealFeedback(app, file) {
  const leaf = app.workspace.getLeavesOfType("markdown").find(item => item.view?.file?.path === file.path) || app.workspace.getLeaf(false);
  await leaf.openFile(file, {state: {mode: "source"}});
  const records = scan(await app.vault.read(file));
  const first = records.find(r => r.marker === "feedback" && r.edge === "start");
  if (first && leaf.view?.editor) {
    const editor = leaf.view.editor;
    const line = Math.min(first.index + 4, editor.lineCount() - 1);
    editor.setCursor({line, ch: 0});
    editor.scrollIntoView({from: {line, ch: 0}, to: {line, ch: 0}}, true);
    editor.focus();
  }
}
async function runReview(app, obsidian, context) {
  const file = targetFile(context);
  const action = context.args?.status;
  if (!LABELS.some(([, value]) => value === action)) throw new Error("Неизвестное действие.");
  const before = await app.vault.read(file);
  const {metadata: m, body} = readMetadata(before, obsidian);
  if (m.review_history != null && !Array.isArray(m.review_history)) throw new Error("История имеет неизвестный формат; данные сохранены.");
  let nextBody = body;
  const time = new Date().toISOString();
  const previous = snapshot(m);
  const revision = await digest(before);
  if (action === "undo") {
    if (!m.review_undo) { new obsidian.Notice("Нет действия для отмены."); return; }
    let undo;
    try { undo = JSON.parse(m.review_undo); } catch { throw new Error("Некорректная запись отмены; данные сохранены."); }
    if (undo?.version !== 1 || !undo.before || !undo.after || !sameState(previous, undo.after)) {
      throw new Error("Состояние проверки изменено вне кнопки. Отмена не выполнена.");
    }
    for (const key of FIELDS) { delete m[key]; if (Object.hasOwn(undo.before, key)) m[key] = undo.before[key]; }
    delete m.review_undo;
    m.review_history = [...(m.review_history || []), `${time} | отмена ${undo.action} | ${FORMAT}:${revision}`];
  } else {
    if (action === "исправлено мной") {
      if (typeof m.agent_revision !== "string" || !/^[a-f0-9]{40}$/i.test(m.agent_revision) ||
          typeof m.agent_revision_path !== "string" || !m.agent_revision_path.endsWith(".md") ||
          m.agent_revision_path.split(/[\\/]/).includes("..")) {
        throw new Error("Нет достоверной Git-базы: нужны agent_revision (полный SHA) и agent_revision_path. Агент должен заполнить их по истории Git.");
      }
    }
    if (action === "нужны правки") nextBody = ensureFeedback(body);
    const reviewed = ["согласовано", "исправлено мной"].includes(action);
    const sameBase = action !== "исправлено мной" || (m.review_base_commit === m.agent_revision && m.review_base_path === m.agent_revision_path);
    if (reviewed && m.review_status === action && m.reviewed_revision_format === FORMAT && m.reviewed_revision === revision && sameBase) {
      new obsidian.Notice("Эта редакция уже отмечена."); return;
    }
    if (action === "нужны правки" && m.review_status === action && nextBody === body) {
      return {feedback: true, file};
    }
    m.review_status = action;
    m.review_changed_at = time;
    if (reviewed) { m.reviewed_at = time; m.reviewed_revision = revision; m.reviewed_revision_format = FORMAT; }
    if (action === "исправлено мной") { m.review_base_commit = m.agent_revision; m.review_base_path = m.agent_revision_path; }
    m.review_history = [...(m.review_history || []), `${time} | ${previous.review_status || "не проверено"} → ${action} | ${FORMAT}:${revision}`];
    // Persist only review fields, not a copy of text, comments, or recursively nested history.
    m.review_undo = JSON.stringify({version: 1, action, before: previous, after: snapshot(m)});
  }
  const after = "---\n" + obsidian.stringifyYaml(m).trimEnd() + "\n---\n" + nextBody;
  await app.vault.process(file, current => {
    if (current !== before) throw new Error("Файл изменился во время операции. Чужие правки сохранены; повторите действие.");
    return after;
  });
  new obsidian.Notice(action === "undo" ? "Последнее действие отменено. Текст сохранён." : "Сохранено: " + action);
  return {feedback: action === "нужны правки", file};
}
async function renderPanel(app, obsidian, context, component, container) {
  const file = targetFile(context);
  if (!container) throw new Error("Панель требует контейнер заметки.");
  if (context.args?.mode === "properties") return; // Retired footer calls render nothing.
  const row = container.createEl("div", {cls: "review-actions"});
  row.style.display = "flex"; row.style.flexWrap = "wrap"; row.style.gap = "8px";
  const label = container.createEl("p");
  const errorLabel = container.createEl("p");
  errorLabel.setAttribute("role", "alert");
  const buttons = [];
  let busy = false, disposed = false, generation = 0;
  component.register(() => { disposed = true; generation++; });
  const refresh = async () => {
    const run = ++generation;
    try {
      const text = await app.vault.read(file);
      const {metadata: m} = readMetadata(text, obsidian);
      let message = m.review_status || "не проверено";
      if (["согласовано", "исправлено мной"].includes(message)) {
        if (m.reviewed_revision_format !== FORMAT) message += " · версия проверки устарела";
        else if (m.reviewed_revision !== await digest(text)) message += " · текст изменён после проверки";
      }
      if (m.reviewed_at) message += " · " + new Date(m.reviewed_at).toLocaleString("ru-RU");
      if (!disposed && run === generation) label.textContent = message;
    } catch (error) { if (!disposed && run === generation) label.textContent = "Не удалось проверить статус: " + error.message; }
  };
  for (const [title, status] of LABELS) {
    const button = row.createEl("button", {text: title});
    button.type = "button";
    buttons.push(button);
    component.registerDomEvent(button, "click", async () => {
      if (busy || disposed) return;
      busy = true; buttons.forEach(item => { item.disabled = true; }); errorLabel.textContent = "";
      let result;
      try { result = await runReview(app, obsidian, {file, args: {status}}); }
      catch (error) { errorLabel.textContent = "Не сохранено: " + error.message; }
      finally { busy = false; buttons.forEach(item => { item.disabled = false; }); await refresh(); }
      if (result?.feedback) {
        try { await revealFeedback(app, file); }
        catch { new obsidian.Notice("Статус сохранён. Блок «Для ИИ» находится в конце заметки; открыть редактор автоматически не удалось."); }
      }
    });
  }
  component.registerEvent(app.vault.on("modify", changed => { if (changed.path === file.path) void refresh(); }));
  await refresh();
}
await renderPanel(app, obsidian, context, component, container);
