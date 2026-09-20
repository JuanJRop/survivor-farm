using UnityEditor;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    public sealed class LivingValleyArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/SurvivorFarm/Resources/StoryArt/"))return;
            var name=System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if(!name.StartsWith("Pack")&&name!="LivingMaple"&&name!="LivingBirch"&&name!="LivingWater"&&name!="MineralRocks"&&name!="WaterPlants"&&name!="Kitchen"&&name!="GuidePortrait")return;
            var importer=(TextureImporter)assetImporter;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;
            importer.alphaIsTransparency=true;importer.maxTextureSize=2048;
        }
    }
}
