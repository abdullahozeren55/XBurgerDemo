using UnityEngine;

public interface ICustomer
{
    // Müþterinin o anki durumu (Animasyonlar ve Logic için)
    CustomerState CurrentState { get; }

    // Dýþarýdan profili enjekte etmek için (Spawner kullanacak)
    void Initialize(CustomerProfile profile);

    // Oyuncu tepsiyi uzattýðýnda çaðrýlacak
    bool TryReceiveTray(Tray tray);

    // Korku elementi: Müþteriyi korkutmak veya etkileþime girmek için
    void OnScareEvent();
}

public enum CustomerState
{
    Entering,       // Kapýdan giriyor
    WaitingInLine,  // Sýrada (Opsiyonel)
    AtCounter,      // Kasada, sipariþ vermeye hazýr
    Ordering,       // Diyalog halinde
    WaitingForFood, // Kenara çekildi, tepsi bekliyor (Idle + Stare)
    MovingToSeat,
    Eating,         // Masada
    Leaving         // Gidiyor
}