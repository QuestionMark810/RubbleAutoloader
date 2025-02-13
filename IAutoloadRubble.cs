﻿using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ObjectData;

namespace RubbleAutoloader;

/// <summary> Autoloads a rubble variant for this tile. Class access must be public for autoloading to work.<br/>
/// Rubbles are stored by type and can be checked using <see cref="Autoloader.IsRubble"/>. </summary>
public interface IAutoloadRubble
{
	/// <summary> Data used to define rubble tiles. </summary>
	/// <param name="item"> The item drop. </param>
	/// <param name="size"> The size (small/medium/large). </param>
	/// <param name="styles"> The tile styles to use. </param>
	public struct RubbleData(int item, RubbleSize size, int[] styles = null)
	{
        /// <summary> The item drop. </summary>
        public int item = item;

        /// <summary> The size (small/medium/large). </summary>
        public RubbleSize size = size;

		private readonly int[] styles = styles;

        /// <summary> The tile styles to use. </summary>
        public readonly int[] Styles => styles ?? [0];
	}

    /// <summary> Size settings according to <see cref="FlexibleTileWand"/> rubble placement. </summary>
    public enum RubbleSize : byte
	{
        /// <summary><see cref="FlexibleTileWand.RubblePlacementSmall"/></summary>
        Small = 0,
        /// <summary><see cref="FlexibleTileWand.RubblePlacementMedium"/></summary>
        Medium = 1,
        /// <summary><see cref="FlexibleTileWand.RubblePlacementLarge"/></summary>
        Large = 2
	}

    /// <summary> <see cref="RubbleData"/> belonging to this rubble type. </summary>
    public RubbleData Data { get; }
}

internal class RubbleSystem : ModSystem
{
	internal delegate bool orig_AddContent(Mod self, ILoadable instance);

    internal static readonly HashSet<int> RubbleTypes = [];

	private static bool Autoloads(Type type) => typeof(IAutoloadRubble).IsAssignableFrom(type);

	/// <summary> Initializes rubble autoloading. Must be called during loading and after all other content has been loaded. </summary>
	internal static void Initialize(Mod mod)
	{
        var content = mod.GetContent<ModTile>().ToArray();

        for (int i = 0; i < content.Length; i++)
        {
            if (Autoloads(content[i].GetType()))
            {
				var instance = (ModTile)ClassBuilder.CreateDynamic(content[i], content[i].Name + "Rubble", out _);

                mod.AddContent(instance);
                mod.Logger.Info($"[RubbleAutoloader] Added rubble: {instance.Name} ({instance.Type})");

                RubbleTypes.Add(instance.Type);
            }
        }
    }

	public override void PostSetupContent()
	{
		foreach (int type in RubbleTypes)
		{
			var data = ((IAutoloadRubble)TileLoader.GetTile(type)).Data;
			var objData = TileObjectData.GetTileData(type, 0);

			if (objData != null)
				objData.RandomStyleRange = 0;

			TileID.Sets.CanDropFromRightClick[type] = false;

			if (data.size == IAutoloadRubble.RubbleSize.Small)
				FlexibleTileWand.RubblePlacementSmall.AddVariations(data.item, type, data.Styles);
			else if (data.size == IAutoloadRubble.RubbleSize.Medium)
				FlexibleTileWand.RubblePlacementMedium.AddVariations(data.item, type, data.Styles);
			else if (data.size == IAutoloadRubble.RubbleSize.Large)
				FlexibleTileWand.RubblePlacementLarge.AddVariations(data.item, type, data.Styles);
		}
	}
}

/// <summary> Prevents normal item drops for autoloaded rubble tiles. </summary>
internal class RubbleGlobalTile : GlobalTile
{
	public override bool CanDrop(int i, int j, int type) => !Autoloader.IsRubble(type);

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail && Main.netMode != NetmodeID.MultiplayerClient && Autoloader.IsRubble(type) && TileObjectData.IsTopLeft(i, j))
		{
			var data = ((IAutoloadRubble)TileLoader.GetTile(type)).Data;
			var objData = TileObjectData.GetTileData(type, 0);
			var position = new Vector2(i + objData.Width / 2f, j + objData.Height / 2f) * 16;

			Item.NewItem(new EntitySource_TileBreak(i, j), position, data.item, noGrabDelay: true);
		}
	}
}