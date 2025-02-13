global using System;
global using Microsoft.Xna.Framework;
global using Terraria;
global using Terraria.ID;
global using Terraria.ModLoader;

namespace RubbleAutoloader;

/// <summary> The core of RubbleAutoloader. </summary>
public class Autoloader : Mod
{
    /// <returns> Whether <paramref name="type"/> is an autoloaded rubble tile. </returns>
	public static bool IsRubble(int type) => RubbleSystem.RubbleTypes.Contains(type);

    /// <summary> Initializes rubble autoloading. Must be called directly in <paramref name="mod"/>'s Load method. </summary>
    /// <param name="mod"> your mod. </param>
    public static void Load(Mod mod)
    {
        RubbleSystem.Initialize(mod);

        mod.AddContent<RubbleSystem>();
        mod.AddContent<RubbleGlobalTile>();
    }
}
