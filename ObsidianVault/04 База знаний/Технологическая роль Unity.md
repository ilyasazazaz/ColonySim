---
id: технологическая-роль-unity
created: 2026-10-03
parents:
  - "[[03 Структура/ColonySim]]"
  - "[[03 Структура/Что, зачем, откуда]]"
source_revision: "см. раздел «Версии источников»"
review_status: не проверено
reviewed_at:
reviewed_revision:
review_changed_at:
review_history: []
decision_status: идея
tags:
  - инструмент/unity
  - система/архитектура
rules_version: "2026-10-03T00:31:33+03:00"
---

<!-- review-panel:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {context: target, component, container});
```
<!-- review-panel:end -->

> Черновик базы знаний. Это зафиксированное намерение владельца, а не утверждённая архитектура реализации.

## Назначение

[[03 Структура/Что, зачем, откуда#^001|Unity]] рассматривается как средство визуализации и площадка кроссплатформенной разработки.

[[03 Структура/ColonySim#^014|Логика]] моделирования предполагается вынесенной из Unity в доменную область модели; сам движок нужен главным образом для визуализации.

## Связи

Такое разделение связано с [[Концепция ColonySim|мультиплатформенной концепцией]] и [[Совместимость версий моделирования|вариантами моделей]], но конкретные границы модулей не определены.

## Версии источников

| Предшественник | SHA256 |
| --- | --- |
| [[03 Структура/ColonySim]] | `D58A218BCEA264BE2221F7EBFF794C775B88C73644EF3BEFC10BA2DB3A6FA1C1` |
| [[03 Структура/Что, зачем, откуда]] | `20421673E0100575A4EDDDE3E38707CD2B963F383983EE76844AD134B234CD37` |
\n+<!-- review-properties:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {
  context: target, component, container,
  contextOverrides: {args: {mode: "properties"}}
});
```
<!-- review-properties:end -->
