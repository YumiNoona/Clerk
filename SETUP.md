# Clerk Project Setup and System Reference

This is the authoritative setup file for the Clerk Unity project. It explains
which scene objects are required, which components belong on them, how assets
connect, and what every script is responsible for.

Unity version: `6000.5.5f1`

Main scene: `Assets/Scenes/Street.unity`

## Important architecture rule

Do not manually create the `[Clerk Runtime]` object. `GameBootstrap` creates it
before the scene loads and marks it `DontDestroyOnLoad`. It contains the runtime
services for input, modes, shelves, customers, checkout, economy, days,
statistics, finance, progression, settings, saving, furniture, objectives,
employees, notifications, and UI.

Scene objects should contain physical references and authored content. Runtime
state belongs to the services created by `GameBootstrap`.

## Required project layers

Open **Edit > Project Settings > Tags and Layers** and keep these layers:

| Layer | Index | Used by |
|---|---:|---|
| Default | 0 | General environment |
| UI | 5 | Unity UI |
| Stock | 6 | Individual products |
| Shelf | 7 | Shelf interaction colliders |
| Stock Box | 8 | Delivered stock boxes |
| Garbage Bin | 9 | Optional disposal interactions |
| Furniture | 10 | Movable furniture |
| Furniture Placement Surface | 11 | Valid floors |
| Furniture Placement Blocker | 12 | Walls and blocked areas |

The player's `InteractionMask` must include Stock, Shelf, Stock Box, Furniture,
and any device layers used by `StoreDeviceInteractable`.

## Required scene hierarchy

The names are recommendations; components and references are authoritative.

### Player

Create `Player` at the scene root:

| Component | Required setup |
|---|---|
| `CharacterController` | Fit capsule to the player height |
| `PlayerController` | Assign Character Controller and Main Camera; input is provided by `GameInputController` |
| `PlayerInteractionController` | Assign Main Camera, HoldPoint, BoxHoldPoint, range, and interaction mask |

Create these children:

| Child | Placement | Purpose |
|---|---|---|
| `Main Camera` | At eye height | First-person rendering and raycasts |
| `HoldPoint` | In front of camera | Held products |
| `BoxPoint` | Lower/in front of camera | Held boxes |

`InteractionHighlightPresenter` is added to the player automatically. It does
not need to be placed manually.

### Game Systems

Create root empty object `Game Systems` and the following children.

#### Purchase Service

Add `PurchaseService` and assign:

| Field | Assignment |
|---|---|
| Stock Delivery Service | Scene `Stock Delivery Service` component |
| Furniture Delivery Service | Scene `FurnitureDeliveryService` component |
| Purchase Catalog | `Assets/Data/Purchase/Main Purchase Catalog.asset` |
| Customer Database | `Assets/Data/Customer/Customer Database.asset` |
| Starting Objectives | The four assets in `Assets/Data/Objectives` |
| Employee Catalog | `Assets/Data/Employees/Restocker.asset` |
| Mobile Model | `Assets/Models/UI/Mobile.fbx` |
| Checkout Model | Cute Supermarket `checkout_blue.prefab` |
| Create Starter Checkout | Enabled for the first playable store |

This component also creates/configures the `Customer Spawner` and starter
checkout at runtime when they are not already present.

#### Stock Delivery Service

Create `Stock Delivery Service`, add `StockDeliveryService`, then create child
`Stock Delivery Spawn Point`. Assign the child to `Delivery Spawn Point`. Put it
in a clear loading area where boxes can safely appear.

#### Furniture Delivery Service

Create `FurnitureDeliveryService`, add `FurnitureDeliveryService`, then create
child `Furniture Delivery Spawn Point`. Assign it to `Furniture Spawn Point`.
Leave enough clear floor for full furniture prefabs.

#### Furniture Placement Controller

Create `Furniture Placement Controller`, add `FurniturePlacementController`,
and assign the player camera, player interaction controller, and placement-area
reference used by the component.

#### Store Placement Area

Create `Store Placement Area`, add `FurniturePlacementArea`, and use its bounds
to describe the area in which furniture may be placed. Floors should use layer
`Furniture Placement Surface`; walls/obstacles should use
`Furniture Placement Blocker`.

### Customer System

Create empty root `Customer System` with three grouping children.

#### Spawn Points

Create one or more children such as `Customer Spawn Point 01` and add
`CustomerSpawnPoint`. Place these outside the store on a baked NavMesh.

