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

    public static readonly Texture2D PermanentSiteCommandTex = ContentFinder<Texture2D>.Get("UI/Icon/Map/RGStargatePermanentSiteIcon");

    public static readonly Texture2D AbandonStargateSite = ContentFinder<Texture2D>.Get("UI/Icon/Button/RGAbandonStargateSiteIcon");

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
