# Clerk Systems

## Runtime composition

`GameBootstrap` is created before the gameplay scene and owns the long-lived
services. Scene components register themselves with those services when they
become active. Runtime rules no longer depend on finding unrelated objects every
frame.

The scripts are separated into runtime, editor, and edit-mode test assemblies:

- `Clerk.Runtime`
- `Clerk.Editor`
- `Clerk.EditModeTests`

## Implemented gameplay

- central Input System actions, controller bindings, prompt display names,
  persistent rebinding, gameplay modes, pause, and cursor ownership
- first-person movement, prioritized raycast interactions, held items, and
  non-destructive interaction highlighting
- immutable product definitions plus runtime prices and a price-demand curve
- shelf registry, out-of-stock assignment, customer standing positions,
  stock reservations, and reservation-safe player access
- box delivery, opening, preview, player stocking, throwing, and employee
  restocking
- furniture purchase, delivery, placement, snapping, collision validation,
  persistent instance identity, and resale service
- customer spawning, weighted shopping plans, entrance/browse/pickup states,
  patience, unavailable products, price rejection, baskets, bags, queues,
  checkout, payment, and exit
- starter checkout with queue positions, scanning, payment, transaction
  completion, and next-customer handling
- exact cent-based money, purchases, refunds, sales, operating costs, employee
  wages, loans, daily interest, revenue, expenses, profit, and statistics
- store days, progression level/experience/reputation, unlock collections,
  expansion zones, and four starter objectives
- versioned JSON save slots for player, wallet, day, product prices, shelves,
  furniture, delivered boxes, ledger, finance, progression, statistics, and
  objectives
- main menu, gameplay HUD, pause menu, notifications, daily summary, save slots,
  purchase screens, banking, register, staff, tasks, settings, and input
  rebinding

## Desktop and mobile UI

The HTML mockups are represented by a shared application layer and two shells:

- desktop uses a wide Clerk OS window, navigation rail, workspace, and
  desktop-oriented spacing
- mobile uses a narrow phone frame, compact navigation bar, and the supplied
  `Assets/Models/UI/Mobile.fbx` as the held-device visual

Both shells read and command the same authoritative systems. Purchasing,
finances, objectives, saves, and settings therefore cannot drift between
platforms.

## Authoring extension points

Content remains data-driven:

- add products with `StockInfo` and `StockPurchaseData`
- add furniture with `FurniturePurchaseData`
- add customer variants with `CustomerDefinition`
- add objectives with `ObjectiveDefinition`
- add employees with `EmployeeDefinition`
- add store areas with `StoreExpansionZone`

New content should be added to its catalog or scene installer rather than
hard-coded into UI or customer logic.
