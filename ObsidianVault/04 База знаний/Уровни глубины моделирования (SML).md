---
id: "e8126256-6826-566b-95b9-3a37cd5d19ed"
created: "2026-10-12"
parents:
  - "[[03 Структура/ColonySim]]"
  - "[[03 Структура/SystemModulationLevels (SML)]]"
source_revisions:
  - path: "ObsidianVault/03 Структура/ColonySim.md"
    revision: "1338EDE3F2958DDA34C8102E3117957F6D519A634D9226322725E2112A86C793"
  - path: "ObsidianVault/03 Структура/SystemModulationLevels (SML).md"
    revision: "05E7B9D379753D640315F756C10F46EB5421898941BD3C2DB4A8FB9F38AD7715"
review_status: не проверено
reviewed_at:
reviewed_revision:
review_changed_at:
review_history: []
agent_revision: "a14f2daac455a75074a8111a56c22702ba68fd17"
agent_revision_path: "ObsidianVault/04 База знаний/Уровни глубины моделирования (SML).md"
decision_status:
tags: []
cssclasses:
  - review-clean-pilot
rules_version: "2026-10-13T22:33:17+03:00"
aliases:
  - "SML"
kind: "механика"
---

<!-- review-panel:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {context: target, component, container});
```
<!-- review-panel:end -->

О карточке: Настраиваемая глубина моделирования систем на разных устройствах и переходы между уровнями.

> [[03 Структура/ColonySim#^002|Р]]азные **уровни симуляции** разных процессов.

> [[03 Структура/ColonySim#^003|К]]лючевые сущности, присутствующие на любом **уровне симуляции**.

> [[03 Структура/ColonySim#^023|И]]спользуются [[04 База знаний/Пешка|пешками]] в [[04 База знаний/Задания и работы|заданиях]]. Могут иметь составные элементы в зависимости от **уровня симуляции**.

> [[03 Структура/SystemModulationLevels (SML)#^001|Н]]адо обеспечить возможность урезать глубину моделирования не интересующих пользователя систем. Однако надо обеспечить, что при низких **уровнях моделирования** данные в общем походили на более высокие уровни. В идеале сделать возможность менять **SML** в процессе игры и получать валидные данные.

> [[03 Структура/SystemModulationLevels (SML)#^008|К]]роме [[04 База знаний/Риски производительности|оптимизации]] разные **SML** могут дать разный опыт на разных сценариях, или вообще поменять жанр игры.

<!-- review-feedback:start -->

---

## Для ИИ


---
<!-- review-feedback:end -->
