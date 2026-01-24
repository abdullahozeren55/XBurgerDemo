using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DiningTable : MonoBehaviour
{
    [Header("Assignments")]
    public List<Seat> SeatsAroundTable; // Bu masaya ait sandalyeler/koltuklar

    // Bu masa þu an bir grup tarafýndan tutuldu mu?
    public bool IsTableReserved { get; private set; } = false;

    // Masanýn toplam kapasitesi (Sandalye x 1 + Koltuk x 2 ...)
    public int TotalCapacity
    {
        get { return SeatsAroundTable.Sum(s => s.sitPoints.Count); }
    }

    // --- GRUP YÖNETÝMÝ ---

    // Bu masa, gelen grup boyutu için uygun mu?
    public bool CanAccommodateGroup(int groupSize)
    {
        if (IsTableReserved) return false; // Zaten dolu
        if (TotalCapacity < groupSize) return false; // Yer yetmez

        return true;
    }

    // Masayý rezerve et
    public void ReserveTable()
    {
        IsTableReserved = true;
    }

    // Müþteriye masadan rastgele boþ bir koltuk/nokta ver
    public Transform GetSeatForCustomer(ICustomer customer)
    {
        foreach (var seat in SeatsAroundTable)
        {
            // Koltukta yer var mý?
            if (seat.TryOccupy(customer, out Transform sitPoint))
            {
                return sitPoint;
            }
        }
        return null; // Yer kalmadý (Ama mantýken rezerve ederken kontrol ettik, kalmalý)
    }

    // Masa tamamen boþaldý mý kontrol et (Her müþteri kalktýðýnda çaðýrýlýr)
    public void CheckIfTableIsEmpty()
    {
        bool anyoneSitting = false;
        foreach (var seat in SeatsAroundTable)
        {
            // Seat scriptine "occupants.Count > 0" kontrolü eklenebilir veya
            // IsFullyOccupied mantýðýna bakýlýr ama en temizi:
            // Seat içinde Occupant count'a bakmaktýr.
            // (Basitlik için Seat.cs'e public OccupantCount eklediðini varsayýyorum)
            // Þimdilik basit mantýk:
            if (seat.IsFullyOccupied) anyoneSitting = true;
            // (Not: IsFullyOccupied tam doðru deðil, 1 kiþi varsa da oturuyordur. 
            // Seat.cs'e 'public bool HasAnyOccupant => occupants.Count > 0;' ekle.)
        }

        // Hýzlý çözüm için Seat'e eriþim
        // Eðer kimse yoksa rezervasyonu kaldýr
        // (Bunu SeatManager üzerinden yönetmek daha saðlýklý olabilir)
    }

    // Masayý temizle (Grup gitti)
    public void ReleaseTable()
    {
        IsTableReserved = false;
        // Ýsteðe baðlý: Masadaki kirli tepsileri iþaretle
    }
}