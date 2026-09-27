using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Idempotently wires the four settings into the active UpdatedUI canvas prefab.</summary>
public static class SettingsFeaturePrefabWiring
{
    private const string PrefabPath = "Assets/Prefabs/UI/UpdatedUI/Canvas.prefab";

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticWiring()
    {
        EditorApplication.delayCall += WireIfNeeded;
    }

    private static void WireIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return;

        bool isWired = FindDescendant(prefab.transform, "Notifications Toggle") != null &&
                       FindDescendant(prefab.transform, "Debug Mode Toggle") != null &&
                       prefab.GetComponentInChildren<ObjectInfoManager>(true) != null &&
                       prefab.GetComponentInChildren<DebugModeManager>(true) != null &&
                       prefab.GetComponentInChildren<LogManager>(true) != null;
        if (!isWired)
            WireSettingsUI();
    }

    [MenuItem("Tools/AR Viewer/Wire Settings UI")]
    public static void WireSettingsUI()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform canvas = FindDescendant(prefabRoot.transform, "Canvas");
            Transform layout = FindDescendant(prefabRoot.transform, "Layout");
            Transform miscOptions = FindDescendant(prefabRoot.transform, "Misc Options");
            Transform modelOptions = FindDescendant(prefabRoot.transform, "Model Options");
            Transform pivotButton = FindDescendant(prefabRoot.transform, "PivotButton");
            Transform pivotDescription = FindDescendant(prefabRoot.transform, "Pivot Description");
            Transform popupLayer = FindDescendant(prefabRoot.transform, "PopupLayer");
            UIVisibilityManager visibilityManager = prefabRoot.GetComponentInChildren<UIVisibilityManager>(true);

            if (canvas == null || layout == null || miscOptions == null ||
                modelOptions == null || pivotButton == null || pivotDescription == null ||
                popupLayer == null || visibilityManager == null)
            {
                Debug.LogError("Settings UI wiring stopped because the active UpdatedUI hierarchy changed.");
                return;
            }

            TextMeshProUGUI sourceLabel = pivotDescription.GetComponent<TextMeshProUGUI>();
            if (sourceLabel == null)
            {
                Debug.LogError("Settings UI wiring stopped because Pivot Description has no TextMeshProUGUI component.");
                return;
            }

            StyleTextToggle(pivotButton.gameObject, "Use Calculated Pivot", false);

            PivotSettingManager pivotManager = pivotButton.GetComponent<PivotSettingManager>();
            SerializedObject pivotManagerObject = new SerializedObject(pivotManager);
            pivotManagerObject.FindProperty("useCalculatedPivot").boolValue = false;
            pivotManagerObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject objectInfoToggle = GetOrCreateToggle(
                pivotButton.gameObject, modelOptions, "Object Info Toggle", "Show Object Info", false);
            GameObject objectInfoPanel = GetOrCreatePanel(
                modelOptions, sourceLabel, "Object Info Panel", new Vector2(-170f, 0f), new Vector2(520f, 310f));
            TextMeshProUGUI objectInfoText = objectInfoPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            objectInfoText.text = "No model loaded";

            ObjectInfoManager objectInfoManager = GetOrAddComponent<ObjectInfoManager>(objectInfoToggle);
            SetObjectReference(objectInfoManager, "panelRoot", objectInfoPanel);
            SetObjectReference(objectInfoManager, "infoText", objectInfoText);
            SetObjectReference(objectInfoManager, "visibilityToggle", objectInfoToggle.GetComponent<UIToggle>());
            objectInfoPanel.SetActive(false);

            GameObject notificationsToggle = GetOrCreateToggle(
                pivotButton.gameObject, miscOptions, "Notifications Toggle", "Notifications", true);
            GameObject debugToggle = GetOrCreateToggle(
                pivotButton.gameObject, miscOptions, "Debug Mode Toggle", "Debug Mode", false);

            GameObject debugRoot = GetOrCreatePanel(
                layout, sourceLabel, "Debug Overlay", new Vector2(-20f, -120f), new Vector2(360f, 190f));
            RectTransform debugRect = debugRoot.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(1f, 1f);
            debugRect.anchorMax = new Vector2(1f, 1f);
            debugRect.pivot = new Vector2(1f, 1f);
            TextMeshProUGUI debugText = debugRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            debugText.text = "DEBUG";
            debugRoot.GetComponent<Image>().raycastTarget = false;
            debugRoot.SetActive(false);
            debugRoot.transform.SetSiblingIndex(popupLayer.GetSiblingIndex());

            DebugModeManager debugModeManager = GetOrAddComponent<DebugModeManager>(canvas.gameObject);
            SetObjectReference(debugModeManager, "debugRoot", debugRoot);
            SetObjectReference(debugModeManager, "debugText", debugText);
            SetObjectReference(debugModeManager, "uiVisibilityManager", visibilityManager);

            Transform notificationRoot = FindDescendant(layout, "Notification Manager");
            if (notificationRoot == null)
            {
                GameObject notificationObject = new GameObject(
                    "Notification Manager", typeof(RectTransform), typeof(LayoutElement));
                notificationObject.layer = 5;
                notificationRoot = notificationObject.transform;
                notificationRoot.SetParent(layout, false);
                Stretch((RectTransform)notificationRoot);
                notificationObject.GetComponent<LayoutElement>().ignoreLayout = true;
            }

            notificationRoot.SetSiblingIndex(popupLayer.GetSiblingIndex());

            LogManager logManager = GetOrAddComponent<LogManager>(notificationRoot.gameObject);
            Transform notificationTemplate = FindDescendant(notificationRoot, "Notification Template");
            if (notificationTemplate == null)
            {
                GameObject templateObject = Object.Instantiate(sourceLabel.gameObject, notificationRoot, false);
                templateObject.name = "Notification Template";
                notificationTemplate = templateObject.transform;
                RectTransform templateRect = templateObject.GetComponent<RectTransform>();
                templateRect.anchorMin = new Vector2(0.5f, 0.5f);
                templateRect.anchorMax = new Vector2(0.5f, 0.5f);
                templateRect.pivot = new Vector2(0.5f, 0.5f);
                templateRect.sizeDelta = new Vector2(520f, 60f);
                TextMeshProUGUI templateText = templateObject.GetComponent<TextMeshProUGUI>();
                templateText.alignment = TextAlignmentOptions.Center;
                templateText.raycastTarget = false;
                GetOrAddComponent<LogText>(templateObject);
                templateObject.SetActive(false);
            }

            SetObjectReference(logManager, "logPrefab", notificationTemplate.GetComponent<LogText>());
            SetVector2(logManager, "startPoint", new Vector2(0f, 180f));
            SetVector2(logManager, "endPoint", new Vector2(0f, 280f));

            MiscOptionsManager coordinator = GetOrAddComponent<MiscOptionsManager>(miscOptions.gameObject);
            SetObjectReference(coordinator, "panelRoot", miscOptions.gameObject);
            SetObjectReference(coordinator, "notificationsToggle", notificationsToggle.GetComponent<UIToggle>());
            SetObjectReference(coordinator, "debugModeToggle", debugToggle.GetComponent<UIToggle>());
            SetObjectReference(coordinator, "logManager", logManager);
            SetObjectReference(coordinator, "debugModeManager", debugModeManager);

            SetLayerRecursively(objectInfoToggle, 5);
            SetLayerRecursively(objectInfoPanel, 5);
            SetLayerRecursively(debugRoot, 5);
            SetLayerRecursively(notificationRoot.gameObject, 5);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Settings UI wiring completed for " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static GameObject GetOrCreateToggle(
        GameObject source,
        Transform parent,
        string objectName,
        string label,
        bool defaultValue)
    {
        Transform existing = FindDescendant(parent, objectName);
        GameObject toggleObject = existing != null
            ? existing.gameObject
            : Object.Instantiate(source, parent, false);

        toggleObject.name = objectName;
        RemoveSpecializedManagers(toggleObject);
        StyleTextToggle(toggleObject, label, defaultValue);
        return toggleObject;
    }

    private static void RemoveSpecializedManagers(GameObject toggleObject)
    {
        PivotSettingManager pivot = toggleObject.GetComponent<PivotSettingManager>();
        if (pivot != null) Object.DestroyImmediate(pivot);
        ObjectInfoManager objectInfo = toggleObject.GetComponent<ObjectInfoManager>();
        if (objectInfo != null) Object.DestroyImmediate(objectInfo);
    }

    private static void StyleTextToggle(GameObject toggleObject, string label, bool defaultValue)
    {
        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150f, 80f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.SetIsOnWithoutNotify(defaultValue);

        TextMeshProUGUI text = toggleObject.GetComponentInChildren<TextMeshProUGUI>(true);
        text.gameObject.SetActive(true);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 18f;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(6f, 6f);
        text.rectTransform.offsetMax = new Vector2(-6f, -6f);

        for (int i = 0; i < toggleObject.transform.childCount; i++)
        {
            GameObject child = toggleObject.transform.GetChild(i).gameObject;
            if (child != text.gameObject && child.GetComponent<Image>() != null)
                child.SetActive(false);
        }
    }

    private static GameObject GetOrCreatePanel(
        Transform parent,
        TextMeshProUGUI sourceLabel,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        Transform existing = FindDescendant(parent, objectName);
        if (existing != null) return existing.gameObject;

        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.07f, 0.1f, 0.13f, 0.94f);
        background.raycastTarget = true;
        panel.GetComponent<LayoutElement>().ignoreLayout = true;

        GameObject textObject = Object.Instantiate(sourceLabel.gameObject, panel.transform, false);
        textObject.name = "Info Text";
        textObject.SetActive(true);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 20f;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(18f, 18f);
        text.rectTransform.offsetMax = new Vector2(-18f, -18f);
        return panel;
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Object target, string propertyName, Vector2 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.vector2Value = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
