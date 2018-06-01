using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace DigitalOpus.MB.Core
{
    internal class MB3_TextureCombinerCalculateAtlasRectsStandard : MB3_ITextureCombinerCalculateAtlasRects
    {
        public AtlasPackingResult CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, MB2_LogLevel LOG_LEVEL)
        {
            if (data._packingAlgorithm == MB2_PackingAlgorithmEnum.UnitysPackTextures)
            {
                //with Unity texture packer we don't find the rectangles, Unity does. When packer is run
                return new AtlasPackingResult(new AtlasPadding[0]);
            }
            AtlasPackingResult uvRects;
            if (data.distinctMaterialTextures.Count == 1 && data._fixOutOfBoundsUVs == false)
            {
                if (LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Only one image per atlas. Will re-use original texture");
                AtlasPadding[] paddings = new AtlasPadding[] { new AtlasPadding(data._atlasPadding) };
                uvRects = new AtlasPackingResult(paddings);
                uvRects.rects = new Rect[1];
                uvRects.rects[0] = new Rect(0f, 0f, 1f, 1f);
                uvRects.atlasX = data.distinctMaterialTextures[0].idealWidth;
                uvRects.atlasY = data.distinctMaterialTextures[0].idealHeight;
            }
            else
            {
                List<Vector2> imageSizes = new List<Vector2>();
                for (int i = 0; i < data.distinctMaterialTextures.Count; i++)
                {
                    imageSizes.Add(new Vector2(data.distinctMaterialTextures[i].idealWidth, data.distinctMaterialTextures[i].idealHeight));
                }
                MB2_TexturePacker tp = MB3_TextureCombinerPipeline.CreateTexturePacker(data._packingAlgorithm);
                tp.atlasMustBePowerOfTwo = data._meshBakerTexturePackerForcePowerOfTwo;
                int atlasMaxDimension = data._maxAtlasSize;
                List<AtlasPadding> paddings = new List<AtlasPadding>();
                for (int i = 0; i < imageSizes.Count; i++)
                {
                    AtlasPadding padding = new AtlasPadding();
                    padding.topBottom = data._atlasPadding;
                    padding.leftRight = data._atlasPadding;
                    if (data._packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Horizontal) padding.leftRight = 0;
                    if (data._packingAlgorithm == MB2_PackingAlgorithmEnum.MeshBakerTexturePacker_Vertical) padding.topBottom = 0;
                    paddings.Add(padding);
                }
                AtlasPackingResult[] packerRects = tp.GetRects(imageSizes, paddings, atlasMaxDimension, atlasMaxDimension, false);
                uvRects = packerRects[0];
            }
            return uvRects;
        }
    }
}
