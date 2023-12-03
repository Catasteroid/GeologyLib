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
		
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
			if ( this.Attributes["posRand"].Exists )
				{ placeDispersion = this.Attributes["posRand"].AsInt(5); } else { posRand = 5; }
			if ( this.Attributes["minY"].Exists )
				{ placeDispersion = this.Attributes["minY"].AsInt(15); } else { minY = 15; }
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

    public class BlockVerticalGenerator : Block
    {
		public ICoreAPI Api => api;
		
		// Model variables
        public string[] segments = new string[] { "segment1", null, "segment2", null, "segment3", null, "segment4", null, "segment5", null };
        public string[] bases = new string[] { "base1", "base1-short", "base2", "base2-short", "base3", "base3-short", "base4", "base4-short", "base5", "base5-short" };
        public string[] ends = new string[] { "end1", null, "end2", null, "end3", null, "end4", null, "end5", null };
		public bool validParts = false;
		
		// Placement variables
		public int placeAttempts;
		public int placeAttemptsRand;
		public int placeDispersionXZ;
		public int placeDispersionXZRand;
		public int placeDispersionY;
		public int placeDispersionYRand;
		public int placeMinY;
		public int placeMinLength;
		public int placeMaxLength;
		public int maxReplaceable;
		public bool useOldGeneration;
		public bool generateUpwards;
		
		public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
			
			//Handle custom segment, base and end block types, check attributes for whether custom blocks are defined
			//All three attributes, segments, bases and ends have to be defined and 
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
		
			//Check a bunch of generation attributes such as placement attempts, random addition to attempts, the X/Z and Y dispersal...
			//...minimum Y level for generation, maximum length in segments
			
			// Defines the minimum number of attempts to place a column
			if ( this.Attributes["placeAttempts"].Exists ) 
				{ 
					placeAttempts = this.Attributes["placeAttempts"].AsInt(150); 
					Api.World.Logger.Notification("Custom attribute placeAttempts defined: {0}",placeAttempts);
				} else { placeAttempts = 150; }
			
			// Defines the maximum number of attempts to place a column as placeAttempts+placeAttemptsRand
			if ( this.Attributes["placeAttemptsRand"].Exists )
				{
					placeAttemptsRand = this.Attributes["placeAttemptsRand"].AsInt(30); 
					Api.World.Logger.Notification("Custom attribute placeAttemptsRand defined: {0}",placeAttemptsRand);
				} else { placeAttemptsRand = 30;  }
			
			// The minimum length of the X axis of the cube the column is attempted to place in
			if ( this.Attributes["placeDispersionX"].Exists )
				{
					placeDispersionX = this.Attributes["placeDispersionX"].AsInt(11); 
					Api.World.Logger.Notification("Custom attribute placeDispersionX defined: {0}",placeDispersionX);
				} else { placeDispersionX = 11; }
			
			// The random addition to the length of the X axis of the cube the column is attempted to place in
			if ( this.Attributes["placeDispersionXRand"].Exists )
				{
					placeDispersionXRand = this.Attributes["placeDispersionXRand"].AsInt(5); 
					Api.World.Logger.Notification("Custom attribute placeDispersionXRand defined: {0}",placeDispersionXRand);
				} else { placeDispersionXRand = 5; }
			
			// The minimum length of the Y axis of the cube the column is attempted to place in			
			if ( this.Attributes["placeDispersionY"].Exists )
				{
					placeDispersionY = this.Attributes["placeDispersionY"].AsInt(11); 
					Api.World.Logger.Notification("Custom attribute placeDispersionY defined: {0}",placeDispersionY);
				} else { placeDispersionY = 6; }
			
			// The random addition to the length of the Y axis of the cube the column is attempted to place in
			if ( this.Attributes["placeDispersionYRand"].Exists )
				{
					placeDispersionYRand = this.Attributes["placeDispersionYRand"].AsInt(5); 
					Api.World.Logger.Notification("Custom attribute placeDispersionYRand defined: {0}",placeDispersionYRand);
				} else { placeDispersionYRand = 3; }
				
			// The minimum length of the Z axis of the cube the column is attempted to place in
			if ( this.Attributes["placeDispersionZ"].Exists )
				{
					placeDispersionZ = this.Attributes["placeDispersionZ"].AsInt(11); 
					Api.World.Logger.Notification("Custom attribute placeDispersionZ defined: {0}",placeDispersionZ);
				} else { placeDispersionZ = 11; }
			
			// The random addition to the length of the Z axis of the cube the column is attempted to place in
			if ( this.Attributes["placeDispersionZRand"].Exists )
				{
					placeDispersionZRand = this.Attributes["placeDispersionZRand"].AsInt(5); 
					Api.World.Logger.Notification("Custom attribute placeDispersionZRand defined: {0}",placeDispersionZRand);
				} else { placeDispersionZRand = 5; }
			
			// The minimum Y coordinate below which these columns can be placed
			if ( this.Attributes["placeMinY"].Exists )
				{
					placeMinY = this.Attributes["placeMinY"].AsInt(15);
					Api.World.Logger.Notification("Custom attribute placeMinY defined: {0}",placeMinY);
				} else { placeMinY = 15; }
			
			// The minimum length of a vertical column placed by the generator
			if ( this.Attributes["placeMinLength"].Exists )
				{
					placeMaxLength = this.Attributes["placeMinLength"].AsInt(2);
					Api.World.Logger.Notification("Custom attribute placeMinLength defined: {0}",placeMinLength);
				} else { placeMinLength = 2; }
			
			// The maximum length of a vertical column placed by the generator			
			if ( this.Attributes["placeMaxLength"].Exists )
				{
					placeMaxLengthRand = this.Attributes["placeMaxLength"].AsInt(4); 
					Api.World.Logger.Notification("Custom attribute placeMaxLength defined: {0}",placeMaxLength);
				} else { placeMaxLength = 4; }
				
			// The minimum replaceable value above which column blocks will be placed over and below which blocks will obstruct their placement
			if ( this.Attributes["maxReplaceable"].Exists )
				{
					maxReplaceable = this.Attributes["maxReplaceable"].AsInt(3000); 
					Api.World.Logger.Notification("Custom attribute maxReplaceable defined: {0}",maxReplaceable);
				} else { maxReplaceable = 3000; }
				
			// Defines whether to generate the vertical block column upwards or downwards
			// If true starts the column on a floor and builds it upwards, if false starts from a ceiling and builds it downwards
			if ( this.Attributes["generateUpwards"].Exists )
				{
					generateUpwards = this.Attributes["generateUpwards"].AsBool(false); 
					Api.World.Logger.Notification("Custom attribute generateUpwards defined: {0}",generateUpwards);
				} else { generateUpwards = false; }
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

		/// <summary>
        /// Checks whether the block is still attached, checking either whether theres a solid surface (usually rock) or another vertical column section supporting it
		/// If generateUpwards is true it will check if there's a surface or section below it, otherwise it checks if there's one above it
        /// </summary>
        public bool IsAttached(IWorldAccessor world, BlockPos pos)
        {
			if (!generateUpwards)
			{
				Block rootBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
				return rootBlock.SideSolid[BlockFacing.DOWN.Index] || rootBlock is BlockVerticalGenerator;
			} else {
				Block rootBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
				return rootBlock.SideSolid[BlockFacing.DOWN.Index] || rootBlock is BlockVerticalGenerator;
			}
        }

		/// <summary>
        /// Attempts to select a suitable location for placing a column of blocks, 
		/// either selecting a suitable surface on a ceiling or floor
        /// </summary>
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
				
                npos.X = pos.X + worldGenRand.NextInt(placeDispersionX) - placeDispersionXRand;
                npos.Y = pos.Y + worldGenRand.NextInt(placeDispersionY) - placeDispersionYRand;
                npos.Z = pos.Z + worldGenRand.NextInt(placeDispersionZ) - placeDispersionXRand;

                //if (npos.Y > api.World.SeaLevel - 10 || npos.Y < 15) continue; // To hot for glowworms
				//if (npos.Y < 15) continue;
				if (npos.Y < placeMinY) continue;
                if (blockAccessor.GetBlock(npos).Replaceable < 6000) continue;

                didplace = TryGenVertical(blockAccessor, npos, worldGenRand);
            }

            return didplace;
        }

		/// <summary>
        /// Attempts to select a suitable location for placing a column of blocks, 
		/// either selecting a suitable surface on a ceiling or floor
        /// </summary>
        private bool TryGenVertical(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
			// Not guaranteed to actually place the blocks but no checks are made to see if they're replacable
            bool didplace = false;
			if (generateUpwards == true)
			{
				for (int dy = 0; dy < 5; dy++)
				{
					Block block = blockAccessor.GetBlock(pos.X, pos.Y + dy, pos.Z);
					if (block.SideSolid[BlockFacing.DOWN.Index])
					{
						didplace = GenDownHere(blockAccessor, pos.AddCopy(0, dy - 1, 0), worldGenRand);
						break;
					}
					else if (block.Id != 0) break;
				}
			}
			else
			{
				for (int dy = 0; dy < 5; dy++)
				{
					Block block = blockAccessor.GetBlock(pos.X, pos.Y - dy, pos.Z);
					if (block.SideSolid[BlockFacing.UP.Index])
					{
						didplace = GenUpHere(blockAccessor, pos.AddCopy(0, dy + 1, 0), worldGenRand);
						break;
					}
					else if (block.Id != 0) break;
				}
			}
            return didplace;
        }

		/// <summary>
        /// Generates a column of blocks downwards choosing a number (rnd) from 0 to bases.Length, placing a base block from bases[rnd] and only a base if segments[rnd] is null
		/// Will not place a base if the block above cannot be replaced and a non-short base is placed (indicated by segments[rnd] being null)
		/// If segments[rnd] is not null it will place a length of blocks of segments[rnd] topped by a block of ends[rnd]
		/// The length of segments is a random number based on placeMaxLength + rand.NextInt(placeMaxLengthRand)
        /// </summary>
        private void GenDownHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);

			if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
			{
				// Don't even bother placing it if the block below is obstructed and it's not a short one-block base
				if (blockAccessor.GetBlock(pos.Down()).Replaceable < maxReplaceable && segments[rnd] != null) return false;
				
				Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
				blockAccessor.SetBlock(placeblock.Id, pos);
            }
			if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

                //int len = placeMaxLength + worldGenRand.NextInt(placeMaxLengthRand+1);
                int len = placeMinLength + worldGenRand.NextInt((placeMaxLength-placeMinLength)+1);
				while (len-- > 0)
                {
					// If the block below this space can be replaced place another segment otherwise terminate early and place an end block
					if (blockAccessor.GetBlock(pos.Down()).Replaceable > maxReplaceable)
					{
                        blockAccessor.SetBlock(placeblock.Id, pos);
						pos.Down();
					}
					else
					{
						break;
					}
				}
				// Place the end block
				placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
				blockAccessor.SetBlock(placeblock.Id, pos);
				return true;
            }
			return true;
        }
		
		/// <summary>
        /// Generates a column of blocks upwards choosing a number (rnd) from 0 to bases.Length, placing a base block from bases[rnd] and only a base if segments[rnd] is null
		/// Will not place a base if the block above cannot be replaced and a non-short base is placed (indicated by segments[rnd] being null)
		/// If segments[rnd] is not null it will place a length of blocks of segments[rnd] topped by a block of ends[rnd]
		/// The length of segments is a random number based on placeinLength + rand.NextInt((placeMaxLength-placeMinLength)+1)
        /// </summary>
        private void GenUpHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);

			if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
			{
				// Don't even bother placing it if the block below is obstructed and it's not a short one-block base
				if (blockAccessor.GetBlock(pos.Up()).Replaceable < maxReplaceable && segments[rnd] != null) return false;
				
				Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
				blockAccessor.SetBlock(placeblock.Id, pos);
            }
			
            if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

                //int len = placeMaxLength + worldGenRand.NextInt(placeMaxLengthRand+1);
                int len = placeMinLength + worldGenRand.NextInt((placeMaxLength-placeMinLength)+1);
				while (len-- > 0)
                {
					// If the block below this space can be replaced place another segment otherwise terminate early and place an end block
					if (blockAccessor.GetBlock(pos.Up()).Replaceable > maxReplaceable)
					{
                        blockAccessor.SetBlock(placeblock.Id, pos);
						pos.Up();
					}
					else
					{
						break;
					}
				}
				// Place the end block
				placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
				blockAccessor.SetBlock(placeblock.Id, pos);
				return true;
            }
			return true;
        }
		
		/*
		private void GenDownHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);

            Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
            blockAccessor.SetBlock(placeblock.Id, pos);
            
            if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

                //int len = placeMaxLength + worldGenRand.NextInt(placeMaxLengthRand+1);
                int len =  worldGenRand.NextInt(placeMaxLength,placeMaxLengthRand+1);
				while (len-- > 0)
                {
                    pos.Down();
                    if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                    {
                        blockAccessor.SetBlock(placeblock.Id, pos);
                    }
                }

                pos.Down();
                placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
                if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                {
                    blockAccessor.SetBlock(placeblock.Id, pos);
                }
            }

        }
		
		
		/// <summary>
        /// Generates a column of blocks upwards choosing a number (rnd) from 0 to bases.Length, placing a base block from bases[rnd] and only a base if segments[rnd] is null
		/// If segments[rnd] is not null (it could be if only the base or a "short" base is to be placed) it will place a length of blocks of segments[rnd] topped by a block of ends[rnd]
		/// The length of segments is a random number based on placeMaxLength + rand.NextInt(placeMaxLengthRand)
        /// </summary>
		private void GenDownHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);
			int placedblocks = 0;
			
            Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
			if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
            {
				blockAccessor.SetBlock(placeblock.Id, pos);
				placedblocks++;
				pos.Down();
			}
			else
			{
				return placedblocks;
			}
            
            if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

				//int len = placeMaxLength + worldGenRand.NextInt(placeMaxLengthRand+1);
                int len =  worldGenRand.NextInt(placeMaxLength,placeMaxLengthRand+1);
				
                //while (len-- > 0)
                //{
                //    pos.Up();
                //    if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                //    {
                //        blockAccessor.SetBlock(placeblock.Id, pos);
                //    }
                //}
				
				while (len-- > 0)
                {
                    
                    if (blockAccessor.GetBlock(pos.Down()).Replaceable > maxReplaceable)
                    {
						blockAccessor.SetBlock(placeblock.BlockId, pos);
						placedblocks++;
						pos.Down();
                    }
					else
					{
						break;
					}
                }

                placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
                if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                {
                    blockAccessor.SetBlock(placeblock.BlockId, pos);
					placedblocks++;
                }
				return placedblocks;
            }
			else
			{
				return placedblocks;
			}

        }
		
		private void GenUpHereOld(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);

			if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
			{
				// Don't even bother placing it if the block above is obstructed and it's not a short one-block base
				if (blockAccessor.GetBlock(pos.Up()).Replaceable < maxReplaceable && segments[rnd] != null) return false;
				
				Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
				blockAccessor.SetBlock(placeblock.Id, pos);
            }
            
            if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

                //int len = placeMaxLength + worldGenRand.NextInt(placeMaxLengthRand+1);
                int len =  worldGenRand.NextInt(placeMaxLength,placeMaxLengthRand+1);
				while (len-- > 0)
                {
                    pos.Up();
                    if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                    {
                        blockAccessor.SetBlock(placeblock.Id, pos);
                    }
                }

                pos.Up();
                placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
                if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                {
                    blockAccessor.SetBlock(placeblock.Id, pos);
                }
            }

        }
		
		/// <summary>
        /// Generates a column of blocks upwards choosing a number (rnd) from 0 to bases.Length, placing a base block from bases[rnd] and only a base if segments[rnd] is null
		/// If segments[rnd] is not null (it could be if only the base or a "short" base is to be placed) it will place a length of blocks of segments[rnd] topped by a block of ends[rnd]
		/// The length of segments is a random number based on placeMaxLength + rand.NextInt(placeMaxLengthRand)
        /// </summary>
		private void GenUpHere(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom worldGenRand)
        {
            int rnd = worldGenRand.NextInt(bases.Length);
			int placedblocks = 0;
			
            Block placeblock = api.World.GetBlock(CodeWithVariant("type", bases[rnd]));
			if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
            {
				blockAccessor.SetBlock(placeblock.Id, pos);
				placedblocks++;
				pos.Up();
			}
            
            if (segments[rnd] != null)
            {
                placeblock = api.World.GetBlock(CodeWithVariant("type", segments[rnd]));

				//int len = placeMaxLength + worldGenRand.NextInt(placeMaxLengthRand+1);
                int len =  worldGenRand.NextInt(placeMaxLength,placeMaxLengthRand+1);
				
                //while (len-- > 0)
                //{
                //    pos.Up();
                //    if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                //    {
                //        blockAccessor.SetBlock(placeblock.Id, pos);
                //    }
                //}
				
				while (len-- > 0)
                {
                    
                    if (blockAccessor.GetBlock(pos.Up()).Replaceable > maxReplaceable)
                    {
						blockAccessor.SetBlock(placeblock.BlockId, pos);
						placedblocks++;
						pos.Up();
						
						
                    }
					else
					{
						// Terminate early if you run out of space because there's something you can't replace above
						break;
					}
                }

                placeblock = api.World.GetBlock(CodeWithVariant("type", ends[rnd]));
                if (blockAccessor.GetBlock(pos).Replaceable > maxReplaceable)
                {
                    blockAccessor.SetBlock(placeblock.BlockId, pos);
					placedblocks++;
                }
				return placedblocks;
            }
			else
			{
				return placedblocks;
			}
        }
		*/
    }
	
}