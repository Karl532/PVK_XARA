using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DebugXRClicks : MonoBehaviour
{
    void Start()
    {
        var interactors = FindObjectsOfType<XRRayInteractor>();

        foreach (var interactor in interactors)
        {
            interactor.selectEntered.AddListener((args) =>
            {
                Debug.Log($"SELECT ENTERED on {args.interactableObject}");
            });

            interactor.hoverEntered.AddListener((args) =>
            {
                Debug.Log($"HOVER ENTERED on {args.interactableObject}");
            });
        }
    }
}