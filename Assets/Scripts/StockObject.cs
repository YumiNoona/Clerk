using UnityEngine;

public class StockObject : MonoBehaviour
{
    [Header("Stock Information")]
    public StockInfo Info;

    [Header("Placement")]
    public float MoveSpeed = 10f;
    public bool IsPlaced;

    [Header("Components")]
    public Rigidbody TheRB;
    public MeshCollider MeshCollider;

    private Vector3 targetLocalPosition;
    private Quaternion targetLocalRotation = Quaternion.identity;

    private void Awake()
    {
        if (TheRB == null)
        {
            TheRB = GetComponent<Rigidbody>();
        }

        if (MeshCollider == null)
        {
            MeshCollider = GetComponentInChildren<MeshCollider>();
        }
    }

    private void Update()
    {
        if (!IsPlaced)
        {
            return;
        }

        transform.localPosition = Vector3.MoveTowards(transform.localPosition,targetLocalPosition,MoveSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation,targetLocalRotation,MoveSpeed * Time.deltaTime);
    }

    public void Pickup()
    {
        IsPlaced = false;

        if (TheRB != null)
        {
            if (!TheRB.isKinematic)
       
        {
            TheRB.linearVelocity = Vector3.zero;
            TheRB.angularVelocity = Vector3.zero;
        }
            
            TheRB.isKinematic = true;

        }

        if (MeshCollider != null)
        {
            MeshCollider.enabled = false;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void MakePlaced(Vector3 localPosition,Quaternion localRotation)
    {
        targetLocalPosition = localPosition;
        targetLocalRotation = localRotation;
        IsPlaced = true;

        if (TheRB != null)
        {
            if (!TheRB.isKinematic)
        
        {
            TheRB.linearVelocity = Vector3.zero;
            TheRB.angularVelocity = Vector3.zero;
        }
            
            TheRB.isKinematic = true;

        }
        
        if (MeshCollider != null)
        {
            MeshCollider.enabled = false;
        }
    }

    public void Release()
    {
        IsPlaced = false;
        transform.SetParent(null,true);

        if (TheRB != null)
        {
            TheRB.isKinematic = false;
            TheRB.linearVelocity = Vector3.zero;
            TheRB.angularVelocity = Vector3.zero;
        }

        if (MeshCollider != null)
        {
            MeshCollider.enabled = true;
        }
    }
}