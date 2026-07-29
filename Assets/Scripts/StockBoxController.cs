using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StockBoxController : MonoBehaviour
{
    //[Header("Product")]
    public StockInfo Product;

    //[Header("Box Layout")]
    public BoxLayout Layout;

    //[Header("Quantity")]
    public int Quantity = 12;

    //[Header("Components")]
    public Rigidbody TheRB;
    public Collider BoxCollider;

    //[Header("Flaps")]
    public Transform LeftFlapPivot;
    public Transform RightFlapPivot;

    public Vector3 LeftFlapClosedRotation = Vector3.zero;
    public Vector3 LeftFlapOpenRotation = new Vector3(0f,0f,110f);

    public Vector3 RightFlapClosedRotation = Vector3.zero;
    public Vector3 RightFlapOpenRotation = new Vector3(0f,0f,-110f);

    public float FlapAnimationSpeed = 5f;

    //[Header("Runtime Contents")]
    public bool ShowRuntimeContents = true;
    public Transform ContentOrigin;
    public float RuntimeContentsRevealDelay = 0.15f;

    [Header("Label")]
    public TMP_Text ProductNameLabel;
    public TMP_Text QuantityLabel;

    [HideInInspector]
    public bool ShowEditorPreview = true;

#if UNITY_EDITOR
    [HideInInspector]
    public bool ShowLayoutGizmos = true;

    [HideInInspector]
    public Transform EditorPreviewRoot;
#endif

    private readonly List<StockObject> runtimePreviewObjects = new List<StockObject>();

    private Transform runtimePreviewRoot;
    private bool isOpen;
    private bool isHeld;

    private Coroutine flapRoutine;
    private Coroutine runtimePreviewRoutine;

    public bool IsOpen
    {
        get
        {
            return isOpen;
        }
    }

    public bool IsHeld
    {
        get
        {
            return isHeld;
        }
    }

    public int MaximumQuantity
    {
        get
        {
            return Layout != null ? Layout.Capacity : 0;
        }
    }

    private void Awake()
    {
        if (TheRB == null)
        {
            TheRB = GetComponent<Rigidbody>();
        }

        if (BoxCollider == null)
        {
            BoxCollider = GetComponent<Collider>();
        }

        if (Layout == null && Product != null)
        {
            Layout = Product.DefaultBoxLayout;
        }

        ClampQuantity();
        SetFlapsImmediate(false);
        ClearRuntimeContents();
        UpdateLabels();
    }

    public void Pickup(Transform holdPoint)
    {
        if (holdPoint == null)
        {
            return;
        }

        isHeld = true;

        transform.SetParent(holdPoint,true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (TheRB != null)
        {
            if (!TheRB.isKinematic)
            {
                TheRB.linearVelocity = Vector3.zero;
                TheRB.angularVelocity = Vector3.zero;
            }

            TheRB.isKinematic = true;
        }

        if (BoxCollider != null)
        {
            BoxCollider.enabled = false;
        }
    }

    public void Release()
    {
        isHeld = false;
        SetOpen(false);
        transform.SetParent(null,true);

        if (TheRB != null)
        {
            TheRB.isKinematic = false;
            TheRB.linearVelocity = Vector3.zero;
            TheRB.angularVelocity = Vector3.zero;
        }

        if (BoxCollider != null)
        {
            BoxCollider.enabled = true;
        }
    }

    public void ToggleOpen()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open)
        {
            return;
        }

        isOpen = open;

        if (flapRoutine != null)
        {
            StopCoroutine(flapRoutine);
        }

        flapRoutine = StartCoroutine(AnimateFlaps());

        if (runtimePreviewRoutine != null)
        {
            StopCoroutine(runtimePreviewRoutine);
            runtimePreviewRoutine = null;
        }

        if (isOpen && ShowRuntimeContents)
        {
            runtimePreviewRoutine = StartCoroutine(RevealRuntimeContents());
        }
        else
        {
            ClearRuntimeContents();
        }
    }

    public bool TryStockShelf(ShelfSpaceController shelf)
    {
        if (!isOpen)
        {
            return false;
        }

        if (shelf == null || Product == null || Product.StockPrefab == null || Layout == null || Quantity <= 0)
        {
            return false;
        }

        Transform contentOrigin = GetContentOrigin();

        if (contentOrigin == null)
        {
            return false;
        }

            int sourceIndex = Mathf.Clamp(Quantity - 1,0,Layout.Capacity - 1);

            StockObject newStock = Instantiate(Product.StockPrefab,contentOrigin);
            newStock.Info = Product;
            newStock.transform.localPosition = Layout.GetLocalPosition(sourceIndex);
            newStock.transform.localRotation = Quaternion.Euler(Layout.LocalRotation);

            Vector3 startingWorldPosition = newStock.transform.position;
            Quaternion startingWorldRotation = newStock.transform.rotation;

            newStock.transform.SetParent(null,true);
            newStock.transform.SetPositionAndRotation(startingWorldPosition,startingWorldRotation);

            bool wasPlaced = shelf.PlaceStock(newStock);

        if (!wasPlaced)
        {
            Destroy(newStock.gameObject);
            return false;
        }

        Quantity--;

        if (ShowRuntimeContents)
        {
            RefreshRuntimeContents();
        }

        UpdateLabels();

        return true;
    }

    private IEnumerator AnimateFlaps()
    {
        Quaternion leftTarget = Quaternion.Euler(isOpen ? LeftFlapOpenRotation : LeftFlapClosedRotation);
        Quaternion rightTarget = Quaternion.Euler(isOpen ? RightFlapOpenRotation : RightFlapClosedRotation);

        while (true)
        {
            bool leftFinished = true;
            bool rightFinished = true;

            if (LeftFlapPivot != null)
            {
                LeftFlapPivot.localRotation = Quaternion.Slerp(LeftFlapPivot.localRotation,leftTarget,FlapAnimationSpeed * Time.deltaTime);
                leftFinished = Quaternion.Angle(LeftFlapPivot.localRotation,leftTarget) < 0.2f;
            }

            if (RightFlapPivot != null)
            {
                RightFlapPivot.localRotation = Quaternion.Slerp(RightFlapPivot.localRotation,rightTarget,FlapAnimationSpeed * Time.deltaTime);
                rightFinished = Quaternion.Angle(RightFlapPivot.localRotation,rightTarget) < 0.2f;
            }

            if (leftFinished && rightFinished)
            {
                break;
            }

            yield return null;
        }

        if (LeftFlapPivot != null)
        {
            LeftFlapPivot.localRotation = leftTarget;
        }

        if (RightFlapPivot != null)
        {
            RightFlapPivot.localRotation = rightTarget;
        }

        flapRoutine = null;
    }

    private IEnumerator RevealRuntimeContents()
    {
        yield return new WaitForSeconds(RuntimeContentsRevealDelay);

        if (isOpen && ShowRuntimeContents)
        {
            RefreshRuntimeContents();
        }

        runtimePreviewRoutine = null;
    }

    private void RefreshRuntimeContents()
    {
        ClearRuntimeContents();

        if (!ShowRuntimeContents || !isOpen)
        {
            return;
        }

        if (Layout == null || Product == null || Product.StockPrefab == null || Quantity <= 0)
        {
            return;
        }

        Transform parent = GetContentOrigin();
        runtimePreviewRoot = new GameObject("_RUNTIME_BOX_CONTENTS").transform;
        runtimePreviewRoot.SetParent(parent,false);

        int previewCount = Mathf.Min(Quantity,Layout.MaximumRuntimePreviewObjects,Layout.Capacity);

        for (int i = 0; i < previewCount; i++)
        {
            StockObject previewObject = Instantiate(Product.StockPrefab,runtimePreviewRoot);
            previewObject.Info = Product;
            previewObject.transform.localPosition = Layout.GetLocalPosition(i);
            previewObject.transform.localRotation = Quaternion.Euler(Layout.LocalRotation);
            previewObject.SetAsBoxPreview();

            runtimePreviewObjects.Add(previewObject);
        }
    }

    private void ClearRuntimeContents()
    {
        for (int i = runtimePreviewObjects.Count - 1; i >= 0; i--)
        {
            if (runtimePreviewObjects[i] != null)
            {
                Destroy(runtimePreviewObjects[i].gameObject);
            }
        }

        runtimePreviewObjects.Clear();

        if (runtimePreviewRoot != null)
        {
            Destroy(runtimePreviewRoot.gameObject);
            runtimePreviewRoot = null;
        }
    }

    private Transform GetContentOrigin()
    {
        if (ContentOrigin != null)
        {
            return ContentOrigin;
        }

        return transform;
    }

    private void UpdateLabels()
    {
        if (ProductNameLabel != null)
        {
            ProductNameLabel.text = Product != null ? Product.ProductName : "Empty";
        }

        if (QuantityLabel != null)
        {
            QuantityLabel.text = Quantity + " / " + MaximumQuantity;
        }
    }

    private void SetFlapsImmediate(bool open)
    {
        if (LeftFlapPivot != null)
        {
            LeftFlapPivot.localRotation = Quaternion.Euler(open ? LeftFlapOpenRotation : LeftFlapClosedRotation);
        }

        if (RightFlapPivot != null)
        {
            RightFlapPivot.localRotation = Quaternion.Euler(open ? RightFlapOpenRotation : RightFlapClosedRotation);
        }
    }

    private void ClampQuantity()
    {
        if (Layout == null)
        {
            Quantity = Mathf.Max(0,Quantity);
            return;
        }

        Quantity = Mathf.Clamp(Quantity,0,Layout.Capacity);
    }

    private void OnValidate()
    {
        if (Layout == null && Product != null)
        {
            Layout = Product.DefaultBoxLayout;
        }

        FlapAnimationSpeed = Mathf.Max(0.01f,FlapAnimationSpeed);
        RuntimeContentsRevealDelay = Mathf.Max(0f,RuntimeContentsRevealDelay);

        ClampQuantity();
    }

    private void OnDestroy()
    {
        ClearRuntimeContents();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!ShowEditorPreview || !ShowLayoutGizmos)
        {
            return;
        }

        BoxLayout activeLayout = Layout;

        if (activeLayout == null && Product != null)
        {
            activeLayout = Product.DefaultBoxLayout;
        }

        if (activeLayout == null)
        {
            return;
        }

        Transform origin = ContentOrigin != null ? ContentOrigin : transform;

        Gizmos.matrix = origin.localToWorldMatrix;

        for (int i = 0; i < activeLayout.Capacity; i++)
        {
            Gizmos.DrawWireCube(activeLayout.GetLocalPosition(i),new Vector3(0.06f,0.06f,0.06f));
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}