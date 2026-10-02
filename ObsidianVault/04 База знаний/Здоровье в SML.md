---
id: здоровье-в-sml
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
  - система/здоровье
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

> Черновик базы знаний. Названия уровней — компактный пересказ ИИ; примеры и порядок происходят из записи владельца.

## Пример уровней

[[03 Структура/SystemModulationLevels (SML)#^kb-sml-health|Здоровье]] используется как пример изменения глубины одной системы:

1. [[03 Структура/SystemModulationLevels (SML)#^kb-sml-health-1|HP]] как единый параметр.
2. [[03 Структура/SystemModulationLevels (SML)#^kb-sml-health-2|HP]] и эффекты, например влияние перелома ноги на движение.
3. [[03 Структура/SystemModulationLevels (SML)#^kb-sml-health-3|HP]] частей тела и локальные эффекты.
4. [[03 Структура/SystemModulationLevels (SML)#^kb-sml-health-4|Системы]] организма, процессы и циркуляция ресурсов вместо HP.
5. [[03 Структура/SystemModulationLevels (SML)#^kb-sml-health-5|Условно]] полная модель организма, включая мышцы, молочную кислоту и мозоли.

## Связи

Это пример механизма [[Уровни глубины моделирования (SML)]]. Возможный способ организации разных реализаций вынесен в [[Организация систем SML]].
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
