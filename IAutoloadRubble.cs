using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ObjectData;

namespace RubbleAutoloader;

/// <summary> Autoloads a rubble variant for this tile. This class must be public for autoloading to work.<br/>
/// Rubbles are stored by type and can be checked using <see cref="Autoloader.IsRubble"/>. </summary>
public interface IAutoloadRubble
{
    /// <summary> Data used to define rubble tiles. </summary>
    /// <param name="item"> The item drop. </param>
    /// <param name="size"> The size (small/medium/large). </param>
    /// <param name="styles"> The tile styles to use. null will automatically interpret styles from <see cref="TileObjectData.RandomStyleRange"/>. </param>
    public struct RubbleData(int item, RubbleSize size, int[] styles = null)
	{
        /// <summary> The item drop. </summary>
        public int item = item;

        /// <summary> The size (small/medium/large). </summary>
        public RubbleSize size = size;

        /// <summary> The tile styles to use. </summary>
        public int[] styles = styles;
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
				var instance = (ModTile)TileTypeBuilder.CreateDynamic(content[i], (content[i].GetType().Namespace + "." + content[i].Name).Replace(".", "/"), content[i].Texture, out _);

                int type = (int)typeof(TileLoader).GetField("nextTile", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); //Important that this is placed before AddContent increments the value
                if (mod.AddContent(instance))
				{
                    mod.Logger.Info($"[RubbleAutoloader] Added rubble: {instance.Name} ({type})");
                    RubbleTypes.Add(type);
                }
            }
        }
    }


	public override void PostSetupContent()
	{
		foreach (int type in RubbleTypes)
		{
			int[] styles = ReadStyle(type, out var data);

			TileID.Sets.CanDropFromRightClick[type] = false;

			if (data.size == IAutoloadRubble.RubbleSize.Small)
				FlexibleTileWand.RubblePlacementSmall.AddVariations(data.item, type, styles);
			else if (data.size == IAutoloadRubble.RubbleSize.Medium)
				FlexibleTileWand.RubblePlacementMedium.AddVariations(data.item, type, styles);
			else if (data.size == IAutoloadRubble.RubbleSize.Large)
				FlexibleTileWand.RubblePlacementLarge.AddVariations(data.item, type, styles);
		}

		static int[] ReadStyle(int type, out IAutoloadRubble.RubbleData data)
		{
            data = ((IAutoloadRubble)TileLoader.GetTile(type)).Data;

			if (data.styles is not null)
				return data.styles;

            var objData = TileObjectData.GetTileData(type, 0);

			if (objData is null)
				return [0];

			int[] styles = [Math.Min(objData.RandomStyleRange, 1)];
            objData.RandomStyleRange = 0;

            for (int i = 0; i < styles.Length; i++)
				styles[i] = i;

			return styles;
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