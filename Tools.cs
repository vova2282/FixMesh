using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public class Tools : MonoBehaviour
{
	[MenuItem("Tools/More/Mesh Fix")]
    static void FixMeshes(MenuCommand command)
    {
        for (int i = 0; i < GameObject.FindObjectsOfType<MeshFilter>().Length; i++)
        {
            if (GameObject.FindObjectsOfType<MeshFilter>()[i].gameObject.GetComponent<MeshCollider>() != null)
            {
                GameObject.FindObjectsOfType<MeshFilter>()[i].mesh = GameObject.FindObjectsOfType<MeshFilter>()[i].gameObject.GetComponent<MeshCollider>().sharedMesh;
            } 
        }
    }
	
	[MenuItem("Tools/More/Combined Mesh Fix")]
	static void Init()
	{
		if (Selection.activeGameObject == null)
		{
			Debug.LogError("No selected object.");
			return;
		}
		
		ApplyTransformRecursive(Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab));
	}
	
	public static void ApplyTransformRecursive(Transform[] transforms)
	{
		foreach (Transform transform in transforms)
		{
			ApplyTransformRecursive(transform);
		}
	}
	
	public static void ApplyTransformRecursive(Transform transform)
	{
		ApplyTransform(transform);
		
		foreach (Transform child in transform)
		{
			ApplyTransformRecursive(child);
		}
	}	
	
	public static void ApplyTransform(Transform[] transforms)
	{
		bool[] applied = new bool[transforms.Length];
		for (int i = 0; i < applied.Length; ++i) applied[i] = false;

		int applyCount = 0;

		while (applyCount != transforms.Length)
		{
			for (int i = 0; i < transforms.Length; ++i)
			{
				if (!applied[i])
				{
					bool canApply = true;

					for (int j = 0; j < transforms.Length; ++j)
					{
						if (i == j) continue;

						if (transforms[i].IsChildOf(transforms[j]) && !applied[j])
						{
							canApply = false;
							break;
						}
					}

					if (canApply)
					{
						ApplyTransform(transforms[i]);
						applied[i] = true;
						Debug.Log("Applied transform to " + transforms[i].name);
						++applyCount;
					}
				}
			}
		}
	}
	
	public static void ApplyTransform(Transform transform)
	{
		var meshFilter = transform.GetComponent<MeshFilter>();
		var meshRenderer = transform.GetComponent<MeshRenderer>();
		
		if (meshFilter == null || meshRenderer == null)
		{
			return;
		}
		
		if (meshRenderer.isPartOfStaticBatch != true)
		{
			return;
		}
		
		if (meshFilter.sharedMesh == null)
		{
			return;
		}
		
		if (!meshFilter.sharedMesh.name.StartsWith("Combined Mesh (root scene)"))
		{
			return;
		}
		
		Debug.Log("Unbaking mesh for object (" + transform.name + ").");
		var originalMeshName = meshFilter.sharedMesh.name;

		var newMesh = Instantiate(meshFilter.sharedMesh);

		var totalsubmeshcount = newMesh.subMeshCount > meshRenderer.sharedMaterials.Length ? meshRenderer.sharedMaterials.Length : newMesh.subMeshCount;
		for (int i = 0; i < totalsubmeshcount; i += 1)
		{
			newMesh.SetTriangles(newMesh.GetTriangles((int)(meshRenderer.subMeshStartIndex + i)), i, false, (int)newMesh.GetBaseVertex(meshRenderer.subMeshStartIndex + i));
		}

		newMesh.subMeshCount = totalsubmeshcount;

		ApplyInverseTransform(transform, newMesh);
	
		if (!AssetDatabase.IsValidFolder("Assets/Fixed Meshes"))
		{
			AssetDatabase.CreateFolder("Assets", "Fixed Meshes");
		}

		var prefabPath = "";

		var new_mesh_name = string.Format("UnbakedMesh_{0}_{1}_{2}", transform.name, originalMeshName, (int)Mathf.Abs(newMesh.GetHashCode()));
		if (originalMeshName.StartsWith("UnbakedMesh"))
		{
			Debug.Log("Replacing existing unbaked mesh (" + originalMeshName + ").");
			prefabPath = "Assets/Fixed Meshes/" + originalMeshName + ".asset";
		}
		
		else
		{
			newMesh.name = new_mesh_name;
			prefabPath = "Assets/Fixed Meshes/" + new_mesh_name + ".asset";
		}

		meshFilter.sharedMesh = newMesh;

		AssetDatabase.CreateAsset(newMesh, prefabPath);
		AssetDatabase.SaveAssets();
	}
	
	public static void ApplyInverseTransform(Transform transform, Mesh mesh)
	{
		var verts = mesh.vertices;
		var norms = mesh.normals;
		var tans = mesh.tangents;
		var bounds = mesh.bounds;

		for (int i = 0; i < verts.Length; ++i)
		{
			var nvert = verts[i];

			nvert = transform.InverseTransformPoint(nvert);

			verts[i] = nvert;
		}

		for (int i = 0; i < norms.Length; ++i)
		{
			var nnorm = norms[i];

			nnorm = transform.InverseTransformDirection(nnorm);

			norms[i] = nnorm;
		}

		for (int i = 0; i < tans.Length; ++i)
		{
			var ntan = tans[i];

			var transformed = transform.InverseTransformDirection(ntan.x, ntan.y, ntan.z);

			ntan = new Vector4(transformed.x, transformed.y, transformed.z, ntan.w);

			tans[i] = ntan;
		}

		bounds.center = transform.InverseTransformPoint(bounds.center);
		bounds.extents = transform.InverseTransformPoint(bounds.extents);

		mesh.vertices = verts;
		mesh.normals = norms;
		mesh.tangents = tans;
		mesh.bounds = bounds;
	}
	
	public static void ApplyTransform(Transform transform, Mesh mesh)
	{
		var verts = mesh.vertices;
		var norms = mesh.normals;
		var tans = mesh.tangents;
		var bounds = mesh.bounds;

		for (int i = 0; i < verts.Length; ++i)
		{
			var nvert = verts[i];

			nvert = transform.TransformPoint(nvert);

			verts[i] = nvert;
		}

		for (int i = 0; i < norms.Length; ++i)
		{
			var nnorm = norms[i];

			nnorm = transform.TransformDirection(nnorm);

			norms[i] = nnorm;
		}

		for (int i = 0; i < tans.Length; ++i)
		{
			var ntan = tans[i];

			var transformed = transform.TransformDirection(ntan.x, ntan.y, ntan.z);

			ntan = new Vector4(transformed.x, transformed.y, transformed.z, ntan.w);

			tans[i] = ntan;
		}

		bounds.center = transform.TransformPoint(bounds.center);
		bounds.extents = transform.TransformPoint(bounds.extents);

		mesh.vertices = verts;
		mesh.normals = norms;
		mesh.tangents = tans;
		mesh.bounds = bounds;
	}
}