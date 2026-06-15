using UnityEngine;

namespace LizardCrossing
{
    public enum CosmeticSlot { Hat, Glasses, Pattern, TailColor, Backpack, Trail }
    public enum Rarity { Common, Rare, Epic }

    /// <summary>
    /// One purely-cosmetic item (packet: hats, sunglasses, skin patterns, tail
    /// colors, backpacks, glow/dust trails, rare skins). No stats — visual only,
    /// so cosmetics can never be pay-to-win. One item per slot may be equipped.
    /// `Tint`/`ShapeId` drive the procedural rendering until authored art lands.
    /// </summary>
    public class CosmeticItem
    {
        public string Id;
        public string Name;
        public CosmeticSlot Slot;
        public Rarity Rarity;
        public int CostBugs;     // 0 = owned by default
        public bool Premium;     // also buyable with gems
        public int CostGems;
        public Color Tint;       // pattern/tail/trail color, or accent for props
        public string ShapeId;   // which procedural shape to build (hats/glasses/backpacks)

        public bool IsDefault { get { return CostBugs <= 0 && !Premium; } }

        public static readonly CosmeticItem[] All =
        {
            // ---- Hats ----
            new CosmeticItem { Id = "hat_none", Name = "No Hat", Slot = CosmeticSlot.Hat, CostBugs = 0, ShapeId = "none" },
            new CosmeticItem { Id = "hat_straw", Name = "Straw Hat", Slot = CosmeticSlot.Hat, Rarity = Rarity.Common,
                CostBugs = 150, Tint = new Color(0.85f, 0.72f, 0.4f), ShapeId = "straw" },
            new CosmeticItem { Id = "hat_party", Name = "Party Cone", Slot = CosmeticSlot.Hat, Rarity = Rarity.Common,
                CostBugs = 200, Tint = new Color(0.95f, 0.35f, 0.55f), ShapeId = "cone" },
            new CosmeticItem { Id = "hat_crown", Name = "Tiny Crown", Slot = CosmeticSlot.Hat, Rarity = Rarity.Epic,
                CostBugs = 1500, Premium = true, CostGems = 90, Tint = new Color(1f, 0.84f, 0.2f), ShapeId = "crown" },

            // ---- Glasses ----
            new CosmeticItem { Id = "glasses_none", Name = "No Glasses", Slot = CosmeticSlot.Glasses, CostBugs = 0, ShapeId = "none" },
            new CosmeticItem { Id = "glasses_shades", Name = "Cool Shades", Slot = CosmeticSlot.Glasses, Rarity = Rarity.Common,
                CostBugs = 180, Tint = new Color(0.1f, 0.1f, 0.12f), ShapeId = "shades" },
            new CosmeticItem { Id = "glasses_heart", Name = "Heart Glasses", Slot = CosmeticSlot.Glasses, Rarity = Rarity.Rare,
                CostBugs = 450, Tint = new Color(0.95f, 0.3f, 0.5f), ShapeId = "heart" },

            // ---- Skin patterns ----
            new CosmeticItem { Id = "pattern_none", Name = "Smooth", Slot = CosmeticSlot.Pattern, CostBugs = 0, ShapeId = "none" },
            new CosmeticItem { Id = "pattern_spots", Name = "Spots", Slot = CosmeticSlot.Pattern, Rarity = Rarity.Common,
                CostBugs = 220, Tint = new Color(0.15f, 0.3f, 0.12f), ShapeId = "spots" },
            new CosmeticItem { Id = "pattern_neon", Name = "Neon Stripes", Slot = CosmeticSlot.Pattern, Rarity = Rarity.Rare,
                CostBugs = 500, Tint = new Color(0.2f, 1f, 0.85f), ShapeId = "stripes" },

            // ---- Tail colors ----
            new CosmeticItem { Id = "tail_default", Name = "Natural Tail", Slot = CosmeticSlot.TailColor, CostBugs = 0, ShapeId = "tint" },
            new CosmeticItem { Id = "tail_red", Name = "Ember Tail", Slot = CosmeticSlot.TailColor, Rarity = Rarity.Common,
                CostBugs = 160, Tint = new Color(0.95f, 0.35f, 0.2f), ShapeId = "tint" },
            new CosmeticItem { Id = "tail_blue", Name = "Frost Tail", Slot = CosmeticSlot.TailColor, Rarity = Rarity.Common,
                CostBugs = 160, Tint = new Color(0.3f, 0.6f, 0.95f), ShapeId = "tint" },
            new CosmeticItem { Id = "tail_rainbow", Name = "Rainbow Tail", Slot = CosmeticSlot.TailColor, Rarity = Rarity.Epic,
                CostBugs = 1800, Premium = true, CostGems = 110, Tint = new Color(1f, 0.5f, 0.9f), ShapeId = "rainbow" },

            // ---- Backpacks ----
            new CosmeticItem { Id = "pack_none", Name = "No Backpack", Slot = CosmeticSlot.Backpack, CostBugs = 0, ShapeId = "none" },
            new CosmeticItem { Id = "pack_tiny", Name = "Tiny Backpack", Slot = CosmeticSlot.Backpack, Rarity = Rarity.Rare,
                CostBugs = 600, Tint = new Color(0.85f, 0.4f, 0.3f), ShapeId = "pack" },

            // ---- Trails ----
            new CosmeticItem { Id = "trail_none", Name = "No Trail", Slot = CosmeticSlot.Trail, CostBugs = 0, ShapeId = "none" },
            new CosmeticItem { Id = "trail_dust", Name = "Dust Trail", Slot = CosmeticSlot.Trail, Rarity = Rarity.Common,
                CostBugs = 250, Tint = new Color(0.8f, 0.75f, 0.6f), ShapeId = "dust" },
            new CosmeticItem { Id = "trail_glow", Name = "Glow Trail", Slot = CosmeticSlot.Trail, Rarity = Rarity.Rare,
                CostBugs = 700, Tint = new Color(0.4f, 0.95f, 1f), ShapeId = "glow" },
            new CosmeticItem { Id = "trail_star", Name = "Star Trail", Slot = CosmeticSlot.Trail, Rarity = Rarity.Epic,
                CostBugs = 2000, Premium = true, CostGems = 120, Tint = new Color(1f, 0.9f, 0.4f), ShapeId = "star" },
        };

        public static CosmeticItem Get(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>The free default item for a slot (used when nothing is equipped).</summary>
        public static CosmeticItem DefaultFor(CosmeticSlot slot)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Slot == slot && All[i].IsDefault) return All[i];
            return null;
        }
    }
}
