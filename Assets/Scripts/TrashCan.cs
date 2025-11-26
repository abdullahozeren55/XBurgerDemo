using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCan : MonoBehaviour
{
    [SerializeField] private AudioClip trashToCanSound;
    [SerializeField] private float trashToCanVolume = 1f;
    [SerializeField] private float trashToCanMinPitch = 0.85f;
    [SerializeField] private float trashToCanMaxPitch = 1.15f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Trash") ||
            other.gameObject.CompareTag("BurgerBox") ||
            other.gameObject.CompareTag("BurgerBoxClosed") ||
            other.gameObject.CompareTag("BurgerIngredient") ||
            other.gameObject.CompareTag("Knife") ||
            other.gameObject.CompareTag("WholeIngredient") ||
            other.gameObject.CompareTag("FoodPack") ||
            other.gameObject.CompareTag("Drink"))
        {
            SoundManager.Instance.PlaySoundFX(trashToCanSound, transform, trashToCanVolume, trashToCanMinPitch, trashToCanMaxPitch);
            Destroy(other.gameObject);
        }
    }
}
