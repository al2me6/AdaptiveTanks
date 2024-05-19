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

    public static void SetMinMax(this UI_FloatEdit edit, float min, float max)
    {
        edit.minValue = min;
        edit.maxValue = max;
    }

    public static void SetIncrements(this UI_FloatEdit edit, float large, float small, float slide)
    {
        edit.incrementLarge = large;
        edit.incrementSmall = small;
        edit.incrementSlide = slide;
    }
}
