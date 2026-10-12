---
cssclasses:
  - review-clean-pilot
id: уровни-глубины-моделирования-sml
created: 2026-10-02
parents:
  - "[[03 Структура/SystemModulationLevels (SML)]]"
  - "[[03 Структура/ColonySim]]"
source_revisions:
  - path: "ObsidianVault/03 Структура/SystemModulationLevels (SML).md"
    revision: "2047385AECB51B522A16CB663CE40A5CD885D01879F042AC09910EAA29254D7F"
  - path: "ObsidianVault/03 Структура/ColonySim.md"
    revision: "B2E0406C003131A98C68604DBEEDD4093EF2F1FF811C6EE32EEBE8D3F87A8BBB"
review_status: нужны правки
reviewed_at:
reviewed_revision:
review_changed_at: 2026-10-10T18:45:53.794Z
review_history:
  - 2026-10-10T18:45:53.794Z | не проверено → нужны правки | review-v3:741e5ef22df3a60bc3d0fe1d48e65e02ba9d8ca8c84efc6c7bfec20481e644fb
agent_revision: 69d7e73183155fa33c6c3859e81aa53b087e5581
agent_revision_path: ObsidianVault/04 База знаний/Уровни глубины моделирования (SML).md
decision_status: идея
tags:
  - система/моделирование
  - система/производительность
rules_version: "2026-10-11T22:22:01+03:00"
review_undo: '{"version":1,"action":"нужны правки","before":{"review_status":"не проверено","reviewed_at":null,"reviewed_revision":null,"review_changed_at":null},"after":{"review_status":"нужны правки","reviewed_at":null,"reviewed_revision":null,"review_changed_at":"2026-10-10T18:45:53.794Z"}}'
---

<!-- review-panel:start -->
```js-engine
const path = "90 Служебное/review-buttons.js";
const target = await engine.internal.getContextForMarkdownCallingJSFile(context.file.path, path);
await engine.internal.executeFile(path, {context: target, component, container});
```
<!-- review-panel:end -->



## Назначение

[[03 Структура/SystemModulationLevels (SML)#^001|S]]ML — предполагаемые уровни глубины моделирования систем. Идея должна учитывать устройства разной производительности и разные интересы пользователя, позволяя уменьшать глубину неинтересующих систем.

[[03 Структура/SystemModulationLevels (SML)#^001|П]]ри низкой глубине агрегированные данные должны оставаться в целом сопоставимыми с результатами более высоких уровней. Желаемое, но не утверждённое свойство — переключение SML во время игры с сохранением валидных данных.

[[03 Структура/ColonySim#^002|П]]ользователь предположительно выбирает глубину симуляции систем с учётом своего устройства и ожидаемой производительности. В общей концепции SML связан с мобильной целевой площадкой и масштабным движком сущностей и связей.

## Влияние на игровой опыт

[[03 Структура/SystemModulationLevels (SML)#^008|Р]]азные SML рассматриваются не только как оптимизация: в разных сценариях они могут менять игровой опыт или даже жанр игры.

## Примеры и границы

- [[Здоровье в SML]] — пример пяти уровней детализации одной системы.
- [[Организация систем SML]] — возможная техническая организация уровней и сомнения владельца в её необходимости.
- [[Фракция]] — возможная связь с детализацией управления; это гипотеза ИИ, а не установленная принадлежность к SML.
- [[Уровни детализации пешки]] — отдельная шкала L0–L4, которую источник пока не отождествляет с SML.

<!-- review-feedback:start -->

---
## Для ИИ
Что ещё блять за версии источников? это лишний блок, убрать, или перенести в свойства.

---
<!-- review-feedback:end -->
