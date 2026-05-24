using System;
using UnityEngine;
using MallangTwins.Data;
namespace MallangTwins.Puzzle {
    [Serializable] public class BlockSpriteEntry { public BlockType type; public Sprite sprite; }
    public class BlockSpriteSet : MonoBehaviour {
        public BlockSpriteEntry[] entries;
        public Sprite Get(BlockType type) {
            if (entries == null) return null;
            foreach (var e in entries) if (e != null && e.type == type) return e.sprite;
            return null;
        }
    }
}
