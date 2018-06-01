using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalOpus.MB.Core
{
    internal interface MB_ITextureCombinerPacker
    {
        IEnumerator CreateAtlases(ProgressUpdateDelegate progressInfo,
            MB3_TextureCombinerPipeline.TexturePipelineData data, MB3_TextureCombiner combiner,
            AtlasPackingResult packedAtlasRects,
            Texture2D[] atlases, MB2_EditorMethodsInterface textureEditorMethods,
            MB2_LogLevel LOG_LEVEL);
    }
}
