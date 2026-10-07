---
id: концепция-colonysim
created: 2026-10-03
parents:
  - "[[03 Структура/ColonySim]]"
source_revision: "D58A218BCEA264BE2221F7EBFF794C775B88C73644EF3BEFC10BA2DB3A6FA1C1"
review_status: не проверено
reviewed_at:
reviewed_revision:
review_changed_at:
review_history: []
decision_status: идея
tags:
  - проект/концепция
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

> Черновик базы знаний. Формулировки ниже — пересказ ИИ по предварительной записи владельца; они не являются утверждённым описанием продукта.

## Характер записи

[[03 Структура/ColonySim#^001|Исходная]] запись обозначена владельцем как предварительный набор идей для последующего превращения в граф.

## Общий замысел

[[03 Структура/ColonySim#^002|ColonySim]] задуман как мультиплатформенный колони-сим с основной площадкой на телефоне, вниманием к использованию процессора и памяти и управляемой глубиной симуляции. При развитии замысел может превратиться в масштабный движок моделирования сущностей и связей.

## Карта концепций

- [[Сущности симуляции]]
- [[Обобщение популяций]]
- [[Генерация на основе фактов]]
- [[Пешка]]
- [[Уровни глубины моделирования (SML)]]
- [[Наблюдаемость и оперативные пешки]]
- [[Децентрализованное поведение]]
- [[Технологическая роль Unity]]
- [[Бесконечный лабиринт]]
- [[Риски производительности]]
- [[Моддинг]]
- [[Совместимость версий моделирования]]

## Междисциплинарное направление

[[03 Структура/ColonySim#^020|Владелец]] хочет использовать в проекте подходы из баз данных, операционных систем и БЭВМ. Конкретные заимствования пока не определены.
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
