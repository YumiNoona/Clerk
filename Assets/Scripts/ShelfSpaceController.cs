using UnityEngine;

public class ShelfSpaceController : MonoBehaviour
{
    public StockInfo Info;

    public int AmountOnShelf;

    public void PlaceStock(StockObject ObjectToPlace)
    {

        bool PreventPlacing = true;

        if (AmountOnShelf == 0)
        {
            Info = ObjectToPlace.Info;
            PreventPlacing = false;
        }
        else
        {
            if (Info.Name == ObjectToPlace.Info.Name)
            {
                PreventPlacing = false;
            }
        }

        if (PreventPlacing == false)
        {
            ObjectToPlace.transform.SetParent(transform);
            ObjectToPlace.MakePlaced();

            AmountOnShelf += 1;
        }
    }

    public StockObject GetStock()
    {
        StockObject ObjectToReturn = null;

        return ObjectToReturn;
    }
}