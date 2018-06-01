using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerPackerUnity : MB_ITextureCombinerPacker
    {
        public IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL)
        {
            Rect[] uvRects = packedAtlasRects.rects;
            if (data.distinctMaterialTextures.Count == 1 && data._fixOutOfBoundsUVs == false)
            {
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Only one image per atlas. Will re-use original texture");
                uvRects = new Rect[1];
                uvRects[0] = new Rect(0f, 0f, 1f, 1f);
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
                long estArea = 0;
                int atlasSizeX = 1;
                int atlasSizeY = 1;
                uvRects = null;
                for (int i = 0; i < data.numAtlases; i++)
                { //i is an atlas "MainTex", "BumpMap" etc...
                  //-----------------------
                    Texture2D atlas = null;
                    if (!MB3_TextureCombinerPipeline._ShouldWeCreateAtlasForThisProperty(i, data._considerNonTextureProperties, data.allTexturesAreNullAndSameColor))
                    {
                        atlas = null;
                    }
                    else
                    {
                        if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.LogWarning("Beginning loop " + i + " num temporary textures " + combiner._temporaryTextures.Count);
                        for (int j = 0; j < data.distinctMaterialTextures.Count; j++)
                        { //j is a distinct set of textures one for each of "MainTex", "BumpMap" etc...
                            MB_TexSet txs = data.distinctMaterialTextures[j];

                            int tWidth = txs.idealWidth;
                            int tHeight = txs.idealHeight;

                            Texture2D tx = txs.ts[i].GetTexture2D();
                            if (tx == null)
                            {
                                tx = txs.ts[i].t = combiner._createTemporaryTexture(tWidth, tHeight, TextureFormat.ARGB32, true);
                                if (data._considerNonTextureProperties && data.nonTexturePropertyBlender != null)
                                {
                                    Color col = data.nonTexturePropertyBlender.GetColorIfNoTexture(txs.matsAndGOs.mats[0].mat, data.texPropertyNames[i]);
                                    if (LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Setting texture to solid color " + col);
                                    MB_Utility.setSolidColor(tx, col);
                                }
                                else
                                {
                                    Color col = MB3_TextureCombinerNonTextureProperties.GetColorIfNoTexture(data.texPropertyNames[i]);
                                    MB_Utility.setSolidColor(tx, col);
                                }
                            }

                            if (progressInfo != null)
                            {
                                progressInfo("Adjusting for scale and offset " + tx, .01f);
                            }
                            if (textureEditorMethods != null)
                            {
                                textureEditorMethods.SetReadWriteFlag(tx, true, true);
                            }
                            tx = GetAdjustedForScaleAndOffset2(txs.ts[i], txs.obUVoffset, txs.obUVscale, data, combiner, LOG_LEVEL);

                            //create a resized copy if necessary
                            if (tx.width != tWidth || tx.height != tHeight)
                            {
                                if (progressInfo != null) progressInfo("Resizing texture '" + tx + "'", .01f);
                                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.LogWarning("Copying and resizing texture " + data.texPropertyNames[i].name + " from " + tx.width + "x" + tx.height + " to " + tWidth + "x" + tHeight);
                                tx = combiner._resizeTexture((Texture2D)tx, tWidth, tHeight);
                            }

                            txs.ts[i].t = tx;
                        }

                        Texture2D[] texToPack = new Texture2D[data.distinctMaterialTextures.Count];
                        for (int j = 0; j < data.distinctMaterialTextures.Count; j++)
                        {
                            Texture2D tx = data.distinctMaterialTextures[j].ts[i].GetTexture2D();
                            estArea += tx.width * tx.height;
                            if (data._considerNonTextureProperties)
                            {
                                //combine the tintColor with the texture
                                tx = combiner._createTextureCopy(tx);
                                data.nonTexturePropertyBlender.TintTextureWithTextureCombiner(tx, data.distinctMaterialTextures[j], data.texPropertyNames[i]);
                            }
                            texToPack[j] = tx;
                        }

                        if (textureEditorMethods != null) textureEditorMethods.CheckBuildSettings(estArea);

                        if (Math.Sqrt(estArea) > 3500f)
                        {
                            if (LOG_LEVEL >= MB2_LogLevel.warn) Debug.LogWarning("The maximum possible atlas size is 4096. Textures may be shrunk");
                        }
                        atlas = new Texture2D(1, 1, TextureFormat.ARGB32, true);
                        if (progressInfo != null)
                            progressInfo("Packing texture atlas " + data.texPropertyNames[i].name, .25f);
                        if (i == 0)
                        {
                            if (progressInfo != null)
                                progressInfo("Estimated min size of atlases: " + Math.Sqrt(estArea).ToString("F0"), .1f);
                            if (LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("Estimated atlas minimum size:" + Math.Sqrt(estArea).ToString("F0"));

                            if (data.distinctMaterialTextures.Count == 1 && data._fixOutOfBoundsUVs == false)
                            { //don't want to force power of 2 so tiling will still work
                                uvRects = new Rect[1] { new Rect(0f, 0f, 1f, 1f) };
                                atlas = _copyTexturesIntoAtlas(texToPack, data._atlasPadding, uvRects, texToPack[0].width, texToPack[0].height, combiner);
                            }
                            else
                            {
                                int maxAtlasSize = 4096;
                                uvRects = atlas.PackTextures(texToPack, data._atlasPadding, maxAtlasSize, false);
                            }

                            if (LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("After pack textures atlas size " + atlas.width + " " + atlas.height);
                            atlasSizeX = atlas.width;
                            atlasSizeY = atlas.height;
                            atlas.Apply();
                        }
                        else
                        {
                            if (progressInfo != null)
                                progressInfo("Copying Textures Into: " + data.texPropertyNames[i].name, .1f);
                            atlas = _copyTexturesIntoAtlas(texToPack, data._atlasPadding, uvRects, atlasSizeX, atlasSizeY, combiner);
                        }
                    }
                    atlases[i] = atlas;
                    //----------------------

                    if (data._saveAtlasesAsAssets && textureEditorMethods != null)
                    {
                        textureEditorMethods.SaveAtlasToAssetDatabase(atlases[i], data.texPropertyNames[i], i, data.resultMaterial);
                    }
                    data.resultMaterial.SetTextureOffset(data.texPropertyNames[i].name, Vector2.zero);
                    data.resultMaterial.SetTextureScale(data.texPropertyNames[i].name, Vector2.one);

                    combiner._destroyTemporaryTextures(); // need to save atlases before doing this
                    GC.Collect();
                }
            }
            packedAtlasRects.rects = uvRects;
            yield break;
        }

        internal static Texture2D _copyTexturesIntoAtlas(Texture2D[] texToPack, int padding, Rect[] rs, int w, int h, MB3_TextureCombiner combiner)
        {
            Texture2D ta = new Texture2D(w, h, TextureFormat.ARGB32, true);
            MB_Utility.setSolidColor(ta, Color.clear);
            for (int i = 0; i < rs.Length; i++)
            {
                Rect r = rs[i];
                Texture2D t = texToPack[i];
                int x = Mathf.RoundToInt(r.x * w);
                int y = Mathf.RoundToInt(r.y * h);
                int ww = Mathf.RoundToInt(r.width * w);
                int hh = Mathf.RoundToInt(r.height * h);
                if (t.width != ww && t.height != hh)
                {
                    t = MB_Utility.resampleTexture(t, ww, hh);
                    combiner._temporaryTextures.Add(t);
                }
                ta.SetPixels(x, y, ww, hh, t.GetPixels());
            }
            ta.Apply();
            return ta;
        }

        // used by Unity texture packer to handle tiled textures.
        // may create a new texture that has the correct tiling to handle fix out of bounds UVs
        internal static Texture2D GetAdjustedForScaleAndOffset2(MeshBakerMaterialTexture source, Vector2 obUVoffset, Vector2 obUVscale, MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner, MB2_LogLevel LOG_LEVEL)
        {
            Texture2D sourceTex = source.GetTexture2D();
            if (source.matTilingRect.x == 0f && source.matTilingRect.y == 0f && source.matTilingRect.width == 1f && source.matTilingRect.height == 1f)
            {
                if (data._fixOutOfBoundsUVs)
                {
                    if (obUVoffset.x == 0f && obUVoffset.y == 0f && obUVscale.x == 1f && obUVscale.y == 1f)
                    {
                        return sourceTex; //no adjustment necessary
                    }
                }
                else
                {
                    return sourceTex; //no adjustment necessary
                }
            }
            Vector2 dim = MB3_TextureCombinerPipeline.GetAdjustedForScaleAndOffset2Dimensions(source, obUVoffset, obUVscale, data, LOG_LEVEL);

            if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.LogWarning("GetAdjustedForScaleAndOffset2: " + sourceTex + " " + obUVoffset + " " + obUVscale);
            float newWidth = dim.x;
            float newHeight = dim.y;
            float scx = (float)source.matTilingRect.width;
            float scy = (float)source.matTilingRect.height;
            float ox = (float)source.matTilingRect.x;
            float oy = (float)source.matTilingRect.y;
            if (data._fixOutOfBoundsUVs)
            {
                scx *= obUVscale.x;
                scy *= obUVscale.y;
                ox = (float)(source.matTilingRect.x * obUVscale.x + obUVoffset.x);
                oy = (float)(source.matTilingRect.y * obUVscale.y + obUVoffset.y);
            }
            Texture2D newTex = combiner._createTemporaryTexture((int)newWidth, (int)newHeight, TextureFormat.ARGB32, true);
            for (int i = 0; i < newTex.width; i++)
            {
                for (int j = 0; j < newTex.height; j++)
                {
                    float u = i / newWidth * scx + ox;
                    float v = j / newHeight * scy + oy;
                    newTex.SetPixel(i, j, sourceTex.GetPixelBilinear(u, v));
                }
            }
            newTex.Apply();
            return newTex;
        }
    }
}
