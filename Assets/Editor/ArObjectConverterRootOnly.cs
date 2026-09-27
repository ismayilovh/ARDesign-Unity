// Save as: Assets/Editor/ArObjectConverterRootOnly.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ArObjectConverterRootOnly : EditorWindow
{
    GameObject root;

    bool addRigidbodyToRoot = true;
    bool makeRigidbodyKinematic = true;
    bool freezeRigidbodyConstraints = true;

    bool setRootColliderAsTrigger = false;

    bool addXRGrabInteractableToRoot = true;
    bool addARTransformerToRoot = true;

    [MenuItem("Window/AR Tools/Ar Object Converter (Root Only)")]
    static void OpenWindow()
    {
        GetWindow<ArObjectConverterRootOnly>("AR: Root Collider Tool").minSize = new Vector2(600, 300);
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Adds a single BoxCollider that fits the bounds of all children to the root. Optionally adds Rigidbody, XRGrabInteractable, and ARTransformer to the root.", MessageType.Info);

        EditorGUILayout.Space();
        root = (GameObject)EditorGUILayout.ObjectField("Root Object", root, typeof(GameObject), true);

        EditorGUILayout.Space();
        addRigidbodyToRoot = EditorGUILayout.ToggleLeft("Add Rigidbody to Root", addRigidbodyToRoot);
        using (new EditorGUI.DisabledScope(!addRigidbodyToRoot))
        {
            makeRigidbodyKinematic = EditorGUILayout.ToggleLeft("  Make Rigidbody Kinematic", makeRigidbodyKinematic);
            freezeRigidbodyConstraints = EditorGUILayout.ToggleLeft("  Freeze Rigidbody Constraints", freezeRigidbodyConstraints);
        }

        setRootColliderAsTrigger = EditorGUILayout.ToggleLeft("Set Root BoxCollider as Trigger", setRootColliderAsTrigger);

        EditorGUILayout.Space();
        addXRGrabInteractableToRoot = EditorGUILayout.ToggleLeft("Add XRGrabInteractable to Root", addXRGrabInteractableToRoot);
        addARTransformerToRoot = EditorGUILayout.ToggleLeft("Add ARTransformer to Root", addARTransformerToRoot);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(root == null))
        {
            if (GUILayout.Button("Run"))
            {
                if (root != null)
                    RunTool();
                else
                    EditorUtility.DisplayDialog("Error", "Please assign a Root GameObject.", "OK");
            }
        }
    }

    void RunTool()
    {
        Undo.RegisterFullObjectHierarchyUndo(root, "Add Root BoxCollider and AR Components");

        // Calculate combined bounds of all children
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No renderers found in children!", "OK");
            return;
        }

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            combinedBounds.Encapsulate(r.bounds);
        }

        // Convert world bounds to root local space
        Vector3 localCenter = root.transform.InverseTransformPoint(combinedBounds.center);
        Vector3 localSize = new Vector3(
            Mathf.Abs(root.transform.InverseTransformVector(new Vector3(combinedBounds.size.x, 0, 0)).magnitude),
            Mathf.Abs(root.transform.InverseTransformVector(new Vector3(0, combinedBounds.size.y, 0)).magnitude),
            Mathf.Abs(root.transform.InverseTransformVector(new Vector3(0, 0, combinedBounds.size.z)).magnitude)
        );

        // Add or update BoxCollider on root
        BoxCollider rootCollider = root.GetComponent<BoxCollider>();
        if (rootCollider == null)
            rootCollider = Undo.AddComponent<BoxCollider>(root);

        rootCollider.center = localCenter;
        rootCollider.size = localSize * 0.98f; // shrink slightly to avoid overlap
        rootCollider.isTrigger = setRootColliderAsTrigger;

        // Add Rigidbody if requested
        if (addRigidbodyToRoot)
        {
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb == null)
                rb = Undo.AddComponent<Rigidbody>(root);

            rb.isKinematic = makeRigidbodyKinematic;
            rb.useGravity = !makeRigidbodyKinematic;
            if (freezeRigidbodyConstraints && makeRigidbodyKinematic)
                rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // Add XRGrabInteractable via reflection if available
        if (addXRGrabInteractableToRoot)
        {
            Type xrGrabType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (xrGrabType != null && root.GetComponent(xrGrabType) == null)
                Undo.AddComponent(root, xrGrabType);
            else if (xrGrabType == null)
                Debug.LogWarning("XRGrabInteractable not found. Make sure XR Interaction Toolkit is installed.");
        }

        // Add ARTransformer via reflection if available
        if (addARTransformerToRoot)
        {
            Type arTransformerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Transformers.ARTransformer, Unity.XR.Interaction.Toolkit");
            if (arTransformerType != null && root.GetComponent(arTransformerType) == null)
                Undo.AddComponent(root, arTransformerType);
            else if (arTransformerType == null)
                Debug.LogWarning("ARTransformer not found. Make sure XR Interaction Toolkit >=2.5 is installed.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Root BoxCollider and AR components added successfully!");
        EditorUtility.DisplayDialog("Done", "Root BoxCollider and AR components added.", "OK");
    }
}
