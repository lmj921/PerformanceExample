using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerPackerMeshBakerFast : MB_ITextureCombinerPacker
    {
        public IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Rect[] uvRects = packedAtlasRects.rects;
            if (uvRects.Length == 1)
            {
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Only one image per atlas. Will re-use original texture");
                for (int i = 0; i < data.numAtlases; i++)
                {
                    MeshBakerMaterialTexture dmt = data.distinctMaterialTextures[0].ts[i];
                    atlases[i] = dmt.GetTexture2D();
                    data.resultMaterial.SetTexture(data.texPropertyNames[i].name, atlases[i]);
                    data.resultMaterial.SetTextureScale(data.texPropertyNames[i].name, dmt.matTilingRect.size);
                    data.resultMaterial.SetTextureOffset(data.texPropertyNames[i].name, dmt.matTilingRect.min);
                }
            }
            else
            {
                int atlasSizeX = packedAtlasRects.atlasX;
                int atlasSizeY = packedAtlasRects.atlasY;
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Generated atlas will be " + atlasSizeX + "x" + atlasSizeY);

                //create a game object
                GameObject renderAtlasesGO = null;
                try
                {
                    renderAtlasesGO = new GameObject("MBrenderAtlasesGO");
                    MB3_AtlasPackerRenderTexture atlasRenderTexture = renderAtlasesGO.AddComponent<MB3_AtlasPackerRenderTexture>();
                    renderAtlasesGO.AddComponent<Camera>();
                    if (data._considerNonTextureProperties)
                    {
                        if (LOG_LEVEL >= MB2_LogLevel.warn)
                        {
                            Debug.LogWarning("Blend Non-Texture Properties has limited functionality when used with Mesh Baker Texture Packer Fast.");
                        }
                    }
                    for (int i = 0; i < data.numAtlases; i++)
                    {
                        Texture2D atlas = null;
                        if (!MB3_TextureCombinerPipeline._ShouldWeCreateAtlasForThisProperty(i, data._considerNonTextureProperties, data.allTexturesAreNullAndSameColor))
                        {
                            atlas = null;
                            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Not creating atlas for " + data.texPropertyNames[i].name + " because textures are null and default value parameters are the same.");
                        }
                        else
                        {
                            GC.Collect();
                            if (progressInfo != null) progressInfo("Creating Atlas '" + data.texPropertyNames[i].name + "'", .01f);
                            // ===========
                            // configure it
                            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("About to render " + data.texPropertyNames[i].name + " isNormal=" + data.texPropertyNames[i].isNormalMap);
                            atlasRenderTexture.LOG_LEVEL = LOG_LEVEL;
                            atlasRenderTexture.width = atlasSizeX;
                            atlasRenderTexture.height = atlasSizeY;
                            atlasRenderTexture.padding = data._atlasPadding;
                            atlasRenderTexture.rects = uvRects;
                            atlasRenderTexture.textureSets = data.distinctMaterialTextures;
                            atlasRenderTexture.indexOfTexSetToRender = i;
                            atlasRenderTexture.texPropertyName = data.texPropertyNames[i];
                            atlasRenderTexture.isNormalMap = data.texPropertyNames[i].isNormalMap;
                            atlasRenderTexture.fixOutOfBoundsUVs = data._fixOutOfBoundsUVs;
                            atlasRenderTexture.considerNonTextureProperties = data._considerNonTextureProperties;
                            atlasRenderTexture.resultMaterialTextureBlender = data.nonTexturePropertyBlender;
                            // call render on it
                            atlas = atlasRenderTexture.OnRenderAtlas(combiner);

                            // destroy it
                            // =============
                            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Saving atlas " + data.texPropertyNames[i].name + " w=" + atlas.width + " h=" + atlas.height + " id=" + atlas.GetInstanceID());
                        }
                        atlases[i] = atlas;
                        if (progressInfo != null) progressInfo("Saving atlas: '" + data.texPropertyNames[i].name + "'", .04f);
                        if (data._saveAtlasesAsAssets && textureEditorMethods != null)
                        {
                            textureEditorMethods.SaveAtlasToAssetDatabase(atlases[i], data.texPropertyNames[i], i, data.resultMaterial);
                        }
                        else
                        {
                            data.resultMaterial.SetTexture(data.texPropertyNames[i].name, atlases[i]);
                        }
                        data.resultMaterial.SetTextureOffset(data.texPropertyNames[i].name, Vector2.zero);
                        data.resultMaterial.SetTextureScale(data.texPropertyNames[i].name, Vector2.one);
                        combiner._destroyTemporaryTextures(); // need to save atlases before doing this				
                    }
                }
                catch (Exception ex)
                {
                    //Debug.LogError(ex);
                    Debug.LogException(ex);
                }
                finally
                {
                    if (renderAtlasesGO != null)
                    {
                        MB_Utility.Destroy(renderAtlasesGO);
                    }
                }
            }
            yield break;
        }
    }
}
