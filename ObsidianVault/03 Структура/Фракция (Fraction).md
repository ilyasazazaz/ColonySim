---
id: фракция-структура
created: 2026-10-02
parents:
  - "[[02 Корректура/Фракция (Fraction)]]"
source_revision: "869EE93229CA0D55C0CD31505A94538928D1143A626CA7BBBBF283E0B582FF47"
review_status: не проверено
reviewed_at:
reviewed_revision:
review_changed_at:
review_history: []
decision_status:
tags: []
rules_version: "2026-10-03T00:31:33+03:00"
---

<!-- review-panel:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {context: target, component, container});
```
<!-- review-panel:end -->

## Фракция в классическом подходе

[[02 Корректура/Фракция (Fraction)#^b-001|В]] классическом подходе фракция обладает локациями и пешками. Ну, или они имеют фракцию как значение переменной «фракция». ^kb-faction-classic

## Управление фракцией и баланс

[[02 Корректура/Фракция (Fraction)#^b-002|Вопрос]], зависящий от уровня модели: может ли фракция управляться так, как это делает игрок? Приоритеты работ, эффективность построек, разные тактики сражения и развития. При подобном усложнении механики может появиться разница между результатами управления фракцией ИИ и ожидаемым результатом от статистического метода, из-за чего переход между версиями будет сильно влиять на баланс сил. ^kb-faction-control

## Открытый вопрос

[[02 Корректура/Фракция (Fraction)#^b-003|Насколько]] понятие фракции вообще применимо в децентрализованном планировщике (капитализме)? ^kb-faction-decentralized

## Изменения

- Материал сгруппирован по трём темам. В корректуру добавлены технические якоря `^b-001`–`^b-003`; формулировки не менялись.
- Обращений к ИИ в записи нет.

<!-- review-properties:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {
  context: target, component, container,
  contextOverrides: {args: {mode: "properties"}}
});
```
<!-- review-properties:end -->
