using UnityEditor;
using UnityEngine;

public sealed class ClerkProjectGuideWindow : EditorWindow
{
    private readonly string[] tabs =
        { "Start", "Scene", "Content", "Parameters", "Performance", "Fixes" };
    private int tab;
    private Vector2 scroll;

    [MenuItem("Clerk/Project Guide",priority = 0)]
    public static void Open()
    {
        ClerkProjectGuideWindow window = GetWindow<ClerkProjectGuideWindow>();
        window.titleContent = new GUIContent("Clerk Guide");
        window.minSize = new Vector2(620f,520f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("CLERK PROJECT GUIDE",EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Unity 6000.5.5f1  |  Desktop-first store simulator",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(6f);
        tab = GUILayout.Toolbar(tab,tabs);
        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        switch (tab)
        {
            case 0: DrawStart(); break;
            case 1: DrawScene(); break;
            case 2: DrawContent(); break;
            case 3: DrawParameters(); break;
            case 4: DrawPerformance(); break;
            default: DrawFixes(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawStart()
    {
        Title("Fast start");
        Note("Open Assets/Scenes/Street.unity and press Play. The playable loop is: order, deliver, stock, shop, queue, scan, collect payment, exit.");
        Title("Create a new gameplay scene");
        Step("1", "Build/import the environment and player, then add a NavMeshSurface.");
        Step("2", "Run Clerk > Setup > Create Modular Store Configuration.");
        Step("3", "Select Store Configuration and move its colored point handles.");
        Step("4", "Assign the Checkout field and bake the NavMesh.");
        Step("5", "Add shelves, a furniture placement area, and a garbage bin if required.");
        if (GUILayout.Button("Create Modular Store Configuration",GUILayout.Height(32f)))
        {
            StorePointHierarchyMigration.CreateModularStoreConfiguration();
        }
        if (GUILayout.Button("Open Project README",GUILayout.Height(26f)))
        {
            Object readme = AssetDatabase.LoadAssetAtPath<Object>("README.md");
            if (readme != null) AssetDatabase.OpenAsset(readme);
        }
    }

    private static void DrawScene()
    {
        Title("Only two point-authoring components");
        Field("StoreSceneConfiguration", "Customer spawns, entrance wait, inside point, clerk position, queue, exit, despawn, and pedestrian track. Points are stored as data; no authored point empties are needed.");
        Field("StoreDeliveryConfiguration", "Stock and furniture delivery poses. Delivery services consume the poses directly without runtime delivery empties.");
        Title("Still required because they are physical scene content");
        Field("Player", "CharacterController, PlayerController, PlayerInteractionController, camera, HoldPoint and BoxPoint.");
        Field("Checkout", "A CheckoutCounter model plus its interaction/clerk area.");
        Field("NavMesh", "Bake every walkable customer/pedestrian area after moving walls, doors or permanent fixtures.");
        Field("FurniturePlacementArea", "The only required store zone. It defines where purchased furniture may be placed.");
        Note("[Clerk Runtime] is created automatically. Never add it to a scene.");
    }

    private static void DrawContent()
    {
        Title("Create menu");
        Field("Clerk/Products/Product", "Immutable product definition: name, category, initial price, prefab and default box layout.");
        Field("Clerk/Products/Purchase Entry", "What the desktop supply app sells: box prefab, quantity, price and unlock level.");
        Field("Clerk/Products/Box Layout", "Local positions used to preview products inside a box.");
        Field("Clerk/Furniture/Purchase Entry", "Furniture prefab, price and unlock requirements.");
        Field("Clerk/Customers/Definition", "Prefab, spawn weight, shopping behavior, patience and movement ranges.");
        Field("Clerk/Customers/Database", "List of customer definitions available to spawners and pedestrians.");
        Field("Clerk/Objectives/Definition", "Tracked event, target, money reward and XP reward.");
        Field("Clerk/Employees/Definition", "Role, prefab, hiring price, wage, speed and work interval.");
        Note("After creating purchase data, add it to the Purchase Catalog. After creating a customer, add it to the Customer Database.");
    }

    private static void DrawParameters()
    {
        Title("Scene configuration");
        Field("Spawn Weight", "Relative chance of choosing a spawn. 80 and 20 means approximately 80% versus 20%; values do not need to total 100.");
        Field("Spawn Radius", "Random NavMesh offset around the spawn pose. Use 0 for an exact point; 1-2 prevents crowd overlap.");
        Field("Entrance Wait Min/Max", "Random pause outside the entrance before moving to Inside Point.");
        Field("Checkout Clerk Radius", "How close the player must stand to the configured clerk point to operate checkout.");
        Field("Checkout Queue", "Ordered standing positions. Element 0 is served first; later elements extend away from the counter.");
        Field("Pedestrian Track", "At least two NavMesh-reachable points with at least two metres total length. Pedestrians travel back and forth.");
        Title("Customer definition");
        Field("Spawn Weight", "Relative likelihood of selecting this customer appearance/type.");
        Field("Patience", "Total waiting budget. Mood falls as remaining patience drops; efficient checkout can improve the final result.");
        Field("Shopping/Browse ranges", "Randomized quantities and delays; keep minimum less than or equal to maximum.");
        Title("Shelf and checkout");
        Field("Shelf capacity/layout", "Controls physical product slots. Customer standing position must be on the NavMesh in front of the shelf.");
        Field("Queue capacity", "Maximum customers owned by one checkout. Supply enough queue poses or use fallback spacing.");
    }

    private static void DrawPerformance()
    {
        Title("Performance rules used by the project");
        Field("Authored UI", "Canvas, menus and desktop shells exist in the scene. Only variable catalog/history rows are populated at runtime.");
        Field("Registries", "Shelves, customers and checkouts register once. Avoid FindObjects calls inside Update.");
        Field("Point data", "Delivery poses are direct data. Customer route adapters are created once and hidden, not searched or rebuilt per frame.");
        Field("Mood icons", "Six sprites are cached globally; customers reuse them and update mood at a throttled interval.");
        Field("NavMesh agents", "Customer count and pedestrian count are the largest scalable CPU costs. Reduce them before lowering visual quality.");
        Note("Do not add runtime UI builders or per-frame scene searches for convenience. Add authored references or register components with an existing service.");
    }

    private static void DrawFixes()
    {
        Title("Nothing spawns");
        Field("Check", "Store is open, Customer Database is assigned, spawn/entrance/inside points touch the baked NavMesh, and the pedestrian track has two separated points.");
        Title("Customers get stuck");
        Field("Check", "Widen doorways and queue spacing, rebake NavMesh, keep shelf standing positions clear, and avoid overlapping spawn radii.");
        Title("Checkout cannot be operated");
        Field("Check", "Stand within Checkout Clerk Radius, face the counter, verify the checkout collider/layer, and scan every basket item before collecting payment.");
        Title("UI cannot be clicked");
        Field("Check", "There must be exactly one EventSystem. Rebuild authored UI from the StoreUIAuthoring inspector if references are missing.");
        Title("Missing scripts or duplicate UI");
        Field("Check", "Exit Play mode, allow compilation to finish, then reopen the scene. Never save runtime-created objects back into the scene.");
        if (GUILayout.Button("Run Edit Mode Tests",GUILayout.Height(30f)))
        {
            ClerkTestRunner.RunEditModeTests();
        }
    }

    private static void Title(string text)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(text,EditorStyles.boldLabel);
    }

    private static void Step(string number,string text)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(number,EditorStyles.boldLabel,GUILayout.Width(22f));
        EditorGUILayout.LabelField(text,EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(3f);
    }

    private static void Field(string name,string description)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(name,EditorStyles.boldLabel);
        EditorGUILayout.LabelField(description,EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
    }

    private static void Note(string text)
    {
        EditorGUILayout.HelpBox(text,MessageType.Info);
    }
}
