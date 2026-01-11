using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Pig1InteractionController), true)]
[CanEditMultipleObjects]
public class PigInteractionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-Assignment Tools", EditorStyles.boldLabel);
        
        Pig1InteractionController controller = (Pig1InteractionController)target;
        
        EditorGUI.BeginDisabledGroup(controller.rawMaterialsParent == null && 
                                      controller.processedMaterialsParent == null && 
                                      controller.buildMaterialsParent == null);
        
        if (GUILayout.Button("Auto Assign All Materials", GUILayout.Height(30)))
        {
            controller.AutoAssignMaterials();
            EditorUtility.SetDirty(controller);
            Debug.Log("Auto-assignment completed! Check the arrays above.");
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.HelpBox(
            "1. Assign parent GameObjects containing materials\n" +
            "2. Click 'Auto Assign All Materials' button\n" +
            "3. Materials will be found by name or automatically from all children",
            MessageType.Info);
    }
}

[CustomEditor(typeof(Pig2InteractionController), true)]
[CanEditMultipleObjects]
public class Pig2InteractionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-Assignment Tools", EditorStyles.boldLabel);
        
        Pig2InteractionController controller = (Pig2InteractionController)target;
        
        EditorGUI.BeginDisabledGroup(controller.rawMaterialsParent == null && 
                                      controller.processedMaterialsParent == null && 
                                      controller.buildMaterialsParent == null);
        
        if (GUILayout.Button("Auto Assign All Materials", GUILayout.Height(30)))
        {
            controller.AutoAssignMaterials();
            EditorUtility.SetDirty(controller);
            Debug.Log("Auto-assignment completed! Check the arrays above.");
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.HelpBox(
            "1. Assign parent GameObjects containing materials\n" +
            "2. Click 'Auto Assign All Materials' button\n" +
            "3. Materials will be found by name or automatically from all children",
            MessageType.Info);
    }
}

[CustomEditor(typeof(Pig3InteractionController), true)]
[CanEditMultipleObjects]
public class Pig3InteractionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-Assignment Tools", EditorStyles.boldLabel);
        
        Pig3InteractionController controller = (Pig3InteractionController)target;
        
        EditorGUI.BeginDisabledGroup(controller.rawMaterialsParent == null && 
                                      controller.processedMaterialsParent == null && 
                                      controller.buildMaterialsParent == null);
        
        if (GUILayout.Button("Auto Assign All Materials", GUILayout.Height(30)))
        {
            controller.AutoAssignMaterials();
            EditorUtility.SetDirty(controller);
            Debug.Log("Auto-assignment completed! Check the arrays above.");
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.HelpBox(
            "1. Assign parent GameObjects containing materials\n" +
            "2. Click 'Auto Assign All Materials' button\n" +
            "3. Materials will be found by name or automatically from all children",
            MessageType.Info);
    }
}