#### Entrance Points

Create one or more `Customer Entrance Point 01` objects with
`CustomerEntrancePoint`. Add child `Inside Point` just inside the door and
assign it. Both positions must be reachable on the NavMesh.

#### Exit Points

Create one or more `Customer Exit Point 01` objects with `CustomerExitPoint`.
Add child `Despawn Point` beyond the visible exit and assign it.

`CustomerSpawner` finds these enabled points automatically.

### Navigation

Create `Store NavMesh` with the project's NavMesh surface component. Include
the customer-accessible floor, exclude shelves/walls as appropriate, and bake
after changing the store shell. Furniture clearance bounds should reserve
enough walking space in front of shelves and checkouts.

### UI Canvas

The responsive main menu, HUD, pause menu, desktop, phone, notifications, daily
summary, settings, and save slots are generated by `StoreUIService`.

The gameplay UI is generated at runtime:

| Component/object | Purpose |
|---|---|
| `StoreUIService` | HUD, crosshair, shelf price editor, menus, and device apps |
| `InteractionPromptDisplay` | Context-sensitive world interaction prompts |
| `WalletController` | Persistent runtime balance owned by `GameBootstrap` |
| Runtime `EventSystem` + `InputSystemUIInputModule` | Mouse, keyboard, and controller UI input |

There must be exactly one active `EventSystem` in the scene. `StoreUIService`
creates one only if the scene has none.

## Prefab setup

### Product prefab

Each physical product prefab needs:

- layer `Stock`
- renderer and collider
- Rigidbody
- `StockObject`
- `StockObject.Info` assigned to its `StockInfo`
- Rigidbody and mesh-collider fields assigned when not found automatically

### Box prefab

Each box prefab needs:

- layer `Stock Box`
- Rigidbody and collider
- `StockBoxController`
- flap pivots when the box has animated flaps
- content origin for preview/stock spawning
- optional TMP product and quantity labels

Product, layout, and quantity are supplied by `StockDeliveryService` after a
purchase.

### Shelf or shelf furniture prefab

The furniture root needs `PlaceableFurniture`, a placement `BoxCollider`, and
optional disabled clearance bounds. Each stockable shelf section needs
`ShelfSpaceController` and a collider on the Shelf layer.

For every `ShelfSpaceController`:

- choose Smart Placement or Placement Points
- assign shelf label TMP text if present
- set customer standing point in front of the shelf
- keep `Keep Product Assignment When Empty` enabled for out-of-stock reporting
- use unique placement-point groups for category-specific layouts

Runtime shelf and furniture IDs are generated automatically. Never copy IDs
between scene instances manually.

### Customer prefab

The root needs:

- `NavMeshAgent`
- `CustomerNavigation`
- `CustomerAnimator`
- `CustomerVisualVariation`
- an Animator on its visual child
- a child whose name contains `Shopping Bag`

`CustomerContext` and `CustomerBrain` are added by the spawner if absent. The
bag is found automatically and remains hidden until the basket contains stock.
Optional animation parameter names must be left empty until the Animator
Controller contains those parameters.

### Checkout prefab

For a custom checkout, add:

- `PlaceableFurniture`
- placement bounds
- `CheckoutCounter`
- interaction collider
- optional TMP status display
- optional ordered queue-point transforms behind the serving position

When queue points are omitted, the counter generates positions using fallback
spacing and capacity.

### Employee prefab

An employee prefab needs `NavMeshAgent` and `EmployeeContext`. A restocker also
needs `RestockEmployeeBrain`. The supplied Restocker definition reuses the
customer visual rig; its navigation animation continues to read agent velocity.

### Desktop or phone world device

Add a collider and `StoreDeviceInteractable` to any physical monitor or phone.
Select Desktop or Mobile in `Device Kind`. The pause menu also exposes both
interfaces, so world devices are optional.

## Data asset setup

### Product data

1. Create a `StockCategory`.
2. Create `StockInfo`; assign name, category, base price, initial price, product
   prefab, and default box layout.
3. Create `StockPurchaseData`; assign product, box prefab, quantity, price, and
   unlock level.
4. Add it to `Main Purchase Catalog.Stock Purchases`.

`StockInfo` is definition data. Changing a price during play updates
`ProductStateService`, never the asset.

### Furniture data

