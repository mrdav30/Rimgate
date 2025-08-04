using UnityEngine;
using Verse;

namespace Rimgate;

[StaticConstructorOnStartup]
public class Gizmo_ShieldStatus : Gizmo
{
    public Comp_ShieldEmitter shield;

    private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
    private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

    public Gizmo_ShieldStatus() => Order = -100f;

    public override float GetWidth(float maxWidth) => 140f;

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect1 = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
        Rect rect2 = GenUI.ContractedBy(rect1, 6f);
        Widgets.DrawWindowBackground(rect1);
        Rect rect3 = rect2;
        rect3.height = rect1.height / 2f;
        Text.Font = GameFont.Tiny;
        Widgets.Label(rect3, shield.Props.stressLabel);
        Rect rect4 = rect2;
        rect4.yMin = rect2.y + rect2.height / 2f;
        float num1 = shield.CurStressLevel / Mathf.Max(1f, shield.MaxStressLevel);
        Widgets.FillableBar(
            rect4,
            num1,
            Gizmo_ShieldStatus.FullShieldBarTex,
            Gizmo_ShieldStatus.EmptyShieldBarTex,
            false);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect rect5 = rect4;
        float num2 = shield.CurStressLevel * 100f;
        string str1 = num2.ToString("F0");
        num2 = shield.MaxStressLevel * 100f;
        string str2 = num2.ToString("F0");
        string str3 = $"{str1} / {str2}";
        Widgets.Label(rect5, str3);
        Text.Anchor = TextAnchor.UpperLeft;
        return new GizmoResult(GizmoState.Clear);
    }
}
