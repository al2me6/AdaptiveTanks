namespace AdaptiveTanks.Extensions;

public static class PAWExtensions
{
    public static void AddSelfAndSymmetryListener(this BaseFieldList fields, string fieldName,
        Callback<BaseField, object> action)
    {
        fields[fieldName].uiControlEditor.onFieldChanged += action;
        fields[fieldName].uiControlEditor.onSymmetryFieldChanged += action;
    }

    public static T AsEditorUICtrl<T>(this BaseFieldList fields, string fieldName)
        where T : UI_Control
    {
        return fields[fieldName].uiControlEditor as T;
    }
}
