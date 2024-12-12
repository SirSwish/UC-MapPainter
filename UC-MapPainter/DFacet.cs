using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UC_MapPainter
{
    public class DFacet
    {
        // 1-byte unsigned integer
        public byte FacetType { get; set; } = 1; // Default: 1
        public byte Height { get; set; } = 4;    // Default: 4

        // 2-element array of 1-byte unsigned integers
        public byte[] X { get; set; } = new byte[2];

        // 2-element array of 2-byte signed integers
        public short[] Y { get; set; } = new short[2];

        // 2-element array of 1-byte unsigned integers
        public byte[] Z { get; set; } = new byte[2];

        // 2-byte unsigned integers
        public ushort FacetFlags { get; set; } = 256; // Default: 256
        public ushort StyleIndex { get; set; } = 1;   // Default: 1
        public ushort Building { get; set; } = 1;
        public ushort DStorey { get; set; }

        // 1-byte unsigned integers
        public byte FHeight { get; set; }
        public byte BlockHeight { get; set; } = 16; // Default: 16
        public byte Open { get; set; }
        public byte Dfcache { get; set; }
        public byte Shake { get; set; }
        public byte CutHole { get; set; }

        // 2-element array of 1-byte unsigned integers
        public byte[] Counter { get; set; } = new byte[2];

        public const ushort FACET_FLAG_INVISIBLE = 1 << 0;
        public const ushort FACET_FLAG_INSIDE = 1 << 3;
        public const ushort FACET_FLAG_DLIT = 1 << 4;
        public const ushort FACET_FLAG_HUG_FLOOR = 1 << 5;
        public const ushort FACET_FLAG_ELECTRIFIED = 1 << 6;
        public const ushort FACET_FLAG_2SIDED = 1 << 7;
        public const ushort FACET_FLAG_UNCLIMBABLE = 1 << 8;
        public const ushort FACET_FLAG_ONBUILDING = 1 << 9;
        public const ushort FACET_FLAG_BARB_TOP = 1 << 10;
        public const ushort FACET_FLAG_SEETHROUGH = 1 << 11;
        public const ushort FACET_FLAG_OPEN = 1 << 12;
        public const ushort FACET_FLAG_90DEGREE = 1 << 13;
        public const ushort FACET_FLAG_2TEXTURED = 1 << 14;
        public const ushort FACET_FLAG_FENCE_CUT = 1 << 15;




        // Default constructor with preset defaults
        public DFacet() { }

        // Constructor for zero-initialized object
        public DFacet(bool initializeToZero)
        {
            if (initializeToZero)
            {
                FacetType = 0;
                Height = 0;
                X = new byte[2] { 0, 0 };
                Y = new short[2] { 0, 0 };
                Z = new byte[2] { 0, 0 };
                FacetFlags = 0;
                StyleIndex = 0;
                Building = 0;
                DStorey = 0;
                FHeight = 0;
                BlockHeight = 0;
                Open = 0;
                Dfcache = 0;
                Shake = 0;
                CutHole = 0;
                Counter = new byte[2] { 0, 0 };
            }
        }

        public void SetInvisible() => FacetFlags |= FACET_FLAG_INVISIBLE;
        public void UnsetInvisible() => FacetFlags &= unchecked((ushort)~FACET_FLAG_INVISIBLE);

        public void SetInside() => FacetFlags |= FACET_FLAG_INSIDE;
        public void UnsetInside() => FacetFlags &= unchecked((ushort)~FACET_FLAG_INSIDE);

        public void SetDlit() => FacetFlags |= FACET_FLAG_DLIT;
        public void UnsetDlit() => FacetFlags &= unchecked((ushort)~FACET_FLAG_DLIT);

        public void SetHugFloor() => FacetFlags |= FACET_FLAG_HUG_FLOOR;
        public void UnsetHugFloor() => FacetFlags &= unchecked((ushort)~FACET_FLAG_HUG_FLOOR);

        public void SetElectrified() => FacetFlags |= FACET_FLAG_ELECTRIFIED;
        public void UnsetElectrified() => FacetFlags &= unchecked((ushort)~FACET_FLAG_ELECTRIFIED);

        public void SetTwoSided() => FacetFlags |= FACET_FLAG_2SIDED;
        public void UnsetTwoSided() => FacetFlags &= unchecked((ushort)~FACET_FLAG_2SIDED);

        public void SetUnclimbable() => FacetFlags |= FACET_FLAG_UNCLIMBABLE;
        public void UnsetUnclimbable() => FacetFlags &= unchecked((ushort)~FACET_FLAG_UNCLIMBABLE);

        public void SetOnBuilding() => FacetFlags |= FACET_FLAG_ONBUILDING;
        public void UnsetOnBuilding() => FacetFlags &= unchecked((ushort)~FACET_FLAG_ONBUILDING);

        public void SetBarbTop() => FacetFlags |= FACET_FLAG_BARB_TOP;
        public void UnsetBarbTop() => FacetFlags &= unchecked((ushort)~FACET_FLAG_BARB_TOP);

        public void SetSeeThrough() => FacetFlags |= FACET_FLAG_SEETHROUGH;
        public void UnsetSeeThrough() => FacetFlags &= unchecked((ushort)~FACET_FLAG_SEETHROUGH);

        public void SetOpen() => FacetFlags |= FACET_FLAG_OPEN;
        public void UnsetOpen() => FacetFlags &= unchecked((ushort)~FACET_FLAG_OPEN);

        public void Set90Degree() => FacetFlags |= FACET_FLAG_90DEGREE;
        public void Unset90Degree() => FacetFlags &= unchecked((ushort)~FACET_FLAG_90DEGREE);

        public void SetTwoTextured() => FacetFlags |= FACET_FLAG_2TEXTURED;
        public void UnsetTwoTextured() => FacetFlags &= unchecked((ushort)~FACET_FLAG_2TEXTURED);

        public void SetFenceCut() => FacetFlags |= FACET_FLAG_FENCE_CUT;
        public void UnsetFenceCut() => FacetFlags &= unchecked((ushort)~FACET_FLAG_FENCE_CUT);

        // Is methods
        public bool IsInvisible() => (FacetFlags & FACET_FLAG_INVISIBLE) != 0;
        public bool IsInside() => (FacetFlags & FACET_FLAG_INSIDE) != 0;
        public bool IsDlit() => (FacetFlags & FACET_FLAG_DLIT) != 0;

        public bool IsHugFloor() => (FacetFlags & FACET_FLAG_HUG_FLOOR) != 0;

        public bool IsElectrified() => (FacetFlags & FACET_FLAG_ELECTRIFIED) != 0;
        public bool IsTwoSided() => (FacetFlags & FACET_FLAG_2SIDED) != 0;

        public bool IsUnclimbable() => (FacetFlags & FACET_FLAG_UNCLIMBABLE) != 0;
        public bool IsOnBuilding() => (FacetFlags & FACET_FLAG_ONBUILDING) != 0;
        public bool IsBarbTop() => (FacetFlags & FACET_FLAG_BARB_TOP) != 0;
        public bool IsSeeThrough() => (FacetFlags & FACET_FLAG_SEETHROUGH) != 0;
        public bool IsOpen() => (FacetFlags & FACET_FLAG_OPEN) != 0;
        public bool Is90Degree() => (FacetFlags & FACET_FLAG_90DEGREE) != 0;
        public bool IsTwoTextured() => (FacetFlags & FACET_FLAG_2TEXTURED) != 0;
        public bool IsFenceCut() => (FacetFlags & FACET_FLAG_FENCE_CUT) != 0;




        //public void setClimbable()
        //{
        //    //FacetFlags |= (1 << 6);
        //    FacetFlags &= unchecked((ushort)~(1 << 8));
        //    //for(ushort i = 0; i < 9; i++) {
        //    //    FacetFlags |= (ushort)(1 << i);
        //    //}
        //}

        public void ToggleInvisible()
        {
            FacetFlags ^= FACET_FLAG_INVISIBLE;
        }

        public void ToggleInside()
        {
            FacetFlags ^= FACET_FLAG_INSIDE;
        }

        public void ToggleDlit()
        {
            FacetFlags ^= FACET_FLAG_DLIT;
        }

        public void ToggleHugFloor()
        {
            FacetFlags ^= FACET_FLAG_HUG_FLOOR;
        }

        public void ToggleElectrified()
        {
            FacetFlags ^= FACET_FLAG_ELECTRIFIED;
        }

        public void ToggleTwoSided()
        {
            FacetFlags ^= FACET_FLAG_2SIDED;
        }

        public void ToggleUnclimbable()
        {
            FacetFlags ^= FACET_FLAG_UNCLIMBABLE;
        }

        public void ToggleOnBuilding()
        {
            FacetFlags ^= FACET_FLAG_ONBUILDING;
        }

        public void ToggleBarbTop()
        {
            FacetFlags ^= FACET_FLAG_BARB_TOP;
        }

        public void ToggleSeeThrough()
        {
            FacetFlags ^= FACET_FLAG_SEETHROUGH;
        }

        public void ToggleOpen()
        {
            FacetFlags ^= FACET_FLAG_OPEN;
        }

        public void Toggle90Degree()
        {
            FacetFlags ^= FACET_FLAG_90DEGREE;
        }

        public void ToggleTwoTextured()
        {
            FacetFlags ^= FACET_FLAG_2TEXTURED;
        }

        public void ToggleFenceCut()
        {
            FacetFlags ^= FACET_FLAG_FENCE_CUT;
        }
    }
}