1. Create `FurniturePurchaseData`.
2. Assign display information, purchase price, unlock level, and furniture
   prefab.
3. Add it to `Main Purchase Catalog.Furniture Purchases`.

### Customer data

1. Create `CustomerDefinition` and assign the customer prefab, spawn weight,
   movement range, shopping range, browse time, patience, and penalties.
2. Add it to `Customer Database.Customer Definitions`.

### Objective data

Create `ObjectiveDefinition`, select its tracked event, target, money reward,
and experience reward. Add starting objectives to `PurchaseService`.

### Employee data

Create `EmployeeDefinition`, choose role, prefab, hiring price, daily wage,
movement speed, and work interval. Add it to `PurchaseService.Employee Catalog`.

### Expansion data

Place `StoreExpansionZone` in the scene. Assign its stable ID, price, required
store level, locked barrier, and unlocked content root.

## Runtime scripts

### Core

| Script | Responsibility |
|---|---|
| `GameBootstrap` | Composition root and persistent service owner |
| `GameInputController` | Central actions, bindings, prompts, and rebinding persistence |
| `GameplayAction` | Names all player actions |
| `GameplayMode` | Names gameplay/UI modes |
| `GameplayModeController` | Movement/input permissions, cursor, pause, and time scale |
| `PlayerController` | First-person movement, jump, and look adapter |
| `PlayerInteractionController` | Raycast scan, priority selection, prompts, input dispatch, and held-item ownership |
| `ShelfSpaceController` | Physical shelf placement/view and customer-safe inventory API |

### Interaction

| Script | Responsibility |
|---|---|
| `IInteractable` | Interaction contract |
| `IInteractionPromptProvider` | Context-sensitive prompt contract |
| `IHeldItem` | Pick-up, held update, release, and prompt contract |
| `InteractableBehaviour` | Shared enable/priority/custom-prompt behavior |
| `InteractionContext` | Player, ray, hit, and interaction type passed to targets |
| `InteractionType` | Primary, secondary, use, and move interaction types |
| `InteractionHighlightPresenter` | Non-destructive renderer highlighting for the selected target |
| `InteractionPromptDisplay` | Shows current prompt at the crosshair |

### Stock and boxes

| Script | Responsibility |
|---|---|
| `StockInfo` | Immutable authored product definition |
| `StockCategory` | Product category definition |
| `StockObject` | One physical product and held-item behavior |
| `BoxLayout` | Authored product positions inside a box |
| `StockBoxController` | Box inventory, preview, flaps, carrying, throwing, and stocking |
| `StockPurchaseData` | Purchasable stock-box definition |
| `StockDeliveryService` | Creates configured boxes at delivery point |
| `ProductState` | Mutable runtime state for one product |
| `ProductStateService` | Runtime product price lookup/change events |
| `ShelfReservation` | Reservation token and remaining quantity |
| `ShelfRegistry` | Active shelf lookup, quantity queries, and nearest reservations |

### Furniture

| Script | Responsibility |
|---|---|
| `PlaceableFurniture` | Persistent instance, preview, bounds, rotation, and placement adapter |
| `FurniturePlacementController` | Starts and controls placement sessions |
| `FurniturePlacementArea` | Defines allowed store placement bounds |
| `FurniturePurchaseData` | Purchasable furniture definition |
| `FurnitureDeliveryService` | Instantiates delivered furniture |
| `FurnitureService` | Registry and furniture resale/refund behavior |

### Customers

| Script | Responsibility |
|---|---|
| `CustomerDefinition` | Authored customer type and behavior ranges |
| `CustomerDatabase` | Weighted customer definition catalog |
| `CustomerState` | Lifecycle states |
| `CustomerPoint` | Shared scene-point base |
| `CustomerSpawnPoint` | Customer creation position |
| `CustomerEntrancePoint` | Door and inside position |
| `CustomerExitPoint` | Exit and despawn position |
| `CustomerNavigation` | NavMesh movement adapter and completion/failure events |
| `CustomerAnimator` | Animator parameter adapter with optional behaviors |
| `CustomerVisualVariation` | Random material/visual variation |
| `CustomerBasket` | Purchased line items and totals |
| `CustomerShoppingPlan` | Desired products and quantities plus demand weighting |
| `CustomerContext` | Per-customer references, state, patience, bag, basket, and checkout assignment |
| `CustomerBrain` | Enter, shop, browse, reserve, queue, checkout, and leave sequence |
| `CustomerRegistry` | Active-customer tracking |
| `CustomerSpawner` | Capacity, timing, point choice, definitions, and lifecycle start |

