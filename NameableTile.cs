namespace RubbleAutoloader;

/// <inheritdoc cref="ModTile"/>
public abstract class NameableTile : ModTile
{
    /// <summary> The type name. </summary>
    public string BaseName => GetType().Name;

	internal void ChangeName(string name, string textureName = default)
	{
		_name = name;
		_textureName = textureName;
	}

	private string _name;
	private string _textureName;

    /// <inheritdoc/>
    public override string Name => (_name == default) ? BaseName : _name;

    /// <inheritdoc/>
    public override string Texture => (GetType().Namespace + "." + ((_textureName == default) ? BaseName : _textureName)).Replace('.', '/');
}
