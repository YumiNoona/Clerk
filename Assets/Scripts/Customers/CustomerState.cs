public enum CustomerState
{
    None,

    Spawning,
    MovingToEntrance,
    WaitingAtEntrance,

    Shopping,
    MovingToCheckout,
    WaitingInCheckoutQueue,
    CheckingOut,

    MovingToExit,
    Despawning
}