---
cssclasses:
  - review-clean-pilot
id: технологическая-роль-unity
created: 2026-10-03
parents:
  - "[[03 Структура/ColonySim]]"
  - "[[03 Структура/Что, зачем, откуда]]"
source_revisions:
  - path: "ObsidianVault/03 Структура/ColonySim.md"
    revision: "B2E0406C003131A98C68604DBEEDD4093EF2F1FF811C6EE32EEBE8D3F87A8BBB"
  - path: "ObsidianVault/03 Структура/Что, зачем, откуда.md"
    revision: "D4DDBE789095B11792D12052C8754B5CA63307A511B504E2E72FB11F7F26D2C0"
review_status: не проверено
reviewed_at:
reviewed_revision:
review_changed_at:
review_history: []
agent_revision: "69d7e73183155fa33c6c3859e81aa53b087e5581"
agent_revision_path: "ObsidianVault/04 База знаний/Технологическая роль Unity.md"
decision_status: идея
tags:
  - инструмент/unity
  - система/архитектура
rules_version: "2026-10-11T22:22:01+03:00"
---

<!-- review-panel:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {context: target, component, container});
```
<!-- review-panel:end -->



## Назначение

[[03 Структура/Что, зачем, откуда#^001|U]]nity рассматривается как средство визуализации и площадка кроссплатформенной разработки.

[[03 Структура/ColonySim#^014|Л]]огика моделирования предполагается вынесенной из Unity в доменную область модели; сам движок нужен главным образом для визуализации.


Такое разделение связано с [[Концепция ColonySim|мультиплатформенной концепцией]] и [[Совместимость версий моделирования|вариантами моделей]], но конкретные границы модулей не определены.

<!-- review-feedback:start -->

---
## Для ИИ

---
<!-- review-feedback:end -->
