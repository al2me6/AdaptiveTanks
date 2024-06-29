using System.Collections.Generic;
using System.Linq;

namespace AdaptiveTanks.Utils;

public static class PAWUtils
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

    public static void SetOptions(this UI_ChooseOption choose, IEnumerable<string> options,
        IEnumerable<string> displays)
    {
        choose.options = options.ToArray();
        choose.display = displays.ToArray();
    }
}
