using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace DigitalOpus.MB.Core
{
    internal interface MB3_ITextureCombinerCalculateAtlasRects
    {
        AtlasPackingResult CalculateAtlasRectangles(MB3_TextureCombinerPipeline.TexturePipelineData data, MB2_LogLevel LOG_LEVEL);
    }
}
