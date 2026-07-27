using UnityEngine;

public class StockObject : MonoBehaviour
{
    public StockInfo Info;

    public float MoveSpeed;

    public bool IsPlaced;

    public Rigidbody TheRB;
    public Collider Col;

    void Start()
    {

    }

    void Update()
    {
        if (IsPlaced == true)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                Vector3.zero,
                MoveSpeed * Time.deltaTime
            );

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                Quaternion.identity,
                MoveSpeed * Time.deltaTime
            );
        }
    }

    public void Pickup()
    {
        TheRB.isKinematic = true;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        IsPlaced = false;

        Col.enabled = false;
    }

    public void MakePlaced()
    {
        TheRB.isKinematic = true;

        IsPlaced = true;

        Col.enabled = false;
    }

    public void Release()
    {
        TheRB.isKinematic = false;

        Col.enabled = true;
    }
} 