using UnityEditor;

namespace SurvivorFarm.Editor
{
    public sealed class FarmRPGTinyAssetPackImporter : AssetPostprocessor
    {
        private const string AssetPackPath = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AssetPackPath, System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = UnityEngine.FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
