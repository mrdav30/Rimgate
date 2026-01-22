using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimgate;

[StaticConstructorOnStartup]
public static class RimgateTex
{
    public static readonly Texture2D CancelCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

    public static readonly Texture2D RenameCommandTex = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

    public static readonly Texture2D TransitSiteCommandTex = ContentFinder<Texture2D>.Get("UI/Map/RGGateTransitSiteIcon");

    public static readonly Texture2D AbandonGateSite = ContentFinder<Texture2D>.Get("UI/Button/RGAbandonGateSiteIcon");

    public static readonly Texture2D AbandonExploration = ContentFinder<Texture2D>.Get("UI/Button/RGAbandonExplorationIcon");

    public static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter");

    public static readonly Texture2D PushCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGPushIcon");

    public static readonly Texture2D PushAndDumpCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGPushAndDumpIcon");

    public static readonly Material ForceFieldMat = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);

    public static readonly Material ForceFieldConeMat = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);

    public static readonly Texture2D ShieldRadiusCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGShieldRadius");

    public static readonly Texture2D ShieldVisibilityCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGShieldVisibility");

    public static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));

    public static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

    public static readonly Texture2D AllowGuestCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGSarcophagusAllowGuestsIcon");

    public static readonly Texture2D AllowPrisonerCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGSarcophagusAllowPrisonersIcon");

    public static readonly Texture2D AllowSlaveCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGSarcophagusAllowSlavesIcon");

    public static readonly Texture2D TreatmentCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGAbortSarcophagusTreatmentIcon");

    public static readonly Texture2D CloneEjectCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGWraithCloningPodEjectIcon");

    public static readonly Texture2D CloneGenomeCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGWraithCloningPodGenomeIcon");

    public static readonly Texture2D CloneFullCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGWraithCloningPodFullIcon");

    public static readonly Texture2D CloneEnhancedCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGWraithCloningPodEnhancedIcon");

    public static readonly Texture2D CloneReconstructCommandTex = ContentFinder<Texture2D>.Get("UI/Button/RGWraithCloningPodReconstructIcon");

    public static readonly Texture2D EssenceCostTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.78f, 0.72f, 0.66f));

    public static Graphic EmptyGraphic
    {
        get
        {
            _cachedEmptyGraphic ??= GraphicDatabase.Get<Graphic_Single>(
                "empty",
                ShaderDatabase.DefaultShader,
                new Vector2(0.1f, 0.1f),
                Color.white,
                Color.white,
                new());

            return _cachedEmptyGraphic;
        }
    }

    private static Graphic _cachedEmptyGraphic;
}
