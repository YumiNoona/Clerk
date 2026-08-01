# Clerk — Project Guide

First-person store simulator built with Unity `6000.5.5f1`.

Playable loop:

`order → deliver → stock → customer shops → queue → scan → collect payment → exit`

For the visual version of this guide, open **Clerk → Project Guide** inside
Unity. That window includes scene setup, parameter explanations, content
creation, performance rules, and troubleshooting.

## Run the project

Open `Assets/Scenes/Street.unity`, press Play, start the store, and use the
desktop interface for purchases, banking, history, tasks, settings and saves.

`GameBootstrap` creates `[Clerk Runtime]` automatically before a scene loads.
Never add that object or its service components manually.

## Create a new gameplay scene

1. Build or import the environment and player.
2. Add a `NavMeshSurface` covering customer-accessible ground.
3. Run **Clerk → Setup → Create Modular Store Configuration**.
4. Select `Store Configuration` and move its colored Scene-view handles.
5. Assign the checkout reference on `StoreSceneConfiguration`.
6. Add shelves and a `FurniturePlacementArea`.
7. Bake the NavMesh and test the complete customer loop.

The setup command creates one configuration object containing purchasing,
customer/pedestrian authoring, stock delivery and furniture delivery. It also
creates the authored desktop UI and the single EventSystem. You do not need a
hierarchy of spawn, entrance, queue, exit or delivery empties.

## Scene authoring components

### StoreSceneConfiguration

This is the only customer/pedestrian point-authoring component.

| Parameter | Meaning |
|---|---|
| Customer Spawns | Any number of customer creation poses. |
| Weight | Relative selection probability. `80` and `20` gives roughly an 80/20 split; values do not need to total 100. |
| Radius | Random NavMesh offset around a spawn. Use `0` for exact placement or `1–2` metres to spread crowds. |
| Entrance Wait | Position outside the door plus randomized pause duration. |
| Inside Point | First destination inside the store, preventing doorway blockage. |
| Checkout | CheckoutCounter used by the configured queue and clerk point. |
| Checkout Clerk Point | Where the player must stand to operate checkout. |
| Clerk Radius | Maximum permitted player distance from that point. |
| Checkout Queue | Ordered customer positions; element zero is served first. |
| Exit / Despawn | Route out of the store and final removal position. |
| Pedestrian Track | At least two NavMesh-reachable points and two metres total length. |

Click a colored sphere directly in the Scene view to select and move it. Use
the inspector buttons to add customer spawns, queue positions, or pedestrian
track points.

### StoreDeliveryConfiguration

Stores both stock-box and furniture delivery poses. The services consume these
poses directly; no authored or runtime delivery-point empties are required.

## Create content

All project assets live beneath one menu: **Assets → Create → Clerk**.

| Menu | Creates |
|---|---|
| Products/Product | Product identity, category, initial price, prefab and default box layout. |
| Products/Category | Reusable product category. |
| Products/Box Layout | Local product-preview positions inside a box. |
| Products/Purchase Entry | Box prefab, quantity, purchase price and unlock level shown in the Store app's Product Stock tab. |
| Furniture/Purchase Entry | Furniture prefab, price and unlock requirement shown in the Store app's Furniture tab. |
| Customers/Definition | Customer prefab, weight, patience and shopping behaviour. |
| Customers/Database | Collection used by customers and pedestrians. |
| Customers/Mood Catalog | Six mood textures shared by customer mood presenters. |
| Store/Purchase Catalog | Stock and furniture entries available to purchasing. |
| Employees/Definition | Role, prefab, hiring cost, wage and work timing. |
| Objectives/Definition | Tracked event, target, money reward and XP reward. |

After creating a product purchase entry, add it to the Purchase Catalog. After
creating a customer definition, add it to the Customer Database.

## Required physical prefabs

- Product: collider, Rigidbody, Stock layer and `StockObject`.
- Box: collider, Rigidbody, Stock Box layer and `StockBoxController`.
- Shelf: `PlaceableFurniture`; each stockable section uses
  `ShelfSpaceController` and a customer standing point.
- Customer: `NavMeshAgent`, `CustomerNavigation`, `CustomerAnimator` and visual
  variation. Context/brain adapters are supplied when absent.
- Checkout: collider, `CheckoutCounter`, serving position and adequate queue
  clearance.
- Garbage bin: collider on Garbage Bin layer plus `GarbageBin`.

## Architecture and performance rules

- ScriptableObjects are immutable definitions. Runtime prices and state live
  in services and save data.
- `[Clerk Runtime]` is the single persistent composition root.
- UI shells are authored in the scene. Only variable catalog/history rows are
  populated at runtime.
- Shelves, customers and checkouts register with registries. Do not perform
  scene-wide searches every frame.
- Delivery poses are direct configuration data. Customer route adapters are
  created once and hidden, then reused for the scene lifetime.
- Mood sprites are cached and shared; mood evaluation is throttled.
- Customer and pedestrian NavMesh agents are the main scalable CPU cost.
- Add gameplay content through definitions/catalogs rather than hard-coding it
  into UI or customer logic.

## Common problems

### Customers do not spawn

Open the store, assign Customer Database, verify spawn/entrance/inside points
touch the baked NavMesh, and make sure the active-customer limit is not full.

### Pedestrians do not spawn

Provide at least two separated pedestrian points on the baked NavMesh.

### Customers get stuck

Widen entrances, separate spawn/queue positions, keep shelf standing points
clear, and rebake after changing geometry.

### Checkout cannot be used

Stand within Clerk Radius at the configured clerk point, face the checkout,
scan all basket items, and only then collect payment.

### UI cannot be clicked

Confirm the scene contains exactly one EventSystem. Select the UI object and
rebuild authored UI if its `StoreUIAuthoring` references are incomplete.

## Validation

Run **Clerk → Validation → Run Edit Mode Tests**, then manually verify:

1. Purchase and receive a stock box.
2. Stock a shelf.
3. Open the store and serve a customer.
4. Verify wallet and transaction history.
5. Close the store and ensure no new customers enter.
6. Save, change state, load, and verify restoration.
