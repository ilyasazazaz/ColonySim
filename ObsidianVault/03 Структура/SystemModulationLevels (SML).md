---
id: system-modulation-levels-структура
created: 2026-10-02
parents:
  - "[[02 Корректура/SystemModulationLevels (SML)]]"
source_revision: "B677DC4183A3B568CF0E61F485AD449CE9FB98091E9242D03E59393E6BABD649"
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

## Назначение SML

[[02 Корректура/SystemModulationLevels (SML)#^b-001|Игра]] рассчитана для разных устройств и с разной производительностью, а также разными потребностями пользователя. Надо обеспечить возможность урезать глубину моделирования не интересующих пользователя систем. Однако надо обеспечить, что при низких уровнях моделирования данные в общем походили на более высокие уровни. В идеале сделать возможность менять SML в процессе игры и получать валидные данные. ^kb-sml-purpose

## Пример: здоровье

[[02 Корректура/SystemModulationLevels (SML)#^b-002|Пример]] - здоровье ^kb-sml-health

- [[02 Корректура/SystemModulationLevels (SML)#^b-003|1]] - HP параметр ^kb-sml-health-1
- [[02 Корректура/SystemModulationLevels (SML)#^b-004|2]] - HP + эффекты (перелом ноги = - к движению) ^kb-sml-health-2
- [[02 Корректура/SystemModulationLevels (SML)#^b-005|3]] - HP частей чела, эффекты на части тела ^kb-sml-health-3
- [[02 Корректура/SystemModulationLevels (SML)#^b-006|4]] - Разные системы организма, HP заменено на процессы и циркуляцию ресурсов в теле ^kb-sml-health-4
- [[02 Корректура/SystemModulationLevels (SML)#^b-007|5]] - Условно полная модель работы организма, с мышцами ,молочной кислотой, эффектами мозолей и тд ^kb-sml-health-5

## Вариативность опыта

[[02 Корректура/SystemModulationLevels (SML)#^b-008|Кроме]] оптимизации разные SML могут дать разный опыт на разных сценариях, или вообще поменять жанр игры. ^kb-sml-experience

## Возможная организация систем

[[02 Корректура/SystemModulationLevels (SML)#^b-009|В]] таком подходе каждая система должна выступать как отдельный сервис с общим интерфейсом. При радикальной разнице в подходах реализации одной и той же системы на разных уровнях может понадобиться иерархия наследования абстрактных классов, и при сборке игровой сессии надо определить интерфейс работы системы. Но звучит почти нереалистичо ,да и не очень то и необходимо. Но проверить логированием можно. ^kb-sml-organization

## Изменения

- Материал сгруппирован по назначению, примеру, вариативности опыта и возможной организации систем.
- Пример здоровья оформлен списком без изменения текста. В корректуру добавлены технические якоря `^b-001`–`^b-009`.
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
