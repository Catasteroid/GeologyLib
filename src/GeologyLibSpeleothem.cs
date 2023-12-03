using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeologyLib
{

	public class BlockSodastraws : Block
    {
		public ICoreAPI Api => api;
		public string[] segments = new string[] { "segment1", null, "segment2", null, "segment3", null, "segment4", null, "segment5", null };
        public string[] bases = new string[] { "base1", "base1-short", "base2", "base2-short", "base3", "base3-short", "base4", "base4-short", "base5", "base5-short" };
        public string[] ends = new string[] { "end1", null, "end2", null, "end3", null, "end4", null, "end5", null };
		public int posRand = 5;
		public int minY = 15;
		public int placeAttempts;
		public int placeAttemptsRand;
		
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
			if ( this.Attributes["posRand"].Exists )
				{ placeDispersion = this.Attributes["posRand"].AsInt(5); } else { posRand = 5; }
			if ( this.Attributes["minY"].Exists )
				{ placeDispersion = this.Attributes["minY"].AsInt(15); } else { minY = 15; }
				
			// Defines the minimum number of attempts to place a column
			if ( this.Attributes["placeAttempts"].Exists ) 
				{ 
					placeAttempts = this.Attributes["placeAttempts"].AsInt(100); 
					//Api.World.Logger.Notification("Custom attribute placeAttempts defined: {0}",placeAttempts);
				} else { placeAttempts = 100; }
			
			// Defines the maximum number of attempts to place a column as placeAttempts+placeAttemptsRand
			if ( this.Attributes["placeAttemptsRand"].Exists )
				{
					placeAttemptsRand = this.Attributes["placeAttemptsRand"].AsInt(50); 
					//Api.World.Logger.Notification("Custom attribute placeAttemptsRand defined: {0}",placeAttemptsRand);
				} else { placeAttemptsRand = 50;  }	
        }

        public Block GetBlock(IWorldAccessor world, string rocktype, string thickness)
        {
            return world.GetBlock(CodeWithParts(rocktype, thickness));
        }

		/// <summary>
        /// Checks whether the block is still attached when it's neighbours update and breaks it if not
        /// </summary>
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (!IsAttached(world, pos))
            {
                world.BlockAccessor.BreakBlock(pos, null);
            }
        }


        /*public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            LCGRandom rand = new LCGRandom(world.Seed + world.ElapsedMilliseconds);
            TryPlaceBlockForWorldGen(world.BlockAccessor, blockSel.Position, blockSel.Face, rand);
            return true;
        }*/

		/// <summary>
        /// Checks whether the block is still attached, checking either whether theres a solid surface (usually rock) or another sodastraw segment above it
        /// </summary>
        public bool IsAttached(IWorldAccessor world, BlockPos pos)
        {
            Block upBlock = world.BlockAccessor.GetBlock(pos.UpCopy());

            return upBlock.SideSolid[BlockFacing.DOWN.Index] || upBlock is BlockSodastraws;
        }

		/// <summary>
        /// Repeatedly attempts to find a suitable location to place the sodastraws
		/// Will make 120-180 attempts to place a sodastraw in a position within a cube of 1+posRand*2 in each dimension
        /// </summary>
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            bool didplace = false;

            if (blockAccessor.GetBlock(pos).Replaceable < 6000) return false;

            BlockPos npos = pos.Copy();
            for (int i = 0; i < 120 + worldGenRand.NextInt(60); i++)
            {
                npos.X = pos.X + worldGenRand.NextInt((posRand*2)+1) - posRand;
                npos.Y = pos.Y + worldGenRand.NextInt((posRand*2)+1) - posRand;
                npos.Z = pos.Z + worldGenRand.NextInt((posRand*2)+1) - posRand;

                //if (npos.Y > api.World.SeaLevel - 10 || npos.Y < 15) continue; // To hot for glowworms
				if (npos.Y < minY) continue;
                if (blockAccessor.GetBlock(npos).Replaceable < 6000) continue;

                didplace |= TryGenSodastraw(blockAccessor, npos, worldGenRand);
            }

            return didplace;
        }
		
		/// <summary>
        /// Attempts to generate a single sodastraw and checks up to five blocks above the selected spot for a viable rock ceiling to build down from
        /// </summary>
        private bool TryGenSodastraw(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            bool didplace = false;

            for (int dy = 0; dy < 5; dy++)
            {
                Block block = blockAccessor.GetBlock(pos.X, pos.Y + dy, pos.Z);
                if (block.SideSolid[BlockFacing.DOWN.Index])
                {
                    GenHere(blockAccessor, pos.AddCopy(0, dy - 1, 0), worldGenRand);
                    break;
                }
                else if (block.Id != 0) break;
            }

            return didplace;
        }

		/// <summary>
        /// Generates a sodastraw and handles the placement of blocks
		/// Places a base block and if the base block isn't a short base (which is indicated by segments[rnd] being null) 1-3 segments and an end block is placed below
        /// </summary>
        private void GenHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);
			Block placeblock = blockAccessor.GetBlock(CodeWithVariant("type", bases[rnd]));
            //Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
            blockAccessor.SetBlock(placeblock.Id, pos);
            
            if (segments[rnd] != null)
            {
                //placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]);
				placeblock = blockAccessor.GetBlock(CodeWithVariant("type", segments[rnd]));

                int len = worldGenRand.NextInt(3);
                while (len-- > 0)
                {
                    pos.Down();
                    if (blockAccessor.GetBlock(pos).Replaceable > 6000)
                    {
                        blockAccessor.SetBlock(placeblock.Id, pos);
                    }
                }

                pos.Down();
                //placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
				placeblock = blockAccessor.GetBlock(CodeWithVariant("type", ends[rnd]));
                if (blockAccessor.GetBlock(pos).Replaceable > 6000)
                {
                    blockAccessor.SetBlock(placeblock.Id, pos);
                }
            }

        }
    }

    
	
}