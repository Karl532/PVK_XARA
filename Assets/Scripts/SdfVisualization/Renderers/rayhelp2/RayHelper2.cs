using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RayHelperRenderer: MonoBehaviour, ISdfRenderer {
  public Material rayHelperMaterial;
  private GameObject rayHelperObject;
  private Mesh bakedMesh;
  bool hasMesh = false;
  // Depth comparison specific properties 
  bool hasDepth = false; 
  public Material depthComparisonMaterial;
  public DepthFrameData currentDepthData;
  public float errorThreshold = 0.01f; // Colors for depth comparison
  private Settings _settings;
  MeshFilter meshFilter;
  void Start() {
    _settings = Settings.GetActive();
    if (rayHelperMaterial == null) {
      Debug.LogError("The Ray Helper Material field is empty. You must assign the depth comparison material!");
    }
    if (GetComponent<MeshFilter>() != null) hasMesh = true;
    if (hasMesh) {
        bakedMesh = new Mesh();
        rayHelperObject = new GameObject("RayHelper");
        rayHelperObject.transform.SetParent(transform);
        rayHelperObject.transform.localPosition = Vector3.zero;
        rayHelperObject.transform.localScale = new Vector3(1, 1, 1);
        rayHelperObject.transform.localRotation = Quaternion.identity;
        meshFilter = GetComponent<MeshFilter>();

        bakedMesh = BakeMesh(meshFilter.sharedMesh);
        rayHelperObject.AddComponent<MeshRenderer>();
        rayHelperObject.AddComponent<MeshFilter>();
        rayHelperObject.GetComponent<MeshFilter>().sharedMesh = bakedMesh;
        rayHelperObject.GetComponent<MeshRenderer>().material = rayHelperMaterial;
      
    } else {
      Debug.LogError(name + " does not have a mesh!");
    } // Initialize depth comparison material 
    if (depthComparisonMaterial != null) {
      SetupDepthComparisonMaterial();
    }
  }
  private void SetupDepthComparisonMaterial() {
    if (!hasDepth)
    {
        Debug.LogWarning("Tried to Update rayhelper without depth data provided");
        return;
    }
    //if (currentDepthData == null) return; // Set the depth texture and metadata 
    depthComparisonMaterial.SetTexture("_DepthTexture", currentDepthData.DepthTexture);
    depthComparisonMaterial.SetMatrix("_InvDepthViewProj", currentDepthData.InvDepthViewProj);
    depthComparisonMaterial.SetMatrix("_TrackingToWorld", currentDepthData.TrackingToWorld);
    depthComparisonMaterial.SetInt("_EyeSlice", currentDepthData.EyeSlice);
    depthComparisonMaterial.SetInt("_FlipY", currentDepthData.FlipY ? 1 : 0);
    depthComparisonMaterial.SetFloat("_MinDepth01", currentDepthData.MinDepth01);
    depthComparisonMaterial.SetFloat("_MaxDepth01", currentDepthData.MaxDepth01);
    depthComparisonMaterial.SetFloat("_ErrorThreshold", errorThreshold); // Set colors 
  }

  // Completely stolen from wireframe shader
	private Mesh BakeMesh(Mesh originalMesh)
	{
		var maxVerts = 2147483647;
		var meshNor = originalMesh.normals;
		var meshTris = originalMesh.triangles;
		var meshVerts = originalMesh.vertices;		
		var boneW = originalMesh.boneWeights;		
		var vertsNeeded = meshTris.Length;

		if (vertsNeeded > maxVerts)
		{	
			Debug.LogError("The mesh has so many vertices that Unity could not create it!");
			return null;
		}

		var resultMesh = new Mesh();
		resultMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;		
		var resultVerts = new Vector3[vertsNeeded];
		var resultUVs = new Vector2[vertsNeeded];
		var resultTris = new int[meshTris.Length];
		var resultNor = new Vector3[vertsNeeded];
		var boneWLen = (boneW.Length > 0) ? vertsNeeded : 0;
		var resultBW = new BoneWeight[boneWLen]; 
		
		for (var i = 0; i < meshTris.Length; i+=3)
		{
			resultVerts[i] = meshVerts[meshTris[i]];
			resultVerts[i+1] = meshVerts[meshTris[i+1]];
			resultVerts[i+2] = meshVerts[meshTris[i+2]];		
			resultUVs[i] = new Vector2(0f,0f);
			resultUVs[i+1] = new Vector2(1f,0f);
			resultUVs[i+2] = new Vector2(0f,1f);
			resultTris[i] = i;
			resultTris[i+1] = i+1;
			resultTris[i+2] = i+2;
			resultNor[i] = meshNor[meshTris[i]];
			resultNor[i+1] = meshNor[meshTris[i+1]];
			resultNor[i+2] = meshNor[meshTris[i+2]];

			if (resultBW.Length > 0)
			{
				resultBW[i] = boneW[meshTris[i]];
				resultBW[i+1] = boneW[meshTris[i+1]];
				resultBW[i+2] = boneW[meshTris[i+2]];
			}
		}

		resultMesh.vertices = resultVerts;
		resultMesh.uv = resultUVs;
		resultMesh.triangles = resultTris;
		resultMesh.normals = resultNor;
		resultMesh.bindposes = originalMesh.bindposes;
		resultMesh.boneWeights = resultBW;

		return resultMesh;
	}

    // Update depth comparison material if data changed 
    // Used by SDF. If you want to use elsewhere:
    //  UpdateDepthData(DepthFrameData newDepthData) should do same thing
    public void UpdateRenderer(in SdfRendererContext context) 
    { 


        UpdateDepthData(context.DepthFrame);
        if (!hasDepth)
            {
                Debug.LogWarning("Tried to Update rayhelper without depth data provided");
            }
 

  }

  private void OnDestroy() {
    if (bakedMesh != null) Destroy(bakedMesh);
    if (rayHelperObject != null) Destroy(rayHelperObject);
  } 
  // Public method to update depth data 
  public void UpdateDepthData(DepthFrameData newDepthData) {
    hasDepth = true;
    currentDepthData = newDepthData;
    if (depthComparisonMaterial != null) {
      SetupDepthComparisonMaterial();
    }
  } // Public method to update error threshold 
  public void UpdateErrorThreshold(float newThreshold) {
    errorThreshold = newThreshold;
    if (depthComparisonMaterial != null) {
      depthComparisonMaterial.SetFloat("_ErrorThreshold", errorThreshold);
    }
  }

}