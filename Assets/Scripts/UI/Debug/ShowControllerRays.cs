using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ShowControllerRays : MonoBehaviour
{
    void Start()
    {
        // Wait a frame for everything to initialize
        StartCoroutine(SetupRays());
    }
    
    System.Collections.IEnumerator SetupRays()
    {
        yield return new WaitForSeconds(0.5f);
        
        var rayInteractors = FindObjectsOfType<XRRayInteractor>();
        Debug.Log($"Found {rayInteractors.Length} XR Ray Interactors");
        
        foreach (var interactor in rayInteractors)
        {
            Debug.Log($"Setting up ray on: {interactor.gameObject.name}");
            
            // Enable the interactor
            interactor.enabled = true;
            interactor.allowHover = true;
            interactor.allowSelect = true;
            
            // Add LineRenderer for visual ray
            LineRenderer line = interactor.GetComponent<LineRenderer>();
            if (line == null)
            {
                line = interactor.gameObject.AddComponent<LineRenderer>();
                Debug.Log($"Added LineRenderer to {interactor.gameObject.name}");
            }
            
            line.enabled = true;
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.positionCount = 2;
            
            // Create bright cyan material
            Material lineMat = new Material(Shader.Find("Unlit/Color"));
            lineMat.color = Color.cyan;
            line.material = lineMat;
            
            line.startColor = Color.cyan;
            line.endColor = Color.cyan;
            
            // Add XR Interactor Line Visual
            UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual lineVisual = interactor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
            if (lineVisual == null)
            {
                lineVisual = interactor.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
                Debug.Log($"Added XRInteractorLineVisual to {interactor.gameObject.name}");
            }
            
            lineVisual.enabled = true;
        }
    }
}