---
id: организация-систем-sml
created: 2026-10-02
parents:
  - "[[03 Структура/SystemModulationLevels (SML)]]"
source_revision: "1079A0D17A85D3022BCC03B2BB87463A4254750F2D5155D9B12AC14EAAC0B34E"
review_status: не проверено
reviewed_at:
reviewed_revision:
review_changed_at:
review_history: []
decision_status: идея
tags:
  - система/архитектура
  - система/моделирование
rules_version: "2026-10-03T00:31:33+03:00"
---

<!-- review-panel:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {context: target, component, container});
```
<!-- review-panel:end -->

> Черновик базы знаний. Здесь сохранены и техническое предположение владельца, и его сомнение; предложение не является утверждённой архитектурой.

## Возможный подход

[[03 Структура/SystemModulationLevels (SML)#^kb-sml-organization|Каждая]] моделируемая система могла бы выступать отдельным сервисом с общим интерфейсом. При радикально разных реализациях уровней в источнике допускаются иерархия абстрактных классов и выбор интерфейса системы при сборке игровой сессии.

## Сомнение и проверка

[[03 Структура/SystemModulationLevels (SML)#^kb-sml-organization|В]] той же исходной мысли владелец оценивает подход как почти нереалистичный и, возможно, ненужный. Проверка логированием названа возможной, но запись не является актуальной командой на реализацию или эксперимент.

## Связи

Подход относится к механизму [[Уровни глубины моделирования (SML)]]; [[Здоровье в SML]] показывает систему, для которой уровни реализации заметно различаются.
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
