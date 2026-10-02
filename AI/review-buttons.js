// Canonical source: AI/review-buttons.js. Deploy unchanged to 90 Служебное/review-buttons.js.
function canonical(text) {
  text = text.replace(/\r\n?/g, "\n").replace(/^---\n[\s\S]*?\n---(?:\n|$)/, "");
  text = text.replace(/<!-- review-(?:panel|properties):start -->[\s\S]*?<!-- review-(?:panel|properties):end -->/g, "");
  const kept = [];
  let fence = null, service = false;
  for (const line of text.split("\n")) {
    const mark = line.match(/^\s*(\x60{3,}|~{3,})/);
    if (fence) {
      if (!service) kept.push(line);
      if (mark && mark[1][0] === fence[0] && mark[1].length >= fence.length) fence = null;
      continue;
    }
    if (mark) { fence = mark[1]; if (!service) kept.push(line); continue; }
    // Headings are content: never discard sections by their visible title.
    if (service) continue;
    kept.push(line.replace(/(?:^|\s)\^[A-Za-z0-9-]+(?=\s*$)/g, "")
      .replace(/\[\[([^\]|]+)(?:\|([^\]]+))?\]\]/g, (_, target, label) => label || target)
      .replace(/\[([^\]]+)\]\([^)]+\)/g, "$1"));
  }
  // Preserve fenced code byte-for-byte after LF normalization; normalize prose only.
  let result = "", prose = [], inCode = null;
  const flush = () => { if (prose.length) { result += prose.join("\n").replace(/\s+/g, " ").trim() + "\n"; prose = []; } };
  for (const line of kept) {
    const mark = line.match(/^\s*(\x60{3,}|~{3,})/);
    if (inCode) { result += line + "\n"; if (mark && mark[1][0] === inCode[0] && mark[1].length >= inCode.length) inCode = null; }
    else if (mark) { flush(); inCode = mark[1]; result += line + "\n"; }
    else prose.push(line);
  }
  flush();
  return result.trim();
}
async function digest(text) {
  const bytes = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(canonical(text)));
  return Array.from(new Uint8Array(bytes), n => n.toString(16).padStart(2, "0")).join("");
}
function readMetadata(text, obsidian) {
  const match = text.match(/^---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)/);
  if (!match) throw new Error("В заметке нет свойств YAML.");
  const metadata = obsidian.parseYaml(match[1]);
  if (!metadata || typeof metadata !== "object" || Array.isArray(metadata)) throw new Error("Некорректные свойства заметки.");
  return {metadata, body: text.slice(match[0].length)};
}
async function runReview(app, obsidian, context, component, container) {
  const file = context.file;
  if (!file || file.extension !== "md" || !/^(02 Корректура|03 Структура|04 База знаний)\//.test(file.path)) {
    throw new Error("Кнопка должна находиться в производной заметке уровней 02–04.");
  }
  if (context.args?.mode === "display") {
    const label = container.createEl("p", {text: "Проверка согласования…"});
    let generation = 0;
    const refresh = async () => {
      const run = ++generation;
      try {
        const text = await app.vault.read(file);
        const {metadata: m} = readMetadata(text, obsidian);
        const changed = m.review_status === "согласовано" && (m.reviewed_revision_format !== "review-v2" || m.reviewed_revision !== await digest(text));
        if (run !== generation) return;
        label.textContent = changed ? "Текст изменён после согласования — нужна повторная проверка."
          : (m.review_status || "не проверено") + (m.reviewed_at ? " · последнее согласование: " + new Date(m.reviewed_at).toLocaleString("ru-RU") : "");
      } catch (e) { if (run === generation) label.textContent = "Не удалось проверить согласование: " + e.message; }
    };
    await refresh();
    component.registerEvent(app.vault.on("modify", changed => { if (changed.path === file.path) void refresh(); }));
    return;
  }
  const status = context.args?.status;
  if (!["согласовано", "нужны правки"].includes(status)) throw new Error("Неизвестное действие кнопки.");
  const before = await app.vault.read(file);
  const revision = await digest(before);
  const {metadata: m, body} = readMetadata(before, obsidian);
  if (status === "согласовано" && m.review_status === status && m.reviewed_revision === revision && m.reviewed_revision_format === "review-v2") {
    new obsidian.Notice("Эта редакция уже согласована.");
    return;
  }
  const time = new Date().toISOString();
  const previous = m.review_status || "не проверено";
  if (m.review_history != null && !Array.isArray(m.review_history)) throw new Error("review_history должен быть списком; прежние данные сохранены.");
  m.review_history = [...(m.review_history || []), time + " | " + previous + " → " + status + " | review-v2:" + revision];
  m.review_status = status;
  m.review_changed_at = time;
  if (status === "согласовано") {
    m.reviewed_at = time;
    m.reviewed_revision = revision;
    m.reviewed_revision_format = "review-v2";
  }
  const after = "---\n" + obsidian.stringifyYaml(m).trimEnd() + "\n---\n" + body;
  await app.vault.process(file, current => {
    if (current !== before) throw new Error("Файл изменился во время согласования. Перечитай его и нажми кнопку снова.");
    return after;
  });
  new obsidian.Notice(status === "согласовано" ? "Редакция согласована, дата сохранена." : "Отмечено: нужны правки.");
}
async function renderPanel(app, obsidian, context, component, container) {
  if (!container) throw new Error("Панель требует контейнер заметки.");
  const file = context.file;
  if (!file || file.extension !== "md" || !/^(02 Корректура|03 Структура|04 База знаний)\//.test(file.path)) {
    throw new Error("Панель доступна только в производной заметке.");
  }
  if (context.args?.mode === "properties") {
    const details = container.createEl("details");
    details.createEl("summary", {text: "Свойства"});
    const body = details.createEl("pre");
    let active = true;
    component.register(() => { active = false; });
    const refresh = async () => {
      try {
        const {metadata} = readMetadata(await app.vault.read(file), obsidian);
        if (active) body.textContent = obsidian.stringifyYaml(metadata);
      } catch (error) { if (active) body.textContent = error.message; }
    };
    component.registerEvent(app.vault.on("modify", changed => { if (changed.path === file.path) void refresh(); }));
    await refresh();
    return;
  }
  const row = container.createEl("div");
  const errorLabel = container.createEl("p");
  const buttons = [];
  let busy = false;
  for (const [title, status] of [["Согласовать", "согласовано"], ["Нужны правки", "нужны правки"]]) {
    const button = row.createEl("button", {text: title});
    buttons.push(button);
    component.registerDomEvent(button, "click", async () => {
      if (busy) return;
      busy = true;
      buttons.forEach(item => { item.disabled = true; });
      errorLabel.textContent = "";
      try { await runReview(app, obsidian, {file, args: {status}}); }
      catch (error) { errorLabel.textContent = "Не сохранено: " + error.message; }
      finally { busy = false; buttons.forEach(item => { item.disabled = false; }); }
    });
  }
  await runReview(app, obsidian, {file, args: {mode: "display"}}, component, container);
}
await renderPanel(app, obsidian, context, component, container);
