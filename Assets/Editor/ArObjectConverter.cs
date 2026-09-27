// Save as: Assets/Editor/ArObjectConverter.cs
// Updated: Adds XRGrabInteractable and ARTransformer (via reflection) to immediate parents
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ArObjectConverter : EditorWindow {
    GameObject root;

    // Project prefab option (keeps prefab linkage if used)
    GameObject prefabToInstantiate;
    bool instantiatePrefabUnderParent = false;

    // Scene GameObject duplication option
    GameObject sceneObjectToDuplicate;
    bool duplicateSceneObjectUnderParent = false;

    bool addRigidbodyToParent = true;
    bool makeRigidbodyKinematic = true; // AR-friendly default
    bool setColliderAsTrigger = false;
    bool freezeRigidbodyConstraints = true;

    // Reflection options for package types
    bool addXRGrabInteractableToParent = true;
    bool addARTransformerToParent = true;

    List<MonoScript> scriptsToAdd = new List<MonoScript>();

    Vector2 scroll;

    [MenuItem("Window/AR Tools/Ar Object Converter")]
    static void OpenWindow() {
        var w = GetWindow<ArObjectConverter>("AR: BoxCollider Tool");
        w.minSize = new Vector2(620, 480);
    }

    void OnGUI() {
        EditorGUILayout.HelpBox("Scans children of Root. For each child with a Renderer, adds a BoxCollider (approx bounds). Then on the immediate parent it can add Rigidbody, selected scripts, XRGrabInteractable and ARTransformer (if available), and instantiate/duplicate helper objects.", MessageType.Info);

        EditorGUILayout.Space();
        root = (GameObject)EditorGUILayout.ObjectField("Root (scan children)", root, typeof(GameObject), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Instantiate / Duplicate Child Object under immediate parent", EditorStyles.boldLabel);

        instantiatePrefabUnderParent = EditorGUILayout.ToggleLeft("Instantiate Project Prefab under parent", instantiatePrefabUnderParent);
        using (new EditorGUI.DisabledScope(!instantiatePrefabUnderParent)) {
            prefabToInstantiate = (GameObject)EditorGUILayout.ObjectField("Prefab (project)", prefabToInstantiate, typeof(GameObject), false);
        }

        duplicateSceneObjectUnderParent = EditorGUILayout.ToggleLeft("Duplicate Scene GameObject under parent", duplicateSceneObjectUnderParent);
        using (new EditorGUI.DisabledScope(!duplicateSceneObjectUnderParent)) {
            sceneObjectToDuplicate = (GameObject)EditorGUILayout.ObjectField("Scene GameObject to duplicate", sceneObjectToDuplicate, typeof(GameObject), true);
            EditorGUILayout.HelpBox("If the selected object is a scene object, a duplicate will be created under each immediate parent (no prefab linkage).", MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Parent component options", EditorStyles.boldLabel);
        addRigidbodyToParent = EditorGUILayout.ToggleLeft("Add Rigidbody to immediate parent (if missing)", addRigidbodyToParent);
        using (new EditorGUI.DisabledScope(!addRigidbodyToParent)) {
            makeRigidbodyKinematic = EditorGUILayout.ToggleLeft("  Make Rigidbody.isKinematic (recommended for AR placed static objects)", makeRigidbodyKinematic);
            freezeRigidbodyConstraints = EditorGUILayout.ToggleLeft("  Freeze Rigidbody constraints (optional)", freezeRigidbodyConstraints);
        }

        setColliderAsTrigger = EditorGUILayout.ToggleLeft("Set added BoxColliders as Trigger", setColliderAsTrigger);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("XR / AR components (package types added via reflection)", EditorStyles.boldLabel);
        addXRGrabInteractableToParent = EditorGUILayout.ToggleLeft("Add XRGrabInteractable to immediate parent (if available)", addXRGrabInteractableToParent);
        addARTransformerToParent = EditorGUILayout.ToggleLeft("Add ARTransformer to immediate parent (if available)", addARTransformerToParent);
        EditorGUILayout.HelpBox("These components come from the XR Interaction Toolkit package. If the package is not installed the tool will skip them and log a warning.", MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scripts to add to immediate parent (drag MonoScript assets):", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(140));
        int removeIndex = -1;
        for (int i = 0; i < scriptsToAdd.Count; ++i) {
            EditorGUILayout.BeginHorizontal();
            scriptsToAdd[i] = (MonoScript)EditorGUILayout.ObjectField(scriptsToAdd[i], typeof(MonoScript), false);
            if (GUILayout.Button("Remove", GUILayout.Width(70))) removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0) scriptsToAdd.RemoveAt(removeIndex);
        if (GUILayout.Button("Add Script Slot")) scriptsToAdd.Add(null);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(root == null)) {
            if (GUILayout.Button("Run")) {
                if (root == null) {
                    EditorUtility.DisplayDialog("Error", "Please assign a Root GameObject.", "OK");
                } else {
                    RunTool();
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Notes:\n- Reflection is used for XRGrabInteractable and ARTransformer so package types can be added even when they live inside package assemblies.\n- Parent ops are executed only once per parent to avoid duplicate helpers.\n- Everything is undoable.", MessageType.None);
    }

    void RunTool() {
        if (root == null) return;

        int boxCollidersAdded = 0;
        int rigidbodiesAdded = 0;
        int scriptsAddedCount = 0;
        int prefabsInstantiated = 0;
        int sceneDuplicatesCreated = 0;
        int xrGrabAdded = 0;
        int arTransformerAdded = 0;

        // Collect script Types from MonoScript list (project scripts)
        List<Type> scriptTypes = new List<Type>();
        foreach (var ms in scriptsToAdd) {
            if (ms == null) continue;
            var t = ms.GetClass();
            if (t == null) {
                Debug.LogWarning($"MonoScript {ms.name} has no valid class (maybe an editor script or missing). Skipping.");
                continue;
            }
            if (!typeof(MonoBehaviour).IsAssignableFrom(t)) {
                Debug.LogWarning($"{t.FullName} is not a MonoBehaviour. Skipping.");
                continue;
            }
            scriptTypes.Add(t);
        }

        // Reflection: try to resolve XRGrabInteractable and ARTransformer types (package assemblies)
        // XRGrabInteractable full type name in XR Interaction Toolkit:
        Type xrGrabType = null;
        Type arTransformerType = null;

        if (addXRGrabInteractableToParent) {
            xrGrabType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (xrGrabType == null) {
                // Try alternative assembly name sometimes used
                xrGrabType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit.Runtime");
            }
            if (xrGrabType == null) {
                Debug.LogWarning("XRGrabInteractable type not found. Make sure XR Interaction Toolkit package is installed.");
            }
        }

        if (addARTransformerToParent) {
            // ARTransformer lives under Transformers namespace inside the XR Interaction Toolkit package
            arTransformerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Transformers.ARTransformer, Unity.XR.Interaction.Toolkit");
            if (arTransformerType == null) {
                // alternative assembly try
                arTransformerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Transformers.ARTransformer, Unity.XR.Interaction.Toolkit.Runtime");
            }
            if (arTransformerType == null) {
                Debug.LogWarning("ARTransformer type not found. Make sure XR Interaction Toolkit package is installed (check version >= 2.5).");
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Add BoxColliders and Parent Components (AR)");

        // Track processed parents so we only run parent ops once per parent
        HashSet<Transform> processedParents = new HashSet<Transform>();

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all) {
            if (t == root.transform) continue;

            var mr = t.GetComponent<Renderer>(); // MeshRenderer or SkinnedMeshRenderer
            if (mr == null) continue;

            // Add BoxCollider if missing
            if (t.GetComponent<BoxCollider>() == null) {
                BoxCollider bc = Undo.AddComponent<BoxCollider>(t.gameObject);

                // Compute world bounds and convert to local axes safely
                Bounds wb = mr.bounds; // world-space AABB
                Vector3 localCenter = t.InverseTransformPoint(wb.center);
                Vector3 localSize = new Vector3(
                    Mathf.Abs(t.InverseTransformVector(new Vector3(wb.size.x, 0, 0)).magnitude),
                    Mathf.Abs(t.InverseTransformVector(new Vector3(0, wb.size.y, 0)).magnitude),
                    Mathf.Abs(t.InverseTransformVector(new Vector3(0, 0, wb.size.z)).magnitude)
                );

                // Optionally shrink slightly to avoid oversized boxes
                float shrinkFactor = 0.98f;
                localSize *= shrinkFactor;

                bc.center = localCenter;
                bc.size = localSize;
                bc.isTrigger = setColliderAsTrigger;

                boxCollidersAdded++;
            }

            // Immediate parent operations (only once per parent)
            Transform parentT = t.parent;
            if (parentT == null) continue;
            if (processedParents.Contains(parentT)) continue;
            processedParents.Add(parentT);

            GameObject parentGo = parentT.gameObject;

            // Add Rigidbody if requested
            if (addRigidbodyToParent) {
                Rigidbody rb = parentGo.GetComponent<Rigidbody>();
                if (rb == null) {
                    rb = Undo.AddComponent<Rigidbody>(parentGo);
                    rigidbodiesAdded++;
                }
                // Configure rb
                if (rb != null) {
                    rb.isKinematic = makeRigidbodyKinematic;
                    rb.useGravity = !makeRigidbodyKinematic;
                    if (freezeRigidbodyConstraints && makeRigidbodyKinematic) {
                        rb.constraints = RigidbodyConstraints.FreezeAll;
                    }
                }
            }

            // Add project scripts to parent
            foreach (var type in scriptTypes) {
                if (parentGo.GetComponent(type) == null) {
                    Undo.AddComponent(parentGo, type);
                    scriptsAddedCount++;
                }
            }

            // Add XRGrabInteractable via reflection if available
            if (xrGrabType != null) {
                if (parentGo.GetComponent(xrGrabType) == null) {
                    Undo.AddComponent(parentGo, xrGrabType);
                    xrGrabAdded++;
                }
            }

            // Add ARTransformer via reflection if available
            if (arTransformerType != null) {
                if (parentGo.GetComponent(arTransformerType) == null) {
                    // ARTransformer is typically an AddComponentMenu item in package; adding via reflection should work
                    Undo.AddComponent(parentGo, arTransformerType);
                    arTransformerAdded++;
                }
            }

            // Instantiate project prefab under parent (preserve prefab linkage)
            if (instantiatePrefabUnderParent && prefabToInstantiate != null) {
                GameObject instGo = null;
#if UNITY_2018_3_OR_NEWER
                UnityEngine.Object prefabInst = PrefabUtility.InstantiatePrefab(prefabToInstantiate, parentGo.transform);
                if (prefabInst != null) {
                    instGo = prefabInst as GameObject;
                    if (instGo != null) {
                        Undo.RegisterCreatedObjectUndo(instGo, "Instantiate Prefab under parent");
                        prefabsInstantiated++;
                    }
                }
#else
                instGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabToInstantiate);
                if (instGo != null) {
                    instGo.transform.SetParent(parentGo.transform, false);
                    Undo.RegisterCreatedObjectUndo(instGo, "Instantiate Prefab under parent");
                    prefabsInstantiated++;
                }
#endif
            }

            // Duplicate a scene GameObject under the parent (keeps copies local)
            if (duplicateSceneObjectUnderParent && sceneObjectToDuplicate != null) {
                // If the provided object is an asset prefab, instantiate via PrefabUtility; otherwise Instantiate the scene object
                GameObject duplicate = null;
#if UNITY_2018_3_OR_NEWER
                duplicate = (GameObject)PrefabUtility.InstantiatePrefab(sceneObjectToDuplicate, parentGo.transform);
                if (duplicate == null) {
                    duplicate = (GameObject)UnityEngine.Object.Instantiate(sceneObjectToDuplicate, parentGo.transform);
                }
#else
                duplicate = (GameObject)UnityEngine.Object.Instantiate(sceneObjectToDuplicate);
                duplicate.transform.SetParent(parentGo.transform, false);
#endif
                if (duplicate != null) {
                    // Try copy local transform from original if possible
                    try {
                        duplicate.transform.localPosition = sceneObjectToDuplicate.transform.localPosition;
                        duplicate.transform.localRotation = sceneObjectToDuplicate.transform.localRotation;
                        duplicate.transform.localScale = sceneObjectToDuplicate.transform.localScale;
                    } catch { }
                    Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Scene Object under parent");
                    sceneDuplicatesCreated++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string summary = $"BoxColliders added: {boxCollidersAdded}, Rigidbodies added: {rigidbodiesAdded}, Scripts added: {scriptsAddedCount}, Prefabs instantiated: {prefabsInstantiated}, Scene duplicates: {sceneDuplicatesCreated}, XRGrabInteractable added: {xrGrabAdded}, ARTransformer added: {arTransformerAdded}";
        Debug.Log(summary);
        EditorUtility.DisplayDialog("Tool finished", summary, "OK");
    }
}
