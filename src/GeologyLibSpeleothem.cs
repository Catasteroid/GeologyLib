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
		
		// Model variables
        public string[] segments = new string[] { "segment1", null, "segment2", null, "segment3", null, "segment4", null, "segment5", null };
        public string[] bases = new string[] { "base1", "base1-short", "base2", "base2-short", "base3", "base3-short", "base4", "base4-short", "base5", "base5-short" };
        public string[] ends = new string[] { "end1", null, "end2", null, "end3", null, "end4", null, "end5", null };
		public bool validParts = false;
		
		if ( this.Attributes["segmentTypes"].Exists && this.Attributes["baseTypes"].Exists && this.Attributes["endTypes"].Exists ) 
			{ 
				if (this.Attributes["baseTypes"].Length == this.Attributes["segmentTypes"].Length && this.Attributes["endTypes"].Length == this.Attributes["segmentTypes"].Length)
				{
					Api.World.Logger.Notification("Custom segment, base and end attributes are defined and are matching length! Lengths-Segments:{0}/Bases:{1}/Ends:{2}",this.Attributes["segmentTypes"].Length,this.Attributes["baseTypes"].Length,this.Attributes["endTypes"].Length);
					List<String> addedBlockTypes = new List<String>();
					for (var i = 0; i < this.Attributes["segmentTypes"].Length; i++)
					{
						addedBlockTypes.Add(this.Attributes["segmentTypes"][i]);
					}
					segments = addedBlockTypes.ToArray();
					Api.World.Logger.Notification("Finished adding custom segment types! Content: {0}", string.Join(',', segments));
					addedBlockTypes.Clear();
					for (i = 0; i < this.Attributes["baseTypes"].Length; i++)
					{
						addedBlockTypes.Add(this.Attributes["baseTypes"][i]);
					}
					bases = addedBlockTypes.ToArray();
					Api.World.Logger.Notification("Finished adding custom segment types! Content: {0}", string.Join(',', bases));
					addedBlockTypes.Clear();
					for (i = 0; i < this.Attributes["endTypes"].Length; i++)
					{
						addedBlockTypes.Add(this.Attributes["endTypes"][i]);
					}
					ends = addedBlockTypes.ToArray();
					Api.World.Logger.Notification("Finished adding custom segment types! Content: {0}", string.Join(',', ends));
					
				} else {
					Api.World.Logger.Notification("Lengths of segmentTypes/baseTypes/endTypes were not equal; using default segment/base/end types! (Lengths were: {0}/{1}/{2}",this.Attributes["segmentTypes"].Length,this.Attributes["baseTypes"].Length,this.Attributes["endTypes"].Length);
				}
			} else { 
				Api.World.Logger.Notification("Using default segment/base/end types!");
			}
		
		// Placement variables
		public int placeAttempts;
		public int placeAttemptsRand;
		public int placeDispersionXZ;
		public int placeDispersionXZRand;
		public int placeDispersionY;
		public int placeDispersionYRand;
		public int placeMinY;
		public int placeMaxLength;
		
		if ( this.Attributes["placeAttempts"].Exists ) 
			{ placeAttempts = this.Attributes["placeAttempts"].AsInt(150); } else { placeAttempts = 150; }
		if ( this.Attributes["placeAttemptsRand"].Exists )
			{ placeAttemptsRand = this.Attributes["placeAttemptsRand"].AsInt(30); } else { placeAttemptsRand = 30;  }
		if ( this.Attributes["placeDispersionXZ"].Exists )
			{ placeDispersion = this.Attributes["placeDispersionXZ"].AsInt(11); } else { placeDispersionXZ = 11; }
		if ( this.Attributes["placeDispersionXZRand"].Exists )
			{ placeDispersionRand = this.Attributes["placeDispersionXZRand"].AsInt(5); } else { placeDispersionXZRand = 5; }
		if ( this.Attributes["placeDispersionY"].Exists )
			{ placeDispersion = this.Attributes["placeDispersionY"].AsInt(11); } else { placeDispersionY = 6; }
		if ( this.Attributes["placeDispersionYRand"].Exists )
			{ placeDispersionRand = this.Attributes["placeDispersionYRand"].AsInt(5); } else { placeDispersionYRand = 3; }
		if ( this.Attributes["placeMinY"].Exists )
			{ placeDispersion = this.Attributes["placeMinY"].AsInt(15); } else { placeMinY = 15; }
		if ( this.Attributes["placeMaxLength"].Exists )
			{ placeDispersion = this.Attributes["placeMaxLength"].AsInt(3); } else { placeMinY = 3; }
		
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public Block GetBlock(IWorldAccessor world, string rocktype, string thickness)
        {
            return world.GetBlock(CodeWithParts(rocktype, thickness));
        }


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

        public bool IsAttached(IWorldAccessor world, BlockPos pos)
        {
            Block upBlock = world.BlockAccessor.GetBlock(pos.UpCopy());

            return upBlock.SideSolid[BlockFacing.DOWN.Index] || upBlock is BlockGlowworms;
        }


        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            bool didplace = false;

            if (blockAccessor.GetBlock(pos).Replaceable < 6000) return false;

            BlockPos npos = pos.Copy();
			//for (int i = 0; i < 150 + worldGenRand.NextInt(30); i++)
            for (int i = 0; i < placeAttempts + worldGenRand.NextInt(placeAttemptsRand); i++)
            {
				//npos.X = pos.X + worldGenRand.NextInt(11) - 5;
                //npos.Y = pos.Y + worldGenRand.NextInt(11) - 5;
                //npos.Z = pos.Z + worldGenRand.NextInt(11) - 5;
				
                npos.X = pos.X + worldGenRand.NextInt(placeDispersionXZ) - placeDispersionXZRand;
                npos.Y = pos.Y + worldGenRand.NextInt(placeDispersionY) - placeDispersionYRand;
                npos.Z = pos.Z + worldGenRand.NextInt(placeDispersionXZ) - placeDispersionXZRand;

                //if (npos.Y > api.World.SeaLevel - 10 || npos.Y < 15) continue; // To hot for glowworms
				//if (npos.Y < 15) continue;
				if (npos.Y < placeMinY) continue;
                if (blockAccessor.GetBlock(npos).Replaceable < 6000) continue;

                didplace |= TryGenSodastraw(blockAccessor, npos, worldGenRand);
            }

            return didplace;
        }

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

        private void GenHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);

            Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
            blockAccessor.SetBlock(placeblock.Id, pos);
            
            if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

                int len = worldGenRand.NextInt(placeMaxLength);
                while (len-- > 0)
                {
                    pos.Down();
                    if (blockAccessor.GetBlock(pos).Replaceable > 6000)
                    {
                        blockAccessor.SetBlock(placeblock.Id, pos);
                    }
                }

                pos.Down();
                placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
                if (blockAccessor.GetBlock(pos).Replaceable > 6000)
                {
                    blockAccessor.SetBlock(placeblock.Id, pos);
                }
            }

        }
    }
	
}