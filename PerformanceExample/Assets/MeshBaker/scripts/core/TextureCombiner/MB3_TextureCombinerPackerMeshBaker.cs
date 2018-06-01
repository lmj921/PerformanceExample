using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerPackerMeshBaker : MB_ITextureCombinerPacker
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
                for (int propIdx = 0; propIdx < data.numAtlases; propIdx++)
                {
                    Texture2D atlas = null;
                    if (!MB3_TextureCombinerPipeline._ShouldWeCreateAtlasForThisProperty(propIdx, data._considerNonTextureProperties, data.allTexturesAreNullAndSameColor))
                    {
                        atlas = null;
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("=== Not creating atlas for " + data.texPropertyNames[propIdx].name + " because textures are null and default value parameters are the same.");
                    }
                    else
                    {
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("=== Creating atlas for " + data.texPropertyNames[propIdx].name);
                        GC.Collect();

                        //use a jagged array because it is much more efficient in memory
                        Color[][] atlasPixels = new Color[atlasSizeY][];
                        for (int j = 0; j < atlasPixels.Length; j++)
                        {
                            atlasPixels[j] = new Color[atlasSizeX];
                        }
                        bool isNormalMap = false;
                        if (data.texPropertyNames[propIdx].isNormalMap) isNormalMap = true;

                        for (int texSetIdx = 0; texSetIdx < data.distinctMaterialTextures.Count; texSetIdx++)
                        {
                            string s = "Creating Atlas '" + data.texPropertyNames[propIdx].name + "' texture " + data.distinctMaterialTextures[texSetIdx];
                            if (progressInfo != null) progressInfo(s, .01f);
                            MB_TexSet texSet = data.distinctMaterialTextures[texSetIdx];
                            if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(string.Format("Adding texture {0} to atlas {1}", texSet.ts[propIdx].GetTexture2D() == null ? "null" : texSet.ts[propIdx].GetTexName(), data.texPropertyNames[propIdx]));
                            Rect r = uvRects[texSetIdx];
                            Texture2D t = texSet.ts[propIdx].GetTexture2D();
                            int x = Mathf.RoundToInt(r.x * atlasSizeX);
                            int y = Mathf.RoundToInt(r.y * atlasSizeY);
                            int ww = Mathf.RoundToInt(r.width * atlasSizeX);
                            int hh = Mathf.RoundToInt(r.height * atlasSizeY);
                            if (ww == 0 || hh == 0) Debug.LogError("Image in atlas has no height or width " + r);
                            if (progressInfo != null) progressInfo(s + " set ReadWrite flag", .01f);
                            if (textureEditorMethods != null) textureEditorMethods.SetReadWriteFlag(t, true, true);
                            if (progressInfo != null) progressInfo(s + "Copying to atlas: '" + texSet.ts[propIdx].GetTexName() + "'", .02f);
                            DRect samplingRect = texSet.ts[propIdx].encapsulatingSamplingRect;
                            yield return CopyScaledAndTiledToAtlas(texSet.ts[propIdx], texSet, data.texPropertyNames[propIdx], samplingRect, x, y, ww, hh, packedAtlasRects.padding[texSetIdx], atlasPixels, isNormalMap, data, combiner, progressInfo, LOG_LEVEL);
                            //							Debug.Log("after copyScaledAndTiledAtlas");
                        }
                        yield return data.numAtlases;
                        if (progressInfo != null) progressInfo("Applying changes to atlas: '" + data.texPropertyNames[propIdx].name + "'", .03f);
                        atlas = new Texture2D(atlasSizeX, atlasSizeY, TextureFormat.ARGB32, true);
                        for (int j = 0; j < atlasPixels.Length; j++)
                        {
                            atlas.SetPixels(0, j, atlasSizeX, 1, atlasPixels[j]);
                        }
                        atlas.Apply();
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Saving atlas " + data.texPropertyNames[propIdx].name + " w=" + atlas.width + " h=" + atlas.height);
                    }
                    atlases[propIdx] = atlas;
                    if (progressInfo != null) progressInfo("Saving atlas: '" + data.texPropertyNames[propIdx].name + "'", .04f);
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();

                    if (data._saveAtlasesAsAssets && textureEditorMethods != null)
                    {
                        textureEditorMethods.SaveAtlasToAssetDatabase(atlases[propIdx], data.texPropertyNames[propIdx], propIdx, data.resultMaterial);
                    }
                    else
                    {
                        data.resultMaterial.SetTexture(data.texPropertyNames[propIdx].name, atlases[propIdx]);
                    }
                    data.resultMaterial.SetTextureOffset(data.texPropertyNames[propIdx].name, Vector2.zero);
                    data.resultMaterial.SetTextureScale(data.texPropertyNames[propIdx].name, Vector2.one);
                    combiner._destroyTemporaryTextures(); // need to save atlases before doing this
                }
            }
            //data.rectsInAtlas = uvRects;
            //			Debug.Log("finished!");
            yield break;
        }


        internal static IEnumerator CopyScaledAndTiledToAtlas(MeshBakerMaterialTexture source, MB_TexSet sourceMaterial,
            ShaderTextureProperty shaderPropertyName, DRect srcSamplingRect, int targX, int targY, int targW, int targH,
            AtlasPadding padding,
            Color[][] atlasPixels, bool isNormalMap,
            MB3_TextureCombinerPipeline.TexturePipelineData data,
            MB3_TextureCombiner combiner,
            ProgressUpdateDelegate progressInfo = null,
            MB2_LogLevel LOG_LEVEL = MB2_LogLevel.info)
        {
            //HasFinished = false;
            Texture2D t = source.GetTexture2D();
            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("CopyScaledAndTiledToAtlas: " + t + " inAtlasX=" + targX + " inAtlasY=" + targY + " inAtlasW=" + targW + " inAtlasH=" + targH);
            float newWidth = targW;
            float newHeight = targH;
            float scx = (float)srcSamplingRect.width;
            float scy = (float)srcSamplingRect.height;
            float ox = (float)srcSamplingRect.x;
            float oy = (float)srcSamplingRect.y;
            int w = (int)newWidth;
            int h = (int)newHeight;

            if (t == null)
            {
                if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("No source texture creating a 16x16 texture.");
                t = combiner._createTemporaryTexture(16, 16, TextureFormat.ARGB32, true);
                scx = 1;
                scy = 1;
                if (data._considerNonTextureProperties && data.nonTexturePropertyBlender != null)
                {
                    Color col = data.nonTexturePropertyBlender.GetColorIfNoTexture(sourceMaterial.matsAndGOs.mats[0].mat, shaderPropertyName);
                    if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Setting texture to solid color " + col);
                    MB_Utility.setSolidColor(t, col);
                }
                else
                {
                    Color col = MB3_TextureCombinerNonTextureProperties.GetColorIfNoTexture(shaderPropertyName);
                    MB_Utility.setSolidColor(t, col);
                }
            }
            if (data._considerNonTextureProperties && data.nonTexturePropertyBlender != null)
            {
                t = combiner._createTextureCopy(t);
                t = data.nonTexturePropertyBlender.TintTextureWithTextureCombiner(t, sourceMaterial, shaderPropertyName);
            }
            for (int i = 0; i < w; i++)
            {

                if (progressInfo != null && w > 0) progressInfo("CopyScaledAndTiledToAtlas " + (((float)i / (float)w) * 100f).ToString("F0"), .2f);
                for (int j = 0; j < h; j++)
                {
                    float u = i / newWidth * scx + ox;
                    float v = j / newHeight * scy + oy;
                    atlasPixels[targY + j][targX + i] = t.GetPixelBilinear(u, v);
                }
            }
            //bleed the border colors into the padding
            for (int i = 0; i < w; i++)
            {
                for (int j = 1; j <= padding.topBottom; j++)
                {
                    //top margin
                    atlasPixels[(targY - j)][targX + i] = atlasPixels[(targY)][targX + i];
                    //bottom margin
                    atlasPixels[(targY + h - 1 + j)][targX + i] = atlasPixels[(targY + h - 1)][targX + i];
                }
            }
            for (int j = 0; j < h; j++)
            {
                for (int i = 1; i <= padding.leftRight; i++)
                {
                    //left margin
                    atlasPixels[(targY + j)][targX - i] = atlasPixels[(targY + j)][targX];
                    //right margin
                    atlasPixels[(targY + j)][targX + w + i - 1] = atlasPixels[(targY + j)][targX + w - 1];
                }
            }
            //corners
            for (int i = 1; i <= padding.leftRight; i++)
            {
                for (int j = 1; j <= padding.topBottom; j++)
                {
                    atlasPixels[(targY - j)][targX - i] = atlasPixels[targY][targX];
                    atlasPixels[(targY + h - 1 + j)][targX - i] = atlasPixels[(targY + h - 1)][targX];
                    atlasPixels[(targY + h - 1 + j)][targX + w + i - 1] = atlasPixels[(targY + h - 1)][targX + w - 1];
                    atlasPixels[(targY - j)][targX + w + i - 1] = atlasPixels[targY][targX + w - 1];
                    yield return null;
                }
                yield return null;
            }
            //			Debug.Log("copyandscaledatlas finished too!");
            //HasFinished = true;
            yield break;
        }
    }
}