### Checkout

| Script | Responsibility |
|---|---|
| `CheckoutRegistry` | Active checkout discovery and best-counter choice |
| `CheckoutCounter` | Queue ownership, scan/use interactions, payment, display, and completion |
| `CheckoutSession` | One customer's immutable basket transaction and scan progress |

### Economy, progression, and staff

| Script | Responsibility |
|---|---|
| `Money` | Exact integer-cent money value |
| `LedgerEntry` | Persisted financial transaction record |
| `StoreEconomyService` | Spend, refunds, revenue, costs, and daily totals |
| `ProductDemandService` | Price/reputation purchase probability |
| `StoreDayController` | Store clock, opening/closing, rent, and utilities |
| `StoreStatisticsService` | Customers, sales, items, revenue, and expense totals |
| `StoreFinanceService` | Loans, repayment, credit, and daily interest |
| `ProgressionService` | Store level, XP, reputation, and unlock sets |
| `StoreExpansionZone` | Purchasable scene expansion activation |
| `ObjectiveDefinition` | Authored objective and reward |
| `ObjectiveService` | Objective event tracking, completion, rewards, and persistence |
| `EmployeeDefinition` | Authored staff role and cost |
| `EmployeeContext` | Employee identity, definition, and NavMeshAgent |
| `EmployeeService` | Hire/fire registry, box claims, and wages |
| `RestockEmployeeBrain` | Finds boxes and compatible shelves and performs restocking |

### Purchasing, saving, settings, and UI

| Script | Responsibility |
|---|---|
| `PurchasableData` | Base display, price, ID, and unlock data |
| `PurchaseCatalog` | Stock and furniture catalogs |
| `PurchaseService` | Validates, charges, fulfills, refunds, and wires authored catalogs |
| `SaveGameData` | Versioned plain save DTOs |
| `SaveGameService` | Three visible JSON slots plus capture/restore of runtime systems |
| `GameSettingsService` | Volume, sensitivity, fullscreen, quality, and frame rate |
| `WalletController` | Persistent wallet balance and balance-change events |
| `UIFactory` | Shared programmatic uGUI construction and visual theme |
| `StoreUIService` | Main menu, HUD, crosshair, shelf pricing, desktop/mobile apps, settings, and saves |
| `StoreDeviceInteractable` | Opens desktop or phone from a world object |
| `NotificationService` | Typed toast notifications |

### Editor scripts and tests

| Script | Responsibility |
|---|---|
| `StockInfoEditor` | Product-definition inspector |
| `ShelfSpaceControllerEditor` | Shelf layout and preview tooling |
| `StockBoxControllerEditor` | Box preview tooling |
| `ClerkTestRunner` | **Clerk > Validation > Run Edit Mode Tests** menu |
| `Clerk.EditModeTests` | Money, basket, shopping request, and objective tests |

## Save data

Save files are stored under `Application.persistentDataPath/Saves` and contain:

- wallet and ledger
- player transform
- current day/time/open state
- runtime product prices
- shelf product assignments and quantities
- furniture transforms and identities
- delivered boxes
- employees
- loans
- progression/unlocks/reputation
- statistics
- objective progress

Settings and input binding overrides use `PlayerPrefs` so they remain available
before a save slot is loaded.

## Validation checklist

After changing scene wiring:

1. Clear the Console and enter Play mode.
2. Confirm the main menu responds to the first click.
3. Start the store and confirm movement/look/interactions.
4. Order a stock box through Desktop or Phone.
5. Open the box and stock a shelf.
6. Wait for a customer to enter, browse, reserve, and queue.
7. Scan all checkout items and take payment.
8. Confirm wallet, ledger, objective, XP, and reputation changes.
9. Hire a restocker and confirm it moves stock from a delivered box.
10. Save, move furniture/change prices, then load and verify restoration.
11. End the day and verify costs, wages, interest, and daily summary.
12. Run **Clerk > Validation > Run Edit Mode Tests**.

If Unity shows `GameObjectInspector m_Targets` or
`SerializedObjectNotCreatableException` immediately after a domain reload,
deselect the destroyed runtime object in the Hierarchy and clear the Console.
Those messages come from Unity's Inspector, not Clerk runtime code.
