﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPK_RePack.Model.Payload
{
    class GenericChunkBlock
    {
        public int signature; //0x9e2a83c1
        public int blocksize;

        public int compressedSize;
        public int uncompressedSize_chunkheader;

        public List<ChunkBlock> chunkBlocks = new List<ChunkBlock>();



        public void Decompress(byte[] uncompressedData, int blockCount, BinaryReader reader, int compFlag)
        {
            int blockOffset = 0;

            for (int j = 0; j < blockCount; ++j)
            {
                var block = new ChunkBlock();
                block.compressedSize = reader.ReadInt32();
                block.uncompressedDataSize = reader.ReadInt32();

                chunkBlocks.Add(block);
            }


            foreach (ChunkBlock block in chunkBlocks)
            {
                block.compressedData = reader.ReadBytes(block.compressedSize);
                block.decompress(compFlag);

                Array.ConstrainedCopy(block.uncompressedData, 0, uncompressedData, blockOffset, block.uncompressedDataSize);
                blockOffset += block.uncompressedDataSize;

                //save memory
                block.uncompressedData = null;
            }

        }


    }
}
