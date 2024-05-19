namespace AdaptiveTanks.Extensions;

public static class PAWExtensions
{
    public static void AddSelfAndSymmetryListener(this BaseField field,
        Callback<BaseField, object> action)
    {
        field.uiControlEditor.onFieldChanged += action;
        field.uiControlEditor.onSymmetryFieldChanged += action;
    }

    public static T AsEditor<T>(this BaseField field)
        where T : UI_Control
    {
        return field.uiControlEditor as T;
    }
}